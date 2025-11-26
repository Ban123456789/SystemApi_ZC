using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Accura_MES.Services
{
    public class OffsetService : IOffsetService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;
        private ISequenceService _sequenceService;

        private OffsetService(string connectionString, IGenericRepository genericRepository, ISequenceService sequenceService)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
            _sequenceService = sequenceService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 OffsetService 實例
        /// </summary>
        /// <param name="connectionString">資料庫連線字串</param>
        /// <returns>OffsetService 實例</returns>
        public static OffsetService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);
            
            // 建立序列號服務
            ISequenceService sequenceService = SequenceService.CreateService(connectionString);

            return new OffsetService(connectionString, genericRepository, sequenceService);
        }

        /// <summary>
        /// 取得未沖帳清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含收款單和出貨單清單</returns>
        public async Task<ResponseObject> GetUnOffsetList(GetUnOffsetListRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. 查詢未沖帳的收款單
                var receiptSqlBuilder = new System.Text.StringBuilder(@"
                    SELECT 
                        receipt.*,
                        customer.number as customerNumber,
                        customer.name as customerName,
                        customer.nickName as customerNickName
                    FROM receipt
                    LEFT JOIN offsetRecord_receipt
                        ON receipt.id = offsetRecord_receipt.receiptId AND offsetRecord_receipt.isDelete = 0
                    INNER JOIN customer
                        ON receipt.customerId = customer.id
                    WHERE 
                        receipt.isDelete = 0
                        AND offsetRecord_receipt.id IS NULL");

                var receiptParameters = new List<SqlParameter>();

                // 如果 receiptDate 不為 null，添加條件
                if (!string.IsNullOrEmpty(request.receiptDate))
                {
                    if (DateTime.TryParse(request.receiptDate, out DateTime receiptDate))
                    {
                        receiptSqlBuilder.Append(" AND receipt.receiptDate = @receiptDate");
                        receiptParameters.Add(new SqlParameter("@receiptDate", receiptDate.Date));
                    }
                }
                // 如果 customerId 不為 null，添加條件
                if (request.customerId.HasValue)
                {
                    receiptSqlBuilder.Append(" AND receipt.customerId = @customerId");
                    receiptParameters.Add(new SqlParameter("@customerId", request.customerId.Value));
                }

                var receipts = new List<Dictionary<string, object>>();
                using (var receiptCommand = new SqlCommand(receiptSqlBuilder.ToString(), connection))
                {
                    receiptCommand.Parameters.AddRange(receiptParameters.ToArray());
                    using var receiptReader = await receiptCommand.ExecuteReaderAsync();
                    
                    while (await receiptReader.ReadAsync())
                    {
                        var receipt = new Dictionary<string, object>();
                        for (int i = 0; i < receiptReader.FieldCount; i++)
                        {
                            string columnName = receiptReader.GetName(i);
                            if (receiptReader.IsDBNull(i))
                            {
                                receipt[columnName] = null;
                            }
                            else
                            {
                                var value = receiptReader.GetValue(i);
                                // 處理日期格式
                                if (value is DateTime dateTime)
                                {
                                    // receiptDate、ticketDate、paymentDate、cashDate 使用 yyyy-MM-dd 格式
                                    if (columnName == "receiptDate" || columnName == "ticketDate" || 
                                        columnName == "paymentDate" || columnName == "cashDate")
                                    {
                                        receipt[columnName] = dateTime.ToString("yyyy-MM-dd");
                                    }
                                    else
                                    {
                                        receipt[columnName] = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                }
                                else
                                {
                                    receipt[columnName] = value;
                                }
                            }
                        }
                        receipts.Add(receipt);
                    }
                }

                // 2. 查詢未沖銷的出貨單
                // 未沖帳出貨單條件
                // 1. type == 1 (出貨單剩餘米數 * 單價 > 已沖帳金額)
                // 2. type == 2 && 單價 > 0 (單價 > 已沖帳金額)
                // 3. type == 2 && 單價 <= 0 (不存在 offsetRecord_shippingOrder 紀錄)
                var shippingOrderSqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        shippingOrder.id as id,
                        shippingOrder.type as type,
                        shippingOrder.price as price,
                        shippingOrder.outputMeters as outputMeters,
                        shippingOrder.returnMeters as returnMeters,
                        shippingOrder.remaining as remaining,
                        shippingOrder.offsetMoney as offsetMoney,
                        [order].id as orderId,
                        [order].shippedDate as shippedDate,
                        customer.id as customerId,
                        customer.number as customerNumber,
                        customer.name as customerName,
                        customer.nickName as customerNickName,
                        project.id as projectId,
                        project.number as projectNumber,
                        project.name as projectName,
                        [product].id as productId,
                        [product].number as productNumber
                    FROM
                        shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    INNER JOIN customer
                        ON shippingOrder.customerId = customer.id
                    INNER JOIN project
                        ON shippingOrder.projectId = project.id
                    INNER JOIN [product]
                        ON shippingOrder.productId = [product].id
                    WHERE
                        shippingOrder.isDelete = 0
                        AND (
                            (shippingOrder.type = '1' 
                             AND ISNULL(shippingOrder.price, 0) * COALESCE(NULLIF(shippingOrder.remaining, 0), 1) > ISNULL(shippingOrder.offsetMoney, 0))
                            OR
                            (shippingOrder.type = '2' 
                            AND (
                                (ISNULL(shippingOrder.price, 0) > 0 
                                    AND ISNULL(shippingOrder.price, 0) > ISNULL(shippingOrder.offsetMoney, 0))
                                OR
                                (ISNULL(shippingOrder.price, 0) <= 0 
                                    AND NOT EXISTS (
                                        SELECT 1 
                                            FROM offsetRecord_shippingOrder
                                        WHERE offsetRecord_shippingOrder.shippingOrderId = shippingOrder.id
                                            AND offsetRecord_shippingOrder.isDelete = 0
                                ))
                            ))
                        )");

                var shippingOrderParameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (request.needToSearchUnoffsetted)
                {
                    // 如果需要搜尋未沖銷的，查找區間之前的所有未沖銷出貨單
                    
                    // 如果只有 shippedDateEnd 存在，則查找 shippedDate <= shippedDateEnd
                    if (!string.IsNullOrEmpty(request.shippedDateEnd))
                    {
                        if (DateTime.TryParse(request.shippedDateEnd, out DateTime shippedDateEnd))
                        {
                            shippingOrderSqlBuilder.Append(" AND [order].shippedDate <= @shippedDateEnd");
                            shippingOrderParameters.Add(new SqlParameter("@shippedDateEnd", shippedDateEnd.Date));
                        }
                    }
                    // 如果 shippedDateStart 存在，則查找 shippedDate <= shippedDateStart
                    else if (!string.IsNullOrEmpty(request.shippedDateStart))
                    {
                        if (DateTime.TryParse(request.shippedDateStart, out DateTime shippedDateStart))
                        {
                            shippingOrderSqlBuilder.Append(" AND [order].shippedDate <= @shippedDateStart");
                            shippingOrderParameters.Add(new SqlParameter("@shippedDateStart", shippedDateStart.Date));
                        }
                    }
                }
                else
                {
                    // 正常日期範圍查詢
                    if (!string.IsNullOrEmpty(request.shippedDateStart))
                    {
                        if (DateTime.TryParse(request.shippedDateStart, out DateTime shippedDateStart))
                        {
                            shippingOrderSqlBuilder.Append(" AND [order].shippedDate >= @shippedDateStart");
                            shippingOrderParameters.Add(new SqlParameter("@shippedDateStart", shippedDateStart.Date));
                        }
                    }

                    if (!string.IsNullOrEmpty(request.shippedDateEnd))
                    {
                        if (DateTime.TryParse(request.shippedDateEnd, out DateTime shippedDateEnd))
                        {
                            shippingOrderSqlBuilder.Append(" AND [order].shippedDate <= @shippedDateEnd");
                            shippingOrderParameters.Add(new SqlParameter("@shippedDateEnd", shippedDateEnd.Date.AddDays(1).AddSeconds(-1)));
                        }
                    }
                }

                // 動態添加客戶 ID 條件
                if (request.customerId.HasValue)
                {
                    shippingOrderSqlBuilder.Append(" AND shippingOrder.customerId = @customerId");
                    shippingOrderParameters.Add(new SqlParameter("@customerId", request.customerId.Value));
                }

                // 動態添加工程 ID 條件
                if (request.projectIds != null && request.projectIds.Any())
                {
                    var projectIdParams = string.Join(",", request.projectIds.Select((id, index) => $"@projectId{index}"));
                    shippingOrderSqlBuilder.Append($" AND shippingOrder.projectId IN ({projectIdParams})");
                    
                    for (int i = 0; i < request.projectIds.Count; i++)
                    {
                        shippingOrderParameters.Add(new SqlParameter($"@projectId{i}", request.projectIds[i]));
                    }
                }

                var shippingOrders = new List<Dictionary<string, object>>();
                using (var shippingOrderCommand = new SqlCommand(shippingOrderSqlBuilder.ToString(), connection))
                {
                    shippingOrderCommand.Parameters.AddRange(shippingOrderParameters.ToArray());
                    using var shippingOrderReader = await shippingOrderCommand.ExecuteReaderAsync();
                    
                    while (await shippingOrderReader.ReadAsync())
                    {
                        var shippingOrder = new Dictionary<string, object>();
                        shippingOrder["id"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("id")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("id"));
                        shippingOrder["type"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("type")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("type"));
                        shippingOrder["price"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("price")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("price"));
                        shippingOrder["outputMeters"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("outputMeters")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("outputMeters"));
                        shippingOrder["returnMeters"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("returnMeters")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("returnMeters"));
                        shippingOrder["remaining"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("remaining")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("remaining"));
                        shippingOrder["offsetMoney"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("offsetMoney")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("offsetMoney"));
                        shippingOrder["orderId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("orderId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("orderId"));
                        
                        // 格式化日期為 yyyy-MM-dd
                        if (!shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("shippedDate")))
                        {
                            DateTime shippedDate = shippingOrderReader.GetDateTime(shippingOrderReader.GetOrdinal("shippedDate"));
                            shippingOrder["shippedDate"] = shippedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            shippingOrder["shippedDate"] = null;
                        }
                        
                        shippingOrder["customerId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("customerId"));
                        shippingOrder["customerNumber"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerNumber")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("customerNumber"));
                        shippingOrder["customerName"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerName")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("customerName"));
                        shippingOrder["customerNickName"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerNickName")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("customerNickName"));
                        shippingOrder["projectId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("projectId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("projectId"));
                        shippingOrder["projectNumber"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("projectNumber")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("projectNumber"));
                        shippingOrder["projectName"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("projectName")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("projectName"));
                        shippingOrder["productId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("productId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("productId"));
                        shippingOrder["productNumber"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("productNumber")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("productNumber"));
                        
                        shippingOrders.Add(shippingOrder);
                    }
                }

                Debug.WriteLine($"[OffsetService] 成功取得 {receipts.Count} 筆未沖帳收款單和 {shippingOrders.Count} 筆未沖銷出貨單");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = new
                {
                    receipts = receipts,
                    shippingOrders = shippingOrders
                };

                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OffsetService] 取得未沖帳清單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 沖帳
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="requests">沖帳請求列表</param>
        /// <returns>響應對象</returns>
        public async Task<ResponseObject> Offset(long userId, List<OffsetRequest> requests)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                var createdOffsetRecordIds = new List<long>();

                // 1. 迴圈 api payload 陣列
                foreach (var request in requests)
                {
                    // 2. 先針對 offsetRecord 做建立
                    var offsetRecord = request.offsetRecord;
                    
                    // 生成編號
                    string offsetNumber = await GenerateOffsetNumber(connection, transaction, offsetRecord.offsetDate);
                    
                    // 準備 offsetRecord 資料（只包含資料表需要的欄位）
                    var offsetRecordData = new Dictionary<string, object?>
                    {
                        ["number"] = offsetNumber,
                        ["offsetDate"] = DateTime.Parse(offsetRecord.offsetDate).Date,
                        ["customerId"] = offsetRecord.customerId,
                        ["note"] = offsetRecord.note,
                        ["discount"] = offsetRecord.discount,
                        ["prePayMoney"] = offsetRecord.prePayMoney,
                        ["totalOffsetMoney"] = offsetRecord.totalOffsetMoney,
                        ["shouldBeOffsetMoney"] = offsetRecord.shouldBeOffsetMoney
                    };

                    // 建立 offsetRecord
                    var offsetRecordIds = await _genericRepository.CreateDataGeneric(
                        connection,
                        transaction,
                        userId,
                        "offsetRecord",
                        new List<Dictionary<string, object?>> { offsetRecordData }
                    );

                    if (offsetRecordIds == null || !offsetRecordIds.Any())
                    {
                        throw new Exception("建立 offsetRecord 失敗");
                    }

                    long offsetRecordId = offsetRecordIds.First();
                    createdOffsetRecordIds.Add(offsetRecordId);

                    // 3. 迴圈 offsetRecordReceipts，建立 offsetRecord_receipt
                    if (request.offsetRecordReceipts != null && request.offsetRecordReceipts.Any())
                    {
                        // 檢查是否有收款單已沖帳過
                        var receiptIds = request.offsetRecordReceipts.Select(r => r.receiptId).ToList();
                        var receiptIdParams = string.Join(",", receiptIds);
                        
                        string checkReceiptSql = $@"
                            SELECT id 
                            FROM offsetRecord_receipt 
                            WHERE receiptId IN ({receiptIdParams}) AND isDelete = 0";
                        
                        var existingReceiptIds = new List<long>();
                        using (var checkCommand = new SqlCommand(checkReceiptSql, connection, transaction))
                        {
                            using var reader = await checkCommand.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                existingReceiptIds.Add(reader.GetInt64(0));
                            }
                        }
                        
                        // 如果有收款單已沖帳過，拋出錯誤
                        if (existingReceiptIds.Any())
                        {
                            await transaction.RollbackAsync();
                            throw new CustomErrorCodeException(
                                SelfErrorCode.RECEIPT_ALREADY_OFFSET_IN_CURRENT_REQUEST,
                                "本次沖帳有收款單已沖帳過，請重新整理畫面後重新沖帳"
                            );
                        }
                        
                        // 檢查是否有收款單已被刪除
                        string checkReceiptDeletedSql = $@"
                            SELECT id 
                            FROM receipt 
                            WHERE id IN ({receiptIdParams}) AND isDelete = 1";
                        
                        var deletedReceiptIds = new List<long>();
                        using (var checkCommand = new SqlCommand(checkReceiptDeletedSql, connection, transaction))
                        {
                            using var reader = await checkCommand.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                deletedReceiptIds.Add(reader.GetInt64(0));
                            }
                        }
                        
                        // 如果有收款單已被刪除，拋出錯誤
                        if (deletedReceiptIds.Any())
                        {
                            await transaction.RollbackAsync();
                            throw new CustomErrorCodeException(
                                SelfErrorCode.RECEIPT_DELETED_IN_CURRENT_REQUEST,
                                "本次沖帳有已被刪除的收款單，請重新整理畫面後重新沖帳"
                            );
                        }
                        
                        var receiptDataList = new List<Dictionary<string, object?>>();
                        foreach (var receipt in request.offsetRecordReceipts)
                        {
                            var receiptData = new Dictionary<string, object?>
                            {
                                ["offsetRecordId"] = offsetRecordId,
                                ["receiptId"] = receipt.receiptId,
                                ["price"] = receipt.price
                            };
                            receiptDataList.Add(receiptData);
                        }

                        await _genericRepository.CreateDataGeneric(
                            connection,
                            transaction,
                            userId,
                            "offsetRecord_receipt",
                            receiptDataList
                        );
                    }

                    // 4. 迴圈 offsetRecordShippingOrders，建立 offsetRecord_shippingOrder
                    if (request.offsetRecordShippingOrders != null && request.offsetRecordShippingOrders.Any())
                    {
                        // 檢查出貨單是否已完全沖帳
                        var shippingOrderIds = request.offsetRecordShippingOrders.Select(s => s.shippingOrderId).ToList();
                        var shippingOrderIdParams = string.Join(",", shippingOrderIds);
                        
                        string checkShippingOrderSql = $@"
                            SELECT id 
                            FROM shippingOrder
                            WHERE id IN ({shippingOrderIdParams})
                            AND (
                                (shippingOrder.type = '1' 
                                    AND ISNULL(shippingOrder.price, 0) * COALESCE(NULLIF(shippingOrder.remaining, 0), 1) > ISNULL(shippingOrder.offsetMoney, 0))
                                OR
                                (shippingOrder.type = '2' 
                                    AND (
                                        (ISNULL(shippingOrder.price, 0) > 0 
                                            AND ISNULL(shippingOrder.price, 0) > ISNULL(shippingOrder.offsetMoney, 0))
                                        OR
                                        (ISNULL(shippingOrder.price, 0) <= 0 
                                            AND NOT EXISTS (
                                                SELECT 1 
                                                    FROM offsetRecord_shippingOrder
                                                WHERE offsetRecord_shippingOrder.shippingOrderId = shippingOrder.id
                                                    AND offsetRecord_shippingOrder.isDelete = 0
                                        ))
                                    ))
                            )";
                        
                        var validShippingOrderIds = new List<long>();
                        using (var checkCommand = new SqlCommand(checkShippingOrderSql, connection, transaction))
                        {
                            using var reader = await checkCommand.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                validShippingOrderIds.Add(reader.GetInt64(0));
                            }
                        }
                        
                        // 檢查是否有出貨單已完全沖帳（不在查詢結果中）
                        var invalidShippingOrderIds = shippingOrderIds.Except(validShippingOrderIds).ToList();
                        if (invalidShippingOrderIds.Any())
                        {
                            await transaction.RollbackAsync();
                            throw new CustomErrorCodeException(
                                SelfErrorCode.SHIPPING_ORDER_ALREADY_FULLY_OFFSET,
                                "本次沖帳有出貨單已完全沖帳，請重新整理畫面後重新沖帳"
                            );
                        }
                        
                        // 檢查是否有出貨單已被刪除
                        string checkShippingOrderDeletedSql = $@"
                            SELECT id 
                            FROM shippingOrder 
                            WHERE id IN ({shippingOrderIdParams}) AND isDelete = 1";
                        
                        var deletedShippingOrderIds = new List<long>();
                        using (var checkCommand = new SqlCommand(checkShippingOrderDeletedSql, connection, transaction))
                        {
                            using var reader = await checkCommand.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                deletedShippingOrderIds.Add(reader.GetInt64(0));
                            }
                        }
                        
                        // 如果有出貨單已被刪除，拋出錯誤
                        if (deletedShippingOrderIds.Any())
                        {
                            await transaction.RollbackAsync();
                            throw new CustomErrorCodeException(
                                SelfErrorCode.SHIPPING_ORDER_DELETED_IN_CURRENT_REQUEST,
                                "本次沖帳有出貨單已被刪除，請重新整理畫面後重新沖帳"
                            );
                        }
                        
                        var shippingOrderDataList = new List<Dictionary<string, object?>>();
                        foreach (var shippingOrder in request.offsetRecordShippingOrders)
                        {
                            var shippingOrderData = new Dictionary<string, object?>
                            {
                                ["offsetRecordId"] = offsetRecordId,
                                ["shippingOrderId"] = shippingOrder.shippingOrderId,
                                ["price"] = shippingOrder.price,
                                ["quantity"] = shippingOrder.quantity,
                                ["offsetMoney"] = shippingOrder.offsetMoney
                            };
                            shippingOrderDataList.Add(shippingOrderData);
                        }

                        await _genericRepository.CreateDataGeneric(
                            connection,
                            transaction,
                            userId,
                            "offsetRecord_shippingOrder",
                            shippingOrderDataList
                        );

                        // 5. 迴圈 offsetRecordShippingOrders，更新 shippingOrder.offsetMoney
                        foreach (var shippingOrder in request.offsetRecordShippingOrders)
                        {
                            // 先查詢當前的 offsetMoney
                            string getCurrentOffsetMoneySql = @"
                                SELECT ISNULL(offsetMoney, 0)
                                FROM shippingOrder
                                WHERE id = @shippingOrderId";

                            decimal currentOffsetMoney = 0;
                            using (var getCommand = new SqlCommand(getCurrentOffsetMoneySql, connection, transaction))
                            {
                                getCommand.Parameters.AddWithValue("@shippingOrderId", shippingOrder.shippingOrderId);
                                var result = await getCommand.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    currentOffsetMoney = Convert.ToDecimal(result);
                                }
                            }

                            // 計算新的 offsetMoney（原先的 + 本次沖帳金額）
                            decimal newOffsetMoney = currentOffsetMoney + shippingOrder.offsetMoney;

                            // 更新 shippingOrder.offsetMoney
                            string updateOffsetMoneySql = @"
                                UPDATE shippingOrder
                                SET offsetMoney = @offsetMoney,
                                    modifiedBy = @modifiedBy,
                                    modifiedOn = GETDATE()
                                WHERE id = @shippingOrderId";

                            using (var updateCommand = new SqlCommand(updateOffsetMoneySql, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@offsetMoney", newOffsetMoney);
                                updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                                updateCommand.Parameters.AddWithValue("@shippingOrderId", shippingOrder.shippingOrderId);
                                await updateCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[OffsetService] 成功創建 {createdOffsetRecordIds.Count} 筆沖帳記錄");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = createdOffsetRecordIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OffsetService] 沖帳失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 取得沖帳紀錄
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含沖帳紀錄及其關聯的收款單和出貨單</returns>
        public async Task<ResponseObject> GetOffsetRecords(GetOffsetRecordsRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. 查詢沖帳紀錄
                var offsetRecordSqlBuilder = new System.Text.StringBuilder(@"
                    SELECT 
                        offsetRecord.id as id,
                        offsetRecord.number as number,
                        offsetRecord.offsetDate as offsetDate,
                        offsetRecord.customerId as customerId,
                        offsetRecord.note as note,
                        offsetRecord.discount as discount,
                        offsetRecord.prePayMoney as prePayMoney,
                        offsetRecord.totalOffsetMoney as totalOffsetMoney,
                        offsetRecord.shouldBeOffsetMoney as shouldBeOffsetMoney,
                        offsetRecord.createdOn as createdOn,
                        customer.number as customerNumber,
                        customer.name as customerName,
                        customer.nickName as customerNickName,
                        [user].name as createdBy
                    FROM offsetRecord 
                    INNER JOIN [user]
                        ON offsetRecord.createdBy = [user].id
                    INNER JOIN customer
                        ON offsetRecord.customerId = customer.id
                    WHERE offsetRecord.isDelete = 0");

                var offsetRecordParameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (!string.IsNullOrEmpty(request.offsetDateStart))
                {
                    offsetRecordSqlBuilder.Append($" AND offsetRecord.offsetDate >= '{request.offsetDateStart}'");
                }

                if (!string.IsNullOrEmpty(request.offsetDateEnd))
                {
                    offsetRecordSqlBuilder.Append($" AND offsetRecord.offsetDate <= '{request.offsetDateEnd}'");
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIds = string.Join(",", request.customerIds);
                    offsetRecordSqlBuilder.Append($" AND offsetRecord.customerId IN ({customerIds})");
                }

                var offsetRecords = new List<Dictionary<string, object>>();
                using (var offsetRecordCommand = new SqlCommand(offsetRecordSqlBuilder.ToString(), connection))
                {
                    offsetRecordCommand.Parameters.AddRange(offsetRecordParameters.ToArray());
                    using var offsetRecordReader = await offsetRecordCommand.ExecuteReaderAsync();
                    
                    while (await offsetRecordReader.ReadAsync())
                    {
                        var offsetRecord = new Dictionary<string, object>();
                        offsetRecord["id"] = offsetRecordReader.GetInt64(offsetRecordReader.GetOrdinal("id"));
                        offsetRecord["number"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("number")) ? null : offsetRecordReader.GetString(offsetRecordReader.GetOrdinal("number"));
                        
                        // 格式化日期為 yyyy-MM-dd
                        if (!offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("offsetDate")))
                        {
                            DateTime offsetDate = offsetRecordReader.GetDateTime(offsetRecordReader.GetOrdinal("offsetDate"));
                            offsetRecord["offsetDate"] = offsetDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            offsetRecord["offsetDate"] = null;
                        }
                        
                        offsetRecord["customerId"] = offsetRecordReader.GetInt64(offsetRecordReader.GetOrdinal("customerId"));
                        offsetRecord["note"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("note")) ? null : offsetRecordReader.GetString(offsetRecordReader.GetOrdinal("note"));
                        offsetRecord["discount"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("discount")) ? 0 : offsetRecordReader.GetDecimal(offsetRecordReader.GetOrdinal("discount"));
                        offsetRecord["prePayMoney"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("prePayMoney")) ? 0 : offsetRecordReader.GetDecimal(offsetRecordReader.GetOrdinal("prePayMoney"));
                        offsetRecord["totalOffsetMoney"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("totalOffsetMoney")) ? 0 : offsetRecordReader.GetDecimal(offsetRecordReader.GetOrdinal("totalOffsetMoney"));
                        offsetRecord["shouldBeOffsetMoney"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("shouldBeOffsetMoney")) ? 0 : offsetRecordReader.GetDecimal(offsetRecordReader.GetOrdinal("shouldBeOffsetMoney"));
                        
                        // 格式化日期為 yyyy-MM-dd HH:mm:ss
                        if (!offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("createdOn")))
                        {
                            DateTime createdOn = offsetRecordReader.GetDateTime(offsetRecordReader.GetOrdinal("createdOn"));
                            offsetRecord["createdOn"] = createdOn.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            offsetRecord["createdOn"] = null;
                        }
                        
                        offsetRecord["customerNumber"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("customerNumber")) ? null : offsetRecordReader.GetString(offsetRecordReader.GetOrdinal("customerNumber"));
                        offsetRecord["customerName"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("customerName")) ? null : offsetRecordReader.GetString(offsetRecordReader.GetOrdinal("customerName"));
                        offsetRecord["customerNickName"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("customerNickName")) ? null : offsetRecordReader.GetString(offsetRecordReader.GetOrdinal("customerNickName"));
                        offsetRecord["createdBy"] = offsetRecordReader.IsDBNull(offsetRecordReader.GetOrdinal("createdBy")) ? null : offsetRecordReader.GetString(offsetRecordReader.GetOrdinal("createdBy"));
                        
                        offsetRecords.Add(offsetRecord);
                    }
                }

                // 2. 查詢沖帳紀錄與收款單關聯
                var receiptSqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        offsetRecord_receipt.id as id,
                        offsetRecord_receipt.offsetRecordId as offsetRecordId,
                        offsetRecord_receipt.receiptId as receiptId,
                        offsetRecord_receipt.price as price,
                        receipt.number as receiptNumber,
                        receipt.receiptDate as receiptDate,
                        receipt.paymentType as paymentType,
                        receipt.note as receiptNote,
                        receipt.price as receiptPrice
                    FROM offsetRecord_receipt
                    INNER JOIN offsetRecord
                        ON offsetRecord_receipt.offsetRecordId = offsetRecord.id AND offsetRecord.isDelete = 0
                    INNER JOIN receipt
                        ON offsetRecord_receipt.receiptId = receipt.id
                    WHERE offsetRecord_receipt.isDelete = 0");

                var receiptParameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (!string.IsNullOrEmpty(request.offsetDateStart))
                {
                    receiptSqlBuilder.Append($" AND offsetRecord.offsetDate >= '{request.offsetDateStart}'");
                }

                if (!string.IsNullOrEmpty(request.offsetDateEnd))
                {
                    receiptSqlBuilder.Append($" AND offsetRecord.offsetDate <='{request.offsetDateEnd}'");
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIds = string.Join(",", request.customerIds);
                    receiptSqlBuilder.Append($" AND offsetRecord.customerId IN ({customerIds})");
                }

                var offsetRecordReceipts = new List<Dictionary<string, object>>();
                using (var receiptCommand = new SqlCommand(receiptSqlBuilder.ToString(), connection))
                {
                    receiptCommand.Parameters.AddRange(receiptParameters.ToArray());
                    using var receiptReader = await receiptCommand.ExecuteReaderAsync();
                    
                    while (await receiptReader.ReadAsync())
                    {
                        var receipt = new Dictionary<string, object>();
                        receipt["id"] = receiptReader.GetInt64(receiptReader.GetOrdinal("id"));
                        receipt["offsetRecordId"] = receiptReader.GetInt64(receiptReader.GetOrdinal("offsetRecordId"));
                        receipt["receiptId"] = receiptReader.GetInt64(receiptReader.GetOrdinal("receiptId"));
                        receipt["price"] = receiptReader.IsDBNull(receiptReader.GetOrdinal("price")) ? 0 : receiptReader.GetDecimal(receiptReader.GetOrdinal("price"));
                        receipt["receiptNumber"] = receiptReader.IsDBNull(receiptReader.GetOrdinal("receiptNumber")) ? null : receiptReader.GetString(receiptReader.GetOrdinal("receiptNumber"));
                        
                        // 格式化日期為 yyyy-MM-dd
                        if (!receiptReader.IsDBNull(receiptReader.GetOrdinal("receiptDate")))
                        {
                            DateTime receiptDate = receiptReader.GetDateTime(receiptReader.GetOrdinal("receiptDate"));
                            receipt["receiptDate"] = receiptDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            receipt["receiptDate"] = null;
                        }
                        
                        receipt["paymentType"] = receiptReader.IsDBNull(receiptReader.GetOrdinal("paymentType")) ? null : receiptReader.GetString(receiptReader.GetOrdinal("paymentType"));
                        receipt["receiptNote"] = receiptReader.IsDBNull(receiptReader.GetOrdinal("receiptNote")) ? null : receiptReader.GetString(receiptReader.GetOrdinal("receiptNote"));
                        receipt["receiptPrice"] = receiptReader.IsDBNull(receiptReader.GetOrdinal("receiptPrice")) ? 0 : receiptReader.GetDecimal(receiptReader.GetOrdinal("receiptPrice"));
                        
                        offsetRecordReceipts.Add(receipt);
                    }
                }

                // 3. 查詢沖帳紀錄與出貨單關聯
                var shippingOrderSqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        offsetRecord_shippingOrder.id as id,
                        offsetRecord_shippingOrder.offsetRecordId as offsetRecordId,
                        offsetRecord_shippingOrder.shippingOrderId as shippingOrderId,
                        offsetRecord_shippingOrder.price as price,
                        offsetRecord_shippingOrder.quantity as quantity,
                        offsetRecord_shippingOrder.offsetMoney as offsetMoney,
                        shippingOrder.type as type,
                        shippingOrder.price as shippingOrderPrice,
                        shippingOrder.outputMeters as outputMeters,
                        shippingOrder.offsetMoney as shippingOrderOffsetMoney,
                        [order].id as orderId,
                        [order].shippedDate as shippedDate,
                        customer.id as customerId,
                        customer.number as customerNumber,
                        customer.name as customerName,
                        customer.nickName as customerNickName,
                        project.id as projectId,
                        project.number as projectNumber,
                        project.name as projectName,
                        [product].id as productId,
                        [product].number as productNumber
                    FROM offsetRecord_shippingOrder
                    INNER JOIN offsetRecord
                        ON offsetRecord_shippingOrder.offsetRecordId = offsetRecord.id AND offsetRecord.isDelete = 0
                    INNER JOIN shippingOrder
                        ON offsetRecord_shippingOrder.shippingOrderId = shippingOrder.id
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    INNER JOIN customer
                        ON shippingOrder.customerId = customer.id
                    INNER JOIN project
                        ON shippingOrder.projectId = project.id
                    INNER JOIN [product]
                        ON shippingOrder.productId = [product].id
                    WHERE offsetRecord_shippingOrder.isDelete = 0");

                var shippingOrderParameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (!string.IsNullOrEmpty(request.offsetDateStart))
                {
                    shippingOrderSqlBuilder.Append($" AND offsetRecord.offsetDate >= '{request.offsetDateStart}'");
                }

                if (!string.IsNullOrEmpty(request.offsetDateEnd))
                {
                    shippingOrderSqlBuilder.Append($" AND offsetRecord.offsetDate <= '{request.offsetDateEnd}'");
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIds = string.Join(",", request.customerIds);
                    shippingOrderSqlBuilder.Append($" AND offsetRecord.customerId IN ({customerIds})");
                }

                var offsetRecordShippingOrders = new List<Dictionary<string, object>>();
                using (var shippingOrderCommand = new SqlCommand(shippingOrderSqlBuilder.ToString(), connection))
                {
                    shippingOrderCommand.Parameters.AddRange(shippingOrderParameters.ToArray());
                    using var shippingOrderReader = await shippingOrderCommand.ExecuteReaderAsync();
                    
                    while (await shippingOrderReader.ReadAsync())
                    {
                        var shippingOrder = new Dictionary<string, object>();
                        shippingOrder["id"] = shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("id"));
                        shippingOrder["offsetRecordId"] = shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("offsetRecordId"));
                        shippingOrder["shippingOrderId"] = shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("shippingOrderId"));
                        shippingOrder["price"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("price")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("price"));
                        shippingOrder["quantity"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("quantity")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("quantity"));
                        shippingOrder["offsetMoney"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("offsetMoney")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("offsetMoney"));
                        shippingOrder["type"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("type")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("type"));
                        shippingOrder["shippingOrderPrice"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("shippingOrderPrice")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("shippingOrderPrice"));
                        shippingOrder["outputMeters"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("outputMeters")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("outputMeters"));
                        shippingOrder["shippingOrderOffsetMoney"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("shippingOrderOffsetMoney")) ? 0 : shippingOrderReader.GetDecimal(shippingOrderReader.GetOrdinal("shippingOrderOffsetMoney"));
                        shippingOrder["orderId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("orderId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("orderId"));
                        
                        // 格式化日期為 yyyy-MM-dd
                        if (!shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("shippedDate")))
                        {
                            DateTime shippedDate = shippingOrderReader.GetDateTime(shippingOrderReader.GetOrdinal("shippedDate"));
                            shippingOrder["shippedDate"] = shippedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            shippingOrder["shippedDate"] = null;
                        }
                        
                        shippingOrder["customerId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("customerId"));
                        shippingOrder["customerNumber"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerNumber")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("customerNumber"));
                        shippingOrder["customerName"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerName")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("customerName"));
                        shippingOrder["customerNickName"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("customerNickName")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("customerNickName"));
                        shippingOrder["projectId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("projectId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("projectId"));
                        shippingOrder["projectNumber"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("projectNumber")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("projectNumber"));
                        shippingOrder["projectName"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("projectName")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("projectName"));
                        shippingOrder["productId"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("productId")) ? (long?)null : shippingOrderReader.GetInt64(shippingOrderReader.GetOrdinal("productId"));
                        shippingOrder["productNumber"] = shippingOrderReader.IsDBNull(shippingOrderReader.GetOrdinal("productNumber")) ? null : shippingOrderReader.GetString(shippingOrderReader.GetOrdinal("productNumber"));
                        
                        offsetRecordShippingOrders.Add(shippingOrder);
                    }
                }

                // 4. 依照 offsetRecords 為主，將相同的 offsetRecord.id 整合在一起
                var result = new List<Dictionary<string, object>>();
                
                foreach (var offsetRecord in offsetRecords)
                {
                    long offsetRecordId = (long)offsetRecord["id"];
                    
                    // 在 offsetRecord 本身添加 offsetRecordReceipts 和 offsetRecordShippingOrders 陣列
                    offsetRecord["offsetRecordReceipts"] = offsetRecordReceipts
                        .Where(r => (long)r["offsetRecordId"] == offsetRecordId)
                        .ToList();
                    offsetRecord["offsetRecordShippingOrders"] = offsetRecordShippingOrders
                        .Where(s => (long)s["offsetRecordId"] == offsetRecordId)
                        .ToList();
                    
                    result.Add(offsetRecord);
                }

                Debug.WriteLine($"[OffsetService] 成功取得 {result.Count} 筆沖帳紀錄");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = result;

                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OffsetService] 取得沖帳紀錄失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 反沖銷
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="request">反沖銷請求</param>
        /// <returns>響應對象</returns>
        public async Task<ResponseObject> RollbackOffset(long userId, RollbackOffsetRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 1. 用 offsetRecordIds 找到所有 offsetRecord
                if (request.offsetRecordIds == null || !request.offsetRecordIds.Any())
                {
                    responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                    return responseObject;
                }

                var offsetRecordIdParams = string.Join(",", request.offsetRecordIds);
                string getOffsetRecordsSql = $@"
                    SELECT id 
                    FROM offsetRecord 
                    WHERE id IN ({offsetRecordIdParams}) AND isDelete = 0";

                var validOffsetRecordIds = new List<long>();
                using (var getCommand = new SqlCommand(getOffsetRecordsSql, connection, transaction))
                {
                    using var reader = await getCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        validOffsetRecordIds.Add(reader.GetInt64(0));
                    }
                }

                // 2. 如果取出來的 offsetRecord ids 為空就直接返回成功
                if (!validOffsetRecordIds.Any())
                {
                    await transaction.CommitAsync();
                    responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                    return responseObject;
                }

                var validOffsetRecordIdParams = string.Join(",", validOffsetRecordIds);

                // 3. 更新 offsetRecord set isDelete=1
                string updateOffsetRecordSql = $@"
                    UPDATE offsetRecord 
                    SET isDelete = 1,
                        modifiedBy = @modifiedBy,
                        modifiedOn = GETDATE()
                    WHERE id IN ({validOffsetRecordIdParams})";

                using (var updateCommand = new SqlCommand(updateOffsetRecordSql, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                    await updateCommand.ExecuteNonQueryAsync();
                }

                // 4. 更新 offsetRecord_receipt set isDelete=1
                string updateOffsetRecordReceiptSql = $@"
                    UPDATE offsetRecord_receipt 
                    SET isDelete = 1,
                        modifiedBy = @modifiedBy,
                        modifiedOn = GETDATE()
                    WHERE offsetRecordId IN ({validOffsetRecordIdParams})";

                using (var updateCommand = new SqlCommand(updateOffsetRecordReceiptSql, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                    await updateCommand.ExecuteNonQueryAsync();
                }

                // 5. 更新 offsetRecord_shippingOrder set isDelete=1
                string updateOffsetRecordShippingOrderSql = $@"
                    UPDATE offsetRecord_shippingOrder 
                    SET isDelete = 1,
                        modifiedBy = @modifiedBy,
                        modifiedOn = GETDATE()
                    WHERE offsetRecordId IN ({validOffsetRecordIdParams})";

                using (var updateCommand = new SqlCommand(updateOffsetRecordShippingOrderSql, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                    await updateCommand.ExecuteNonQueryAsync();
                }

                // 6. 取出所有的 offsetRecord_shippingOrder 並更新 shippingOrder.offsetMoney
                string getOffsetRecordShippingOrdersSql = $@"
                    SELECT shippingOrderId, offsetMoney
                    FROM offsetRecord_shippingOrder
                    WHERE offsetRecordId IN ({validOffsetRecordIdParams})";

                var shippingOrderUpdates = new List<(long shippingOrderId, decimal offsetMoney)>();
                using (var getCommand = new SqlCommand(getOffsetRecordShippingOrdersSql, connection, transaction))
                {
                    using var reader = await getCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        long shippingOrderId = reader.GetInt64(0);
                        decimal offsetMoney = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                        shippingOrderUpdates.Add((shippingOrderId, offsetMoney));
                    }
                }

                // 針對每一筆 offsetRecord_shippingOrder.shippingOrderId 做更新
                foreach (var (shippingOrderId, offsetMoney) in shippingOrderUpdates)
                {
                    // 先查詢當前的 offsetMoney
                    string getCurrentOffsetMoneySql = @"
                        SELECT ISNULL(offsetMoney, 0)
                        FROM shippingOrder
                        WHERE id = @shippingOrderId";

                    decimal currentOffsetMoney = 0;
                    using (var getCommand = new SqlCommand(getCurrentOffsetMoneySql, connection, transaction))
                    {
                        getCommand.Parameters.AddWithValue("@shippingOrderId", shippingOrderId);
                        var result = await getCommand.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            currentOffsetMoney = Convert.ToDecimal(result);
                        }
                    }

                    // 計算新的 offsetMoney（原先的 - 本次反沖銷金額）
                    decimal newOffsetMoney = currentOffsetMoney - offsetMoney;

                    // 檢查結果是否小於 0
                    if (newOffsetMoney < 0)
                    {
                        await transaction.RollbackAsync();
                        throw new CustomErrorCodeException(
                            SelfErrorCode.ROLLBACK_OFFSET_NEGATIVE_AMOUNT,
                            $"反沖銷作業錯誤: 出貨單沖銷金額扣除反沖銷金額結果小於0，請聯絡工程團隊協助處理 (出貨單 ID: {shippingOrderId})",
                            shippingOrderId
                        );
                    }

                    // 更新 shippingOrder.offsetMoney
                    string updateShippingOrderOffsetMoneySql = @"
                        UPDATE shippingOrder
                        SET offsetMoney = @offsetMoney,
                            modifiedBy = @modifiedBy,
                            modifiedOn = GETDATE()
                        WHERE id = @shippingOrderId";

                    using (var updateCommand = new SqlCommand(updateShippingOrderOffsetMoneySql, connection, transaction))
                    {
                        updateCommand.Parameters.AddWithValue("@offsetMoney", newOffsetMoney);
                        updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                        updateCommand.Parameters.AddWithValue("@shippingOrderId", shippingOrderId);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[OffsetService] 成功反沖銷 {validOffsetRecordIds.Count} 筆沖帳紀錄");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = validOffsetRecordIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OffsetService] 反沖銷失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 取得客戶結餘
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含客戶結餘資訊</returns>
        public async Task<ResponseObject> GetCustomerBalance(GetCustomerBalanceRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 構建 SQL 查詢
                var sqlBuilder = new System.Text.StringBuilder(@"
                    SELECT 
                        customer.id as customerId,
                        customer.number as customerNumber,
                        customer.name as customerName,
                        customer.nickName as customerNickName,
                        SUM(ISNULL(offsetRecord.totalOffsetMoney, 0)) as totalOffsetMoney,
                        SUM(ISNULL(offsetRecord.shouldBeOffsetMoney, 0)) as shouldBeOffsetMoney,
                        SUM(ISNULL(offsetRecord.discount, 0)) as discount,
                        -- 公式：totalOffsetMoney - shouldBeOffsetMoney + discount
                        SUM(ISNULL(offsetRecord.totalOffsetMoney, 0)) - 
                        SUM(ISNULL(offsetRecord.shouldBeOffsetMoney, 0)) + 
                        SUM(ISNULL(offsetRecord.discount, 0)) as balance
                    FROM offsetRecord
                    INNER JOIN customer
                        ON offsetRecord.customerId = customer.id
                    WHERE offsetRecord.isDelete = 0");

                var parameters = new List<SqlParameter>();

                // 動態添加客戶 ID 條件（使用參數化查詢）
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) =>
                    {
                        var paramName = $"@customerId{index}";
                        parameters.Add(new SqlParameter(paramName, id));
                        return paramName;
                    }));
                    sqlBuilder.Append($" AND customer.id IN ({customerIdParams})");
                }

                sqlBuilder.Append(@"
                    GROUP BY 
                        customer.id,
                        customer.number,
                        customer.name,
                        customer.nickName
                    ORDER BY customer.number");

                var results = new List<Dictionary<string, object>>();
                using (var command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    using var reader = await command.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        row["customerId"] = reader.GetInt64(reader.GetOrdinal("customerId"));
                        row["customerNumber"] = reader.IsDBNull(reader.GetOrdinal("customerNumber")) ? null : reader.GetString(reader.GetOrdinal("customerNumber"));
                        row["customerName"] = reader.IsDBNull(reader.GetOrdinal("customerName")) ? null : reader.GetString(reader.GetOrdinal("customerName"));
                        row["customerNickName"] = reader.IsDBNull(reader.GetOrdinal("customerNickName")) ? null : reader.GetString(reader.GetOrdinal("customerNickName"));
                        row["totalOffsetMoney"] = reader.IsDBNull(reader.GetOrdinal("totalOffsetMoney")) ? 0 : reader.GetDecimal(reader.GetOrdinal("totalOffsetMoney"));
                        row["shouldBeOffsetMoney"] = reader.IsDBNull(reader.GetOrdinal("shouldBeOffsetMoney")) ? 0 : reader.GetDecimal(reader.GetOrdinal("shouldBeOffsetMoney"));
                        row["discount"] = reader.IsDBNull(reader.GetOrdinal("discount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("discount"));
                        row["balance"] = reader.IsDBNull(reader.GetOrdinal("balance")) ? 0 : reader.GetDecimal(reader.GetOrdinal("balance"));
                        
                        results.Add(row);
                    }
                }

                Debug.WriteLine($"[OffsetService] 取得 {results.Count} 筆客戶結餘資料");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = results;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OffsetService] 取得客戶結餘失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 生成沖帳編號
        /// 格式：民國年 + MMdd + 序列號（0001開始）
        /// 例如：offsetDate = '2025-01-02'，編號為 11401020001（114是民國年，0102是月日，0001是序列號）
        /// </summary>
        private async Task<string> GenerateOffsetNumber(
            SqlConnection connection,
            SqlTransaction transaction,
            string offsetDate)
        {
            if (!DateTime.TryParse(offsetDate, out DateTime date))
            {
                throw new CustomErrorCodeException(
                    SelfErrorCode.BAD_REQUEST,
                    $"offsetDate 格式不正確: {offsetDate}"
                );
            }

            // 使用 offsetDate 作為分組鍵（格式：yyyy-MM-dd）
            string groupKey = date.ToString("yyyy-MM-dd");

            // 生成序列號（格式：0001, 0002, 0003...）
            string sequenceNumber = await _sequenceService.GetNextNumberAsync(
                connection,
                transaction,
                tableName: "offsetRecord",
                groupKey: groupKey,
                numberFormat: "0000"
            );

            // 計算民國年（民國年 = 西元年 - 1911）
            int rocYear = date.Year - 1911;
            string monthDay = date.ToString("MMdd");
            
            // 組合編號：民國年 + MMdd + 序列號
            // 例如：114 + 0102 + 0001 = 11401020001
            string offsetNumber = $"{rocYear}{monthDay}{sequenceNumber}";

            Debug.WriteLine($"[OffsetService] 為日期 {groupKey} 生成沖帳編號: {offsetNumber}");

            return offsetNumber;
        }
    }
}

