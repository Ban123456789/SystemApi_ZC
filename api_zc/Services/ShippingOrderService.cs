using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace Accura_MES.Services
{
    public class ShippingOrderService : IShippingOrderService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;
        private ISequenceService _sequenceService;

        private ShippingOrderService(string connectionString, IGenericRepository genericRepository, ISequenceService sequenceService)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
            _sequenceService = sequenceService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 ShippingOrderService 實例，並接收外部資料庫連線
        /// </summary>
        /// <param name="connectionString">資料庫連線字串</param>
        /// <returns>ShippingOrderService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static ShippingOrderService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);
            
            // 建立序列號服務
            ISequenceService sequenceService = SequenceService.CreateService(connectionString);

            // 接收外部資料庫連線
            return new ShippingOrderService(connectionString, genericRepository, sequenceService);
        }

        /// <summary>
        /// 創建出貨單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="shippingOrderObject">出貨單資料列表</param>
        /// <returns>響應對象，包含創建的出貨單ID列表</returns>
        public async Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> shippingOrderObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 為每個出貨單生成編號
                await GenerateShippingOrderNumbers(connection, transaction, shippingOrderObject);

                // 為每個出貨單生成 carIndex
                await GenerateCarIndex(connection, transaction, shippingOrderObject);

                // 計算 outputTotalMeters 和 actualTotalMeters
                await CalculateTotalMeters(connection, transaction, shippingOrderObject);

                // 插入出貨單數據
                var insertedIds = await _genericRepository.CreateDataGeneric(
                    connection, 
                    transaction, 
                    userId, 
                    "shippingOrder", 
                    shippingOrderObject
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[ShippingOrderService] 成功創建 {insertedIds.Count} 個出貨單");

                // 成功返回，直接返回ID列表
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = insertedIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShippingOrderService] 創建出貨單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 為出貨單生成編號
        /// 規則：根據 shippingOrder.orderId -> order.shippedDate 分組，格式 0001, 0002, 0003...
        /// </summary>
        private async Task GenerateShippingOrderNumbers(
            SqlConnection connection, 
            SqlTransaction transaction, 
            List<Dictionary<string, object?>> shippingOrderObject)
        {
            foreach (var shippingOrder in shippingOrderObject)
            {
                // 獲取 orderId
                var orderIdValue = shippingOrder.GetValueOrDefault("orderId");
                
                if (orderIdValue == null)
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        "orderId 不能為空"
                    );
                }

                // 解析 orderId
                long orderId;
                if (orderIdValue is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Number)
                    {
                        orderId = jsonElement.GetInt64();
                    }
                    else
                    {
                        throw new CustomErrorCodeException(
                            SelfErrorCode.BAD_REQUEST, 
                            $"orderId 必須是數字，當前類型: {jsonElement.ValueKind}"
                        );
                    }
                }
                else if (orderIdValue is long longValue)
                {
                    orderId = longValue;
                }
                else if (orderIdValue is int intValue)
                {
                    orderId = intValue;
                }
                else
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        $"orderId 類型不正確，當前類型: {orderIdValue.GetType().Name}"
                    );
                }

                // 根據 orderId 查詢 order 的 shippedDate
                string querySql = "SELECT shippedDate FROM [order] WHERE id = @OrderId";
                using var queryCommand = new SqlCommand(querySql, connection, transaction);
                queryCommand.Parameters.AddWithValue("@OrderId", orderId);

                var shippedDateResult = await queryCommand.ExecuteScalarAsync();
                
                if (shippedDateResult == null || shippedDateResult == DBNull.Value)
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.NOT_FOUND_WITH_MSG, 
                        $"找不到 orderId={orderId} 的訂單或該訂單沒有 shippedDate"
                    );
                }

                // 解析 shippedDate 並格式化為分組鍵
                DateTime shippedDate = Convert.ToDateTime(shippedDateResult);
                string groupKey = shippedDate.ToString("yyyy-MM-dd");

                // 生成出貨單編號（格式：0001, 0002, 0003...）
                string shippingOrderNumber = await _sequenceService.GetNextNumberAsync(
                    connection,
                    transaction,
                    tableName: "shippingOrder",
                    groupKey: groupKey,
                    numberFormat: "0000"  // 4位數格式
                );

                // 將生成的編號寫入數據
                shippingOrder["number"] = shippingOrderNumber;

                Debug.WriteLine($"[ShippingOrderService] 為 orderId={orderId}, 日期 {groupKey} 生成出貨單編號: {shippingOrderNumber}");
            }
        }

        /// <summary>
        /// 為出貨單生成 carIndex
        /// 規則：根據 orderId 分組，從 1 開始編號，不需要補齊位數
        /// </summary>
        /// <param name="connection">資料庫連接</param>
        /// <param name="transaction">事務</param>
        /// <param name="shippingOrderObject">出貨單資料列表</param>
        private async Task GenerateCarIndex(
            SqlConnection connection,
            SqlTransaction transaction,
            List<Dictionary<string, object?>> shippingOrderObject)
        {
            // 按 orderId 分組
            var groupedByOrderId = shippingOrderObject
                .GroupBy(so => GetOrderIdFromDictionary(so))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in groupedByOrderId)
            {
                long orderId = kvp.Key;
                var currentBatchOrders = kvp.Value;

                // 查詢該 orderId 在數據庫中已存在的最大 carIndex
                string querySql = @"
                    SELECT ISNULL(MAX(carIndex), 0) AS maxCarIndex
                    FROM shippingOrder
                    WHERE orderId = @OrderId AND isDelete = 0";

                int maxCarIndex = 0;

                using (var queryCommand = new SqlCommand(querySql, connection, transaction))
                {
                    queryCommand.Parameters.AddWithValue("@OrderId", orderId);

                    var result = await queryCommand.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        maxCarIndex = Convert.ToInt32(result);
                    }
                }

                Debug.WriteLine($"[ShippingOrderService] orderId={orderId} 當前最大 carIndex={maxCarIndex}");

                // 為當前批次的出貨單依序生成 carIndex
                int currentCarIndex = maxCarIndex;

                foreach (var shippingOrder in currentBatchOrders)
                {
                    currentCarIndex++;
                    shippingOrder["carIndex"] = currentCarIndex;

                    Debug.WriteLine($"[ShippingOrderService] 為 orderId={orderId} 生成 carIndex={currentCarIndex}");
                }
            }
        }

        /// <summary>
        /// 計算出貨單的 outputTotalMeters 和 actualTotalMeters
        /// </summary>
        /// <param name="connection">資料庫連接</param>
        /// <param name="transaction">事務</param>
        /// <param name="shippingOrderObject">出貨單資料列表</param>
        private async Task CalculateTotalMeters(
            SqlConnection connection,
            SqlTransaction transaction,
            List<Dictionary<string, object?>> shippingOrderObject)
        {
            // 按 orderId 分組，方便批量計算
            var groupedByOrderId = shippingOrderObject
                .GroupBy(so => GetOrderIdFromDictionary(so))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in groupedByOrderId)
            {
                long orderId = kvp.Key;
                var currentBatchOrders = kvp.Value;

                // 1. 查詢數據庫中已存在的相同 orderId 的統計數據
                string querySql = @"
                    SELECT 
                        ISNULL(SUM(outputMeters), 0) AS totalOutputMeters,
                        ISNULL(SUM(outputMeters - returnMeters), 0) AS totalActualMeters
                    FROM shippingOrder
                    WHERE orderId = @OrderId AND isDelete = 0";

                using var queryCommand = new SqlCommand(querySql, connection, transaction);
                queryCommand.Parameters.AddWithValue("@OrderId", orderId);

                decimal existingOutputTotal = 0;
                decimal existingActualTotal = 0;

                using (var reader = await queryCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        existingOutputTotal = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                        existingActualTotal = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                    }
                }

                Debug.WriteLine($"[ShippingOrderService] orderId={orderId} 已存在數據: outputTotal={existingOutputTotal}, actualTotal={existingActualTotal}");

                // 2. 計算當前批次的累加值
                decimal currentBatchOutputTotal = 0;
                decimal currentBatchActualTotal = 0;

                foreach (var shippingOrder in currentBatchOrders)
                {
                    // 獲取 outputMeters
                    decimal outputMeters = GetDecimalValue(shippingOrder, "outputMeters");
                    
                    // 獲取 returnMeters
                    decimal returnMeters = GetDecimalValue(shippingOrder, "returnMeters");

                    // 累加當前批次
                    currentBatchOutputTotal += outputMeters;
                    currentBatchActualTotal += (outputMeters - returnMeters);

                    // 3. 計算包含當前記錄的總計
                    decimal outputTotalMeters = existingOutputTotal + currentBatchOutputTotal;
                    decimal actualTotalMeters = existingActualTotal + currentBatchActualTotal;

                    // 4. 寫入計算結果
                    shippingOrder["outputTotalMeters"] = outputTotalMeters;
                    shippingOrder["actualTotalMeters"] = actualTotalMeters;

                    Debug.WriteLine($"[ShippingOrderService] orderId={orderId} 計算完成: outputMeters={outputMeters}, returnMeters={returnMeters}, outputTotalMeters={outputTotalMeters}, actualTotalMeters={actualTotalMeters}");
                }
            }
        }

        /// <summary>
        /// 從 Dictionary 中獲取 orderId
        /// </summary>
        private long GetOrderIdFromDictionary(Dictionary<string, object?> dict)
        {
            var orderIdValue = dict.GetValueOrDefault("orderId");
            
            if (orderIdValue == null)
            {
                throw new CustomErrorCodeException(
                    SelfErrorCode.BAD_REQUEST, 
                    "orderId 不能為空"
                );
            }

            if (orderIdValue is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt64();
                }
                else
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        $"orderId 必須是數字，當前類型: {jsonElement.ValueKind}"
                    );
                }
            }
            else if (orderIdValue is long longValue)
            {
                return longValue;
            }
            else if (orderIdValue is int intValue)
            {
                return intValue;
            }
            else
            {
                throw new CustomErrorCodeException(
                    SelfErrorCode.BAD_REQUEST, 
                    $"orderId 類型不正確，當前類型: {orderIdValue.GetType().Name}"
                );
            }
        }

        /// <summary>
        /// 從 Dictionary 中獲取 decimal 值
        /// </summary>
        private decimal GetDecimalValue(Dictionary<string, object?> dict, string key)
        {
            var value = dict.GetValueOrDefault(key);
            
            if (value == null)
            {
                return 0;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetDecimal();
                }
                else if (jsonElement.ValueKind == JsonValueKind.Null)
                {
                    return 0;
                }
                else
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        $"{key} 必須是數字，當前類型: {jsonElement.ValueKind}"
                    );
                }
            }
            else if (value is decimal decimalValue)
            {
                return decimalValue;
            }
            else if (value is double doubleValue)
            {
                return Convert.ToDecimal(doubleValue);
            }
            else if (value is int intValue)
            {
                return intValue;
            }
            else if (value is long longValue)
            {
                return longValue;
            }
            else
            {
                // 嘗試轉換
                try
                {
                    return Convert.ToDecimal(value);
                }
                catch
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        $"{key} 類型不正確，無法轉換為數字，當前類型: {value.GetType().Name}"
                    );
                }
            }
        }

        /// <summary>
        /// 查詢出貨單列表
        /// </summary>
        /// <param name="searchParams">查詢參數（ids, orderId）</param>
        /// <returns>出貨單列表</returns>
        public async Task<ResponseObject> GetList(Dictionary<string, object?> searchParams)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 基礎 SQL
                string baseSql = @"
                    SELECT
                        shippingOrder.*,
                        [order].shippedDate AS shippedDate,
                        [order].number AS orderNumber,
                        customer.number AS customerNumber,
                        customer.nickName AS customerNickName,
                        [product].number AS productNumber,
                        project.name AS projectName,
                        project.areaCode as areaCode,
                        car.number as carNumber,
                        car.license as license
                    FROM shippingOrder
                    LEFT JOIN [order]
                        ON [order].id = shippingOrder.orderId
                    LEFT JOIN customer
                        ON customer.id = shippingOrder.customerId
                    LEFT JOIN [product]
                        ON [product].id = shippingOrder.productId
                    LEFT JOIN project
                        ON project.id = shippingOrder.projectId
                    LEFT JOIN car
                        ON car.id = shippingOrder.carId";

                // 動態構建 WHERE 條件
                var whereConditions = new List<string>();
                var parameters = new List<SqlParameter>();

                // 基礎條件
                whereConditions.Add("shippingOrder.isDelete = 0");

                // 處理 ids 參數
                if (searchParams.ContainsKey("ids") && searchParams["ids"] != null)
                {
                    var idsValue = searchParams["ids"];
                    var idList = ParseArrayToLongList(idsValue);
                    
                    if (idList.Any())
                    {
                        var idParams = string.Join(",", idList.Select((id, index) =>
                        {
                            var paramName = $"@id{index}";
                            parameters.Add(new SqlParameter(paramName, id));
                            return paramName;
                        }));
                        whereConditions.Add($"shippingOrder.id IN ({idParams})");
                    }
                }

                // 處理 startShippedDate 參數
                if (searchParams.ContainsKey("startShippedDate") && searchParams["startShippedDate"] != null)
                {
                    var dateValue = searchParams["startShippedDate"];
                    string dateStr = ParseValueToString(dateValue);
                    
                    if (!string.IsNullOrEmpty(dateStr))
                    {
                        whereConditions.Add("[order].shippedDate >= @startShippedDate");
                        parameters.Add(new SqlParameter("@startShippedDate", DateTime.Parse(dateStr)));
                    }
                }

                // 處理 endShippedDate 參數
                if (searchParams.ContainsKey("endShippedDate") && searchParams["endShippedDate"] != null)
                {
                    var dateValue = searchParams["endShippedDate"];
                    string dateStr = ParseValueToString(dateValue);
                    
                    if (!string.IsNullOrEmpty(dateStr))
                    {
                        whereConditions.Add("[order].shippedDate <= @endShippedDate");
                        parameters.Add(new SqlParameter("@endShippedDate", DateTime.Parse(dateStr).AddDays(1).AddSeconds(-1)));
                    }
                }

                // 處理 customerIds 參數
                if (searchParams.ContainsKey("customerIds") && searchParams["customerIds"] != null)
                {
                    var customerIdsValue = searchParams["customerIds"];
                    var customerIdList = ParseArrayToLongList(customerIdsValue);
                    
                    if (customerIdList.Any())
                    {
                        var customerIdParams = string.Join(",", customerIdList.Select((id, index) =>
                        {
                            var paramName = $"@customerId{index}";
                            parameters.Add(new SqlParameter(paramName, id));
                            return paramName;
                        }));
                        whereConditions.Add($"shippingOrder.customerId IN ({customerIdParams})");
                    }
                }

                // 處理 shippingOrderNumbers 參數
                if (searchParams.ContainsKey("shippingOrderNumbers") && searchParams["shippingOrderNumbers"] != null)
                {
                    var numbersValue = searchParams["shippingOrderNumbers"];
                    var numberList = ParseArrayToStringList(numbersValue);
                    
                    if (numberList.Any())
                    {
                        var numberParams = string.Join(",", numberList.Select((num, index) =>
                        {
                            var paramName = $"@shippingOrderNumber{index}";
                            parameters.Add(new SqlParameter(paramName, num));
                            return paramName;
                        }));
                        whereConditions.Add($"shippingOrder.number IN ({numberParams})");
                    }
                }

                // 處理 orderNumbers 參數
                if (searchParams.ContainsKey("orderNumbers") && searchParams["orderNumbers"] != null)
                {
                    var orderNumbersValue = searchParams["orderNumbers"];
                    var orderNumberList = ParseArrayToStringList(orderNumbersValue);
                    
                    if (orderNumberList.Any())
                    {
                        var orderNumberParams = string.Join(",", orderNumberList.Select((num, index) =>
                        {
                            var paramName = $"@orderNumber{index}";
                            parameters.Add(new SqlParameter(paramName, num));
                            return paramName;
                        }));
                        whereConditions.Add($"[order].number IN ({orderNumberParams})");
                    }
                }

                // 處理 carIds 參數
                if (searchParams.ContainsKey("carIds") && searchParams["carIds"] != null)
                {
                    var carIdsValue = searchParams["carIds"];
                    var carIdList = ParseArrayToLongList(carIdsValue);
                    
                    if (carIdList.Any())
                    {
                        var carIdParams = string.Join(",", carIdList.Select((id, index) =>
                        {
                            var paramName = $"@carId{index}";
                            parameters.Add(new SqlParameter(paramName, id));
                            return paramName;
                        }));
                        whereConditions.Add($"shippingOrder.carId IN ({carIdParams})");
                    }
                }

                // 處理 includeFinalRemaining 參數
                if (searchParams.ContainsKey("includeFinalRemaining") && searchParams["includeFinalRemaining"] != null)
                {
                    var includeFinalRemainingValue = searchParams["includeFinalRemaining"];
                    var includeFinalRemaining = Utils.ConvertToBool(includeFinalRemainingValue);
                    
                    if (includeFinalRemaining)
                    {
                        whereConditions.Add("shippingOrder.finalRemaining > 0");
                    }
                }

                // 組合 SQL
                string finalSql = baseSql;
                if (whereConditions.Any())
                {
                    finalSql += "\nWHERE " + string.Join(" AND ", whereConditions);
                }
                
                // 添加預設排序
                finalSql += "\nORDER BY [order].shippedDate DESC";

                // 執行查詢
                using var command = new SqlCommand(finalSql, connection);
                command.Parameters.AddRange(parameters.ToArray());

                var result = new List<Dictionary<string, object>>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string fieldName = reader.GetName(i);
                        object? fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        
                        // 格式化日期字段為 yyyy-MM-dd
                        if (fieldValue is DateTime dateTime)
                        {
                            fieldValue = dateTime.ToString("yyyy-MM-dd");
                        }
                        else if (fieldValue is DateTimeOffset dateTimeOffset)
                        {
                            fieldValue = dateTimeOffset.ToString("yyyy-MM-dd");
                        }
                        
                        row[fieldName] = fieldValue;
                    }
                    result.Add(row);
                }

                Debug.WriteLine($"[ShippingOrderService] 查詢到 {result.Count} 筆出貨單");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = result;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShippingOrderService] 查詢出貨單列表失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 更新出貨單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="shippingOrderObject">出貨單資料列表</param>
        /// <returns>響應對象</returns>
        public async Task<ResponseObject> Update(long userId, List<Dictionary<string, object?>> shippingOrderObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 收集所有受影響的 orderId
                var affectedOrderIds = new HashSet<long>();

                // 更新每個出貨單
                foreach (var shippingOrder in shippingOrderObject)
                {
                    // 獲取 id
                    var idValue = shippingOrder.GetValueOrDefault("id");
                    if (idValue == null)
                    {
                        throw new CustomErrorCodeException(
                            SelfErrorCode.BAD_REQUEST,
                            "更新時 id 不能為空"
                        );
                    }

                    long id = GetLongValue(idValue, "id");
                    long orderId = GetOrderIdFromDictionary(shippingOrder);

                    affectedOrderIds.Add(orderId);
                }
                // 更新出貨單（使用 GenericRepository 的更新方法）
                TableDatas tableDatas = new TableDatas();
                tableDatas.Datasheet = "shippingOrder";
                tableDatas.DataStructure = shippingOrderObject;
                await _genericRepository.GenericUpdate(
                    userId,
                    tableDatas,
                    connection,
                    transaction
                ); 

                // 重新計算所有受影響的 orderId 的累計米數
                foreach (var orderId in affectedOrderIds)
                {
                    await RecalculateAllMetersForOrder(connection, transaction, orderId);
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[ShippingOrderService] 成功更新 {shippingOrderObject.Count} 個出貨單");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = "更新成功";
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShippingOrderService] 更新出貨單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 刪除出貨單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="ids">要刪除的出貨單ID列表</param>
        /// <returns>響應對象</returns>
        public async Task<ResponseObject> Delete(long userId, List<long> ids)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                if (ids == null || !ids.Any())
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST,
                        "要刪除的 ID 列表不能為空"
                    );
                }

                // 收集所有受影響的 orderId
                var affectedOrderIds = new HashSet<long>();

                // 1. 先查詢這些出貨單的 orderId
                string queryOrderIdsSql = $@"
                    SELECT DISTINCT orderId 
                    FROM shippingOrder 
                    WHERE id IN ({string.Join(",", ids.Select((_, index) => $"@id{index}"))})
                    AND isDelete = 0";

                using (var queryCommand = new SqlCommand(queryOrderIdsSql, connection, transaction))
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        queryCommand.Parameters.AddWithValue($"@id{i}", ids[i]);
                    }

                    using var reader = await queryCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        affectedOrderIds.Add(reader.GetInt64(0));
                    }
                }

                Debug.WriteLine($"[ShippingOrderService] 刪除操作影響 {affectedOrderIds.Count} 個訂單");

                // 2. 將這些出貨單標記為已刪除（isDelete = true）
                string deleteSql = $@"
                    UPDATE shippingOrder 
                    SET isDelete = 1, 
                        modifiedBy = @UserId, 
                        modifiedOn = GETDATE()
                    WHERE id IN ({string.Join(",", ids.Select((_, index) => $"@id{index}"))})";

                using (var deleteCommand = new SqlCommand(deleteSql, connection, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@UserId", userId);
                    for (int i = 0; i < ids.Count; i++)
                    {
                        deleteCommand.Parameters.AddWithValue($"@id{i}", ids[i]);
                    }

                    int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                    Debug.WriteLine($"[ShippingOrderService] 標記 {rowsAffected} 個出貨單為已刪除");
                }

                // 3. 重新計算所有受影響的 orderId 的累計米數
                foreach (var orderId in affectedOrderIds)
                {
                    await RecalculateAllMetersForOrder(connection, transaction, orderId);
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[ShippingOrderService] 成功刪除 {ids.Count} 個出貨單");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = "刪除成功";
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShippingOrderService] 刪除出貨單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 重新計算指定 orderId 的所有出貨單的累計米數
        /// 按照 number 由小到大排序進行累計計算
        /// </summary>
        /// <param name="connection">資料庫連接</param>
        /// <param name="transaction">事務</param>
        /// <param name="orderId">訂單ID</param>
        private async Task RecalculateAllMetersForOrder(
            SqlConnection connection,
            SqlTransaction transaction,
            long orderId)
        {
            // 1. 查詢該 orderId 的所有未刪除的出貨單，按 number 排序
            string querySql = @"
                SELECT id, outputMeters, returnMeters
                FROM shippingOrder
                WHERE orderId = @OrderId AND isDelete = 0
                ORDER BY number ASC";

            var shippingOrders = new List<(long id, decimal outputMeters, decimal returnMeters)>();

            using (var queryCommand = new SqlCommand(querySql, connection, transaction))
            {
                queryCommand.Parameters.AddWithValue("@OrderId", orderId);

                using var reader = await queryCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long id = reader.GetInt64(0);
                    decimal outputMeters = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                    decimal returnMeters = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);

                    shippingOrders.Add((id, outputMeters, returnMeters));
                }
            }

            Debug.WriteLine($"[ShippingOrderService] orderId={orderId} 需要重新計算 {shippingOrders.Count} 個出貨單");

            // 2. 累計計算並更新每個出貨單
            decimal cumulativeOutputTotal = 0;
            decimal cumulativeActualTotal = 0;

            foreach (var (id, outputMeters, returnMeters) in shippingOrders)
            {
                // 累加當前記錄
                cumulativeOutputTotal += outputMeters;
                cumulativeActualTotal += (outputMeters - returnMeters);

                // 更新該出貨單的累計值
                string updateSql = @"
                    UPDATE shippingOrder
                    SET outputTotalMeters = @OutputTotalMeters,
                        actualTotalMeters = @ActualTotalMeters,
                        modifiedOn = GETDATE()
                    WHERE id = @Id";

                using var updateCommand = new SqlCommand(updateSql, connection, transaction);
                updateCommand.Parameters.AddWithValue("@OutputTotalMeters", cumulativeOutputTotal);
                updateCommand.Parameters.AddWithValue("@ActualTotalMeters", cumulativeActualTotal);
                updateCommand.Parameters.AddWithValue("@Id", id);

                await updateCommand.ExecuteNonQueryAsync();

                Debug.WriteLine($"[ShippingOrderService] 更新 id={id}: outputTotalMeters={cumulativeOutputTotal}, actualTotalMeters={cumulativeActualTotal}");
            }
        }

        /// <summary>
        /// 從 object 中獲取 long 值
        /// </summary>
        private long GetLongValue(object? value, string fieldName)
        {
            if (value == null)
            {
                throw new CustomErrorCodeException(
                    SelfErrorCode.BAD_REQUEST,
                    $"{fieldName} 不能為空"
                );
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt64();
                }
                else
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST,
                        $"{fieldName} 必須是數字，當前類型: {jsonElement.ValueKind}"
                    );
                }
            }
            else if (value is long longValue)
            {
                return longValue;
            }
            else if (value is int intValue)
            {
                return intValue;
            }
            else
            {
                try
                {
                    return Convert.ToInt64(value);
                }
                catch
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST,
                        $"{fieldName} 類型不正確，無法轉換為數字，當前類型: {value.GetType().Name}"
                    );
                }
            }
        }

        /// <summary>
        /// 解析數組為 long list
        /// </summary>
        private List<long> ParseArrayToLongList(object? value)
        {
            var result = new List<long>();
            
            if (value == null)
            {
                return result;
            }

            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number)
                    {
                        result.Add(item.GetInt64());
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 解析數組為 string list
        /// </summary>
        private List<string> ParseArrayToStringList(object? value)
        {
            var result = new List<string>();
            
            if (value == null)
            {
                return result;
            }

            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        result.Add(item.GetString() ?? string.Empty);
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 解析值為 string
        /// </summary>
        private string ParseValueToString(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return jsonElement.GetString() ?? string.Empty;
                }
                else if (jsonElement.ValueKind == JsonValueKind.Null)
                {
                    return string.Empty;
                }
            }

            return value.ToString() ?? string.Empty;
        }
    }
}

