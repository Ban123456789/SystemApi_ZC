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
    public class OrderService : IOrderService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;
        private ISequenceService _sequenceService;

        private OrderService(string connectionString, IGenericRepository genericRepository, ISequenceService sequenceService)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
            _sequenceService = sequenceService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 OrderService 實例，並接收外部資料庫連線
        /// </summary>
        /// <param name="connectionString">資料庫連線字串</param>
        /// <returns>OrderService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static OrderService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);
            
            // 建立序列號服務
            ISequenceService sequenceService = SequenceService.CreateService(connectionString);

            // 接收外部資料庫連線
            return new OrderService(connectionString, genericRepository, sequenceService);
        }

        /// <summary>
        /// 創建訂單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="orderObject">訂單資料列表</param>
        /// <returns>響應對象，包含創建的訂單ID列表</returns>
        public async Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> orderObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 為每個訂單生成編號
                await GenerateOrderNumbers(connection, transaction, orderObject);

                // 插入訂單數據
                var insertedIds = await _genericRepository.CreateDataGeneric(
                    connection, 
                    transaction, 
                    userId, 
                    "order", 
                    orderObject
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[OrderService] 成功創建 {insertedIds.Count} 個訂單");

                // 成功返回，直接返回ID列表
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = insertedIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OrderService] 創建訂單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 為訂單生成編號
        /// 規則：根據 shippedDate 分組，格式 01, 02, 03...
        /// </summary>
        private async Task GenerateOrderNumbers(
            SqlConnection connection, 
            SqlTransaction transaction, 
            List<Dictionary<string, object?>> orderObject)
        {
            foreach (var order in orderObject)
            {
                // 獲取出貨日期作為分組鍵
                var shippedDate = order.GetValueOrDefault("shippedDate");
                
                if (shippedDate == null)
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        "shippedDate 不能為空"
                    );
                }

                // 將日期轉換為字符串作為分組鍵
                string groupKey;
                
                // 處理 JsonElement 類型（ASP.NET Core JSON 反序列化）
                if (shippedDate is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        string dateString = jsonElement.GetString() ?? string.Empty;
                        if (DateTime.TryParse(dateString, out DateTime parsedDate))
                        {
                            groupKey = parsedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            throw new CustomErrorCodeException(
                                SelfErrorCode.BAD_REQUEST, 
                                $"shippedDate 格式不正確: {dateString}"
                            );
                        }
                    }
                    else
                    {
                        throw new CustomErrorCodeException(
                            SelfErrorCode.BAD_REQUEST, 
                            $"shippedDate 必須是日期字符串，當前類型: {jsonElement.ValueKind}"
                        );
                    }
                }
                else if (shippedDate is DateTime dateValue)
                {
                    groupKey = dateValue.ToString("yyyy-MM-dd");
                }
                else if (shippedDate is string dateString)
                {
                    // 嘗試解析日期字符串
                    if (DateTime.TryParse(dateString, out DateTime parsedDate))
                    {
                        groupKey = parsedDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        throw new CustomErrorCodeException(
                            SelfErrorCode.BAD_REQUEST, 
                            $"shippedDate 格式不正確: {dateString}"
                        );
                    }
                }
                else
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        $"shippedDate 類型不正確，當前類型: {shippedDate.GetType().Name}"
                    );
                }

                // 生成訂單編號（格式：01, 02, 03...）
                string orderNumber = await _sequenceService.GetNextNumberAsync(
                    connection,
                    transaction,
                    tableName: "order",
                    groupKey: groupKey,
                    numberFormat: "00"  // 2位數格式
                );

                // 將生成的編號寫入數據
                order["number"] = orderNumber;

                Debug.WriteLine($"[OrderService] 為日期 {groupKey} 生成訂單編號: {orderNumber}");
            }
        }

        /// <summary>
        /// 查詢訂單列表
        /// </summary>
        /// <param name="searchParams">查詢參數（ids, shippedDate）</param>
        /// <returns>訂單列表</returns>
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
                        [order].id AS id,
                        [order].number AS number,
                        [order].shippedDate AS shippedDate,
                        [order].customerId AS customerId,
                        customer.number AS customerNumber,
                        customer.nickName AS customerNickName,
                        [order].productId AS productId,
                        [product].number AS productNumber,
                        [product].pounds AS pounds,
                        [order].projectId AS projectId,
                        project.name AS projectName,
                        [order].collapse AS collapse,
                        [order].printCollapse AS printCollapse,
                        [order].ratio AS ratio,
                        project.businessId AS businessId,
                        business.name AS businessName,
                        project.areaCode AS areaCode,
                        [order].weight as weight,
                        
                        -- 出貨單總筆數
                        ISNULL(shippingStats.totalShippingOrders, 0) AS totalShippingOrders,
                        
                        -- 總出貨米數
                        ISNULL(shippingStats.totalOutputMeters, 0) AS totalOutputMeters,
                        
                        -- 總剩餘米數 (出貨米數 - 退貨米數)
                        ISNULL(shippingStats.totalRemaining, 0) AS totalRemaining

                    FROM [order]

                    INNER JOIN customer
                        ON customer.id = [order].customerId

                    LEFT JOIN [product]
                        ON [product].id = [order].productId

                    INNER JOIN project
                        ON project.id = [order].projectId

                    LEFT JOIN business
                        ON business.id = project.businessId

                    LEFT JOIN (
                        SELECT 
                            orderId,
                            COUNT(*) AS totalShippingOrders,
                            SUM(ISNULL(outputMeters, 0)) AS totalOutputMeters,
                            SUM(ISNULL(outputMeters, 0)) - SUM(ISNULL(returnMeters, 0)) AS totalRemaining
                        FROM shippingOrder
                        WHERE isDelete = 0 and type='1'
                        GROUP BY orderId
                    ) AS shippingStats
                        ON shippingStats.orderId = [order].id";

                // 動態構建 WHERE 條件
                var whereConditions = new List<string>();
                var parameters = new List<SqlParameter>();

                // 基礎條件
                whereConditions.Add("[order].isDelete = 0");
                whereConditions.Add("[order].type = '1'");

                // 處理 ids 參數
                if (searchParams.ContainsKey("ids") && searchParams["ids"] != null)
                {
                    var idsValue = searchParams["ids"];
                    List<long> idList = new List<long>();

                    // 簡單處理：轉換為 JsonElement 後遍歷
                    if (idsValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            idList.Add(item.GetInt64());
                        }
                    }

                    if (idList.Any())
                    {
                        var idParams = string.Join(",", idList.Select((id, index) =>
                        {
                            var paramName = $"@id{index}";
                            parameters.Add(new SqlParameter(paramName, id));
                            return paramName;
                        }));
                        whereConditions.Add($"[order].id IN ({idParams})");
                    }
                }

                // 處理 shippedDate 參數
                if (searchParams.ContainsKey("shippedDate") && searchParams["shippedDate"] != null)
                {
                    var shippedDateValue = searchParams["shippedDate"];
                    
                    // 簡單處理：轉換為字符串後解析
                    string dateString = shippedDateValue.ToString() ?? string.Empty;
                    if (DateTime.TryParse(dateString, out DateTime shippedDate))
                    {
                        whereConditions.Add("[order].shippedDate = @shippedDate");
                        parameters.Add(new SqlParameter("@shippedDate", shippedDate.Date));
                    }
                }

                // 組合 SQL
                string finalSql = baseSql;
                if (whereConditions.Any())
                {
                    finalSql += "\nWHERE " + string.Join(" AND ", whereConditions);
                }

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
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    result.Add(row);
                }

                Debug.WriteLine($"[OrderService] 查詢到 {result.Count} 筆訂單");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = result;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OrderService] 查詢訂單列表失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 刪除訂單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="request">刪除請求</param>
        /// <returns>響應對象</returns>
        public async Task<ResponseObject> DeleteOrders(long userId, DeleteOrdersRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                if (request.orderIds == null || !request.orderIds.Any())
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST,
                        "要刪除的訂單 ID 列表不能為空"
                    );
                }

                // 1. 取得這些訂單底下的所有出貨單，並檢查這些出貨單是否有至少一筆是已經沖銷過
                var orderIdParams = string.Join(",", request.orderIds);
                string checkOffsetSql = $@"
                    SELECT 
                        shippingOrder.id, 
                        shippingOrder.orderId, 
                        [order].number
                    FROM shippingOrder 
                    INNER JOIN [order] 
                        ON shippingOrder.orderId = [order].id 
                    WHERE shippingOrder.isDelete = 0 
                    AND shippingOrder.orderId IN ({orderIdParams})
                    AND shippingOrder.offsetMoney > 0";

                var offsetOrders = new List<Dictionary<string, object>>();
                var processedOrderIds = new HashSet<long>();

                using (var checkCommand = new SqlCommand(checkOffsetSql, connection, transaction))
                {
                    using var reader = await checkCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        long orderId = reader.GetInt64(reader.GetOrdinal("orderId"));
                        
                        // 相同的 orderId 就放一筆即可
                        if (!processedOrderIds.Contains(orderId))
                        {
                            var orderInfo = new Dictionary<string, object>
                            {
                                ["orderId"] = orderId,
                                ["orderNumber"] = reader.IsDBNull(reader.GetOrdinal("number")) ? null : reader.GetString(reader.GetOrdinal("number"))
                            };
                            offsetOrders.Add(orderInfo);
                            processedOrderIds.Add(orderId);
                        }
                    }
                }

                // 如果取出來的資料筆數大於 0，返回錯誤
                if (offsetOrders.Any())
                {
                    await transaction.RollbackAsync();
                    responseObject.SetErrorCode(SelfErrorCode.ORDER_HAS_OFFSET_SHIPPING_ORDER_CANNOT_DELETE);
                    responseObject.ErrorData = offsetOrders;
                    return responseObject;
                }

                // 2. 將這些訂單標記為已刪除（isDelete = 1）
                string deleteSql = $@"
                    UPDATE [order] 
                    SET isDelete = 1, 
                        modifiedBy = @UserId, 
                        modifiedOn = GETDATE()
                    WHERE id IN ({orderIdParams})
                    AND isDelete = 0";

                using (var deleteCommand = new SqlCommand(deleteSql, connection, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@UserId", userId);
                    int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                    Debug.WriteLine($"[OrderService] 標記 {rowsAffected} 個訂單為已刪除");
                }

                // 3. 將這些訂單底下的所有出貨單都刪除
                string deleteShippingOrderSql = $@"
                    UPDATE shippingOrder 
                    SET isDelete = 1, 
                        modifiedBy = @UserId, 
                        modifiedOn = GETDATE()
                    WHERE orderId IN ({orderIdParams})
                    AND isDelete = 0";

                using (var deleteShippingOrderCommand = new SqlCommand(deleteShippingOrderSql, connection, transaction))
                {
                    deleteShippingOrderCommand.Parameters.AddWithValue("@UserId", userId);
                    int shippingOrderRowsAffected = await deleteShippingOrderCommand.ExecuteNonQueryAsync();
                    Debug.WriteLine($"[OrderService] 標記 {shippingOrderRowsAffected} 個出貨單為已刪除");
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[OrderService] 成功刪除 {request.orderIds.Count} 個訂單");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = "刪除成功";
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OrderService] 刪除訂單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }
    }
}

