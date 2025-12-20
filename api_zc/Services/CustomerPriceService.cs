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
    public class CustomerPriceService : ICustomerPriceService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;
        private ISequenceService _sequenceService;

        private CustomerPriceService(string connectionString, IGenericRepository genericRepository, ISequenceService sequenceService)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
            _sequenceService = sequenceService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 CustomerPriceService 實例
        /// </summary>
        /// <param name="connectionString">資料庫連線字串</param>
        /// <returns>CustomerPriceService 實例</returns>
        public static CustomerPriceService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);
            
            // 建立序列號服務
            ISequenceService sequenceService = SequenceService.CreateService(connectionString);

            return new CustomerPriceService(connectionString, genericRepository, sequenceService);
        }

        /// <summary>
        /// 建立客戶價格資料
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="customerPriceObject">客戶價格資料列表</param>
        /// <returns>響應對象，包含創建的客戶價格ID列表</returns>
        public async Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> customerPriceObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                for (int i = 0; i < customerPriceObject.Count; i++)
                {
                    var item = customerPriceObject[i];
                    
                    // 檢查必填欄位
                    var requiredFieldsError = ValidateRequiredFields(item, i);
                    if (requiredFieldsError != null)
                    {
                        await transaction.RollbackAsync();
                        return requiredFieldsError;
                    }

                    // 檢查是否存在相同的 customerId + projectId + productId
                    var duplicateError = await CheckDuplicateRecord(connection, transaction, item, i);
                    if (duplicateError != null)
                    {
                        await transaction.RollbackAsync();
                        return duplicateError;
                    }

                    // 設定預設值
                    SetDefaultValues(item, userId);
                }

                // 插入客戶價格數據
                var insertedIds = await _genericRepository.CreateDataGeneric(
                    connection, 
                    transaction, 
                    userId, 
                    "CustomerPrice", 
                    customerPriceObject
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[CustomerPriceService] 成功創建 {insertedIds.Count} 個客戶價格記錄");

                // 成功返回 ID 陣列
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = insertedIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 創建客戶價格失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 驗證必填欄位
        /// </summary>
        private ResponseObject? ValidateRequiredFields(Dictionary<string, object?> item, int index)
        {
            var requiredFields = new[] { "customerId", "projectId", "productId" };
            var missingFields = new List<string>();

            foreach (var field in requiredFields)
            {
                if (!item.ContainsKey(field) || item[field] == null)
                {
                    missingFields.Add(field);
                }
                else if (field == "customerId" || field == "projectId" || field == "productId")
                {
                    // 檢查數值是否大於0
                    if (long.TryParse(item[field]?.ToString(), out long value) && value <= 0)
                    {
                        missingFields.Add(field);
                    }
                }
            }

            if (missingFields.Any())
            {
                var validationError = new List<object>
                {
                    new
                    {
                        code = "400-7",
                        rowIndex = index,
                        missingFields,
                        message = $"第 {index + 1} 筆資料缺失必要欄位: {string.Join(", ", missingFields)}"
                    }
                };

                var responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.NESTED_STRUCTURE_ERROR);
                responseObject.ErrorData = validationError;
                responseObject.Message = "詳情請看error data";
                return responseObject;
            }

            return null;
        }

        /// <summary>
        /// 檢查重複記錄
        /// </summary>
        private async Task<ResponseObject?> CheckDuplicateRecord(SqlConnection connection, SqlTransaction transaction, 
            Dictionary<string, object?> item, int index)
        {
            try
            {
                var duplicateCheckQuery = @"
                    SELECT COUNT(1) 
                    FROM CustomerPrice 
                    WHERE CustomerId = @CustomerId 
                    AND ProjectId = @ProjectId 
                    AND ProductId = @ProductId 
                    AND IsDelete = 0";

                using var checkCommand = new SqlCommand(duplicateCheckQuery, connection, transaction);
                checkCommand.Parameters.AddWithValue("@CustomerId", Utils.ConvertToLong(item["customerId"]));
                checkCommand.Parameters.AddWithValue("@ProjectId", Utils.ConvertToLong(item["projectId"]));
                checkCommand.Parameters.AddWithValue("@ProductId", Utils.ConvertToLong(item["productId"]));

                var duplicateCount = (int)await checkCommand.ExecuteScalarAsync();

                if (duplicateCount > 0)
                {
                    var responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.DUPLICATE_CUSTOMER_PRICE);
                    return responseObject;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 檢查重複記錄失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 設定預設值
        /// </summary>
        private void SetDefaultValues(Dictionary<string, object?> item, long userId)
        {
            if (!item.ContainsKey("collapse"))
                item["collapse"] = 0;

            if (!item.ContainsKey("isDelete"))
                item["isDelete"] = false;


            // 設定系統欄位
            item["createdBy"] = userId;
            item["createdOn"] = DateTime.Now;
            item["modifiedBy"] = userId;
            item["modifiedOn"] = DateTime.Now;
        }

        /// <summary>
        /// 取得客戶別簡表
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含客戶別簡表資料</returns>
        public async Task<ResponseObject> GetAccountingByCustomer(GetAccountingByCustomerRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 建立動態 SQL 查詢
                var sqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        shippingOrder.id as shippingOrderId,
                        shippingOrder.outputMeters as outputMeters,
                        shippingOrder.remaining as remaining,
                        shippingOrder.price as price,
                        shippingOrder.type as type,
                        customer.id as customerId,
                        customer.number as customerNumber,
                        customer.nickName as customerNickName,
                        customer.phone as phone,
                        customer.address as customerAddress,
                        [order].id as orderId,
                        [order].shippedDate as shippedDate,
                        project.id as projectId,
                        project.number as projectNumber,
                        project.name as projectName,
                        project.address as address,
                        [product].id as productId,
                        [product].number as productNumber
                    FROM
                        shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    INNER JOIN [customer]
                        ON shippingOrder.customerId = customer.id
                    INNER JOIN project
                        ON shippingOrder.projectId = project.id
                    INNER JOIN [product]
                        ON shippingOrder.productId = [product].id
                    WHERE 
                        shippingOrder.isDelete = 0");

                var parameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (request.startShippedDate.HasValue)
                {
                    sqlBuilder.Append(" AND [order].shippedDate >= @startShippedDate");
                    parameters.Add(new SqlParameter("@startShippedDate", request.startShippedDate.Value));
                }

                if (request.endShippedDate.HasValue)
                {
                    sqlBuilder.Append(" AND [order].shippedDate <= @endShippedDate");
                    parameters.Add(new SqlParameter("@endShippedDate", request.endShippedDate.Value));
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) => $"@customerId{index}"));
                    sqlBuilder.Append($" AND customer.id IN ({customerIdParams})");
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId{i}", request.customerIds[i]));
                    }
                }

                sqlBuilder.Append(" ORDER BY [order].shippedDate DESC, customer.nickName, project.number");

                using var command = new SqlCommand(sqlBuilder.ToString(), connection);
                command.Parameters.AddRange(parameters.ToArray());

                var results = new List<GetAccountingByCustomerResponse>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new GetAccountingByCustomerResponse
                    {
                        shippingOrderId = reader.IsDBNull(reader.GetOrdinal("shippingOrderId")) ? null : reader.GetInt64(reader.GetOrdinal("shippingOrderId")),
                        outputMeters = reader.IsDBNull(reader.GetOrdinal("outputMeters")) ? 0 : reader.GetDecimal(reader.GetOrdinal("outputMeters")),
                        remaining = reader.IsDBNull(reader.GetOrdinal("remaining")) ? 0 : reader.GetDecimal(reader.GetOrdinal("remaining")),
                        price = reader.IsDBNull(reader.GetOrdinal("price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("price")),
                        customerId = reader.IsDBNull(reader.GetOrdinal("customerId")) ? null : reader.GetInt64(reader.GetOrdinal("customerId")),
                        customerNumber = reader.IsDBNull(reader.GetOrdinal("customerNumber")) ? null : reader.GetString(reader.GetOrdinal("customerNumber")),
                        customerNickName = reader.IsDBNull(reader.GetOrdinal("customerNickName")) ? null : reader.GetString(reader.GetOrdinal("customerNickName")),
                        phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                        customerAddress = reader.IsDBNull(reader.GetOrdinal("customerAddress")) ? null : reader.GetString(reader.GetOrdinal("customerAddress")),
                        orderId = reader.IsDBNull(reader.GetOrdinal("orderId")) ? null : reader.GetInt64(reader.GetOrdinal("orderId")),
                        shippedDate = reader.IsDBNull(reader.GetOrdinal("shippedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("shippedDate")).ToString("yyyy-MM-dd"),
                        projectId = reader.IsDBNull(reader.GetOrdinal("projectId")) ? null : reader.GetInt64(reader.GetOrdinal("projectId")),
                        projectNumber = reader.IsDBNull(reader.GetOrdinal("projectNumber")) ? null : reader.GetString(reader.GetOrdinal("projectNumber")),
                        projectName = reader.IsDBNull(reader.GetOrdinal("projectName")) ? null : reader.GetString(reader.GetOrdinal("projectName")),
                        address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                        productId = reader.IsDBNull(reader.GetOrdinal("productId")) ? null : reader.GetInt64(reader.GetOrdinal("productId")),
                        productNumber = reader.IsDBNull(reader.GetOrdinal("productNumber")) ? null : reader.GetString(reader.GetOrdinal("productNumber")),
                        type = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString(reader.GetOrdinal("type"))
                    });
                }

                Debug.WriteLine($"[CustomerPriceService] 成功取得 {results.Count} 筆客戶別簡表資料");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = results;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 取得客戶別簡表失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 出貨單轉應收帳款
        /// </summary>
        /// <param name="request">轉換請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns>響應對象，包含處理結果</returns>
        public async Task<ResponseObject> ConverToReceivable(ConverToReceivableRequest request, long userId)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 1. 查詢需要處理的 shippingOrders
                var shippingOrdersQuery = @"
                    SELECT
                        shippingOrder.id,
                        shippingOrder.customerId,
                        shippingOrder.projectId,
                        shippingOrder.productId
                    FROM
                        shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    INNER JOIN [customer]
                        ON shippingOrder.customerId = customer.id
                    INNER JOIN project
                        ON shippingOrder.projectId = project.id
                    WHERE 
                        shippingOrder.isDelete = 0
                        AND shippingOrder.type = '1'
                        AND shippingOrder.price = 0";

                var parameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (request.startShippedDate.HasValue)
                {
                    shippingOrdersQuery += " AND [order].shippedDate >= @startShippedDate";
                    parameters.Add(new SqlParameter("@startShippedDate", request.startShippedDate.Value));
                }

                if (request.endShippedDate.HasValue)
                {
                    shippingOrdersQuery += " AND [order].shippedDate <= @endShippedDate";
                    parameters.Add(new SqlParameter("@endShippedDate", request.endShippedDate.Value));
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) => $"@customerId{index}"));
                    shippingOrdersQuery += $" AND customer.id IN ({customerIdParams})";
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId{i}", request.customerIds[i]));
                    }
                }

                using var command = new SqlCommand(shippingOrdersQuery, connection, transaction);
                command.Parameters.AddRange(parameters.ToArray());

                var shippingOrdersToUpdate = new List<Dictionary<string, object>>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    shippingOrdersToUpdate.Add(new Dictionary<string, object>
                    {
                        ["id"] = reader.GetInt64(reader.GetOrdinal("id")),
                        ["customerId"] = reader.GetInt64(reader.GetOrdinal("customerId")),
                        ["projectId"] = reader.GetInt64(reader.GetOrdinal("projectId")),
                        ["productId"] = reader.GetInt64(reader.GetOrdinal("productId"))
                    });
                }

                reader.Close();

                Debug.WriteLine($"[CustomerPriceService] 找到 {shippingOrdersToUpdate.Count} 筆需要更新的出貨單");

                // 2. 針對每個 shippingOrder 查詢對應的 customerPrice 並更新
                int updatedCount = 0;
                int notFoundCount = 0;

                foreach (var shippingOrder in shippingOrdersToUpdate)
                {
                    var customerPriceQuery = @"
                        SELECT price 
                        FROM customerPrice 
                        WHERE isDelete = 0 
                        AND customerId = @customerId 
                        AND projectId = @projectId 
                        AND productId = @productId";

                    using var priceCommand = new SqlCommand(customerPriceQuery, connection, transaction);
                    priceCommand.Parameters.AddWithValue("@customerId", shippingOrder["customerId"]);
                    priceCommand.Parameters.AddWithValue("@projectId", shippingOrder["projectId"]);
                    priceCommand.Parameters.AddWithValue("@productId", shippingOrder["productId"]);

                    var priceResult = await priceCommand.ExecuteScalarAsync();

                    if (priceResult != null && priceResult != DBNull.Value)
                    {
                        // 找到對應的價格，更新 shippingOrder
                        var updateQuery = @"
                            UPDATE shippingOrder 
                            SET price = @price, 
                                modifiedOn = @modifiedOn,
                                modifiedBy = @modifiedBy
                            WHERE id = @id";

                        using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                        updateCommand.Parameters.AddWithValue("@price", priceResult);
                        updateCommand.Parameters.AddWithValue("@modifiedOn", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                        updateCommand.Parameters.AddWithValue("@id", shippingOrder["id"]);

                        await updateCommand.ExecuteNonQueryAsync();
                        updatedCount++;
                    }
                    else
                    {
                        notFoundCount++;
                        Debug.WriteLine($"[CustomerPriceService] 找不到客戶價格: CustomerId={shippingOrder["customerId"]}, ProjectId={shippingOrder["projectId"]}, ProductId={shippingOrder["productId"]}");
                    }
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[CustomerPriceService] 成功更新 {updatedCount} 筆出貨單價格，{notFoundCount} 筆找不到對應價格");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = new
                {
                    totalProcessed = shippingOrdersToUpdate.Count,
                    updatedCount = updatedCount,
                    notFoundCount = notFoundCount,
                    message = $"處理完成：共處理 {shippingOrdersToUpdate.Count} 筆，成功更新 {updatedCount} 筆，{notFoundCount} 筆找不到對應價格"
                };

                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 出貨單轉應收帳款失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 應收帳款退回
        /// </summary>
        /// <param name="request">退回請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns>響應對象，包含處理結果</returns>
        public async Task<ResponseObject> RollbackReceivable(RollbackReceivableRequest request, long userId)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 1. 查詢需要處理的 shippingOrders
                var shippingOrdersQuery = @"
                    SELECT
                        shippingOrder.id,
                        shippingOrder.customerId,
                        shippingOrder.projectId,
                        shippingOrder.productId,
                        shippingOrder.offsetMoney
                    FROM
                        shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    INNER JOIN [customer]
                        ON shippingOrder.customerId = customer.id
                    INNER JOIN project
                        ON shippingOrder.projectId = project.id
                    WHERE 
                        shippingOrder.isDelete = 0
                        AND shippingOrder.type = '1'";

                var parameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (request.startShippedDate.HasValue)
                {
                    shippingOrdersQuery += " AND [order].shippedDate >= @startShippedDate";
                    parameters.Add(new SqlParameter("@startShippedDate", request.startShippedDate.Value));
                }

                if (request.endShippedDate.HasValue)
                {
                    shippingOrdersQuery += " AND [order].shippedDate <= @endShippedDate";
                    parameters.Add(new SqlParameter("@endShippedDate", request.endShippedDate.Value));
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) => $"@customerId{index}"));
                    shippingOrdersQuery += $" AND customer.id IN ({customerIdParams})";
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId{i}", request.customerIds[i]));
                    }
                }

                using var command = new SqlCommand(shippingOrdersQuery, connection, transaction);
                command.Parameters.AddRange(parameters.ToArray());

                var shippingOrdersToUpdate = new List<Dictionary<string, object>>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // 檢查 offsetMoney 是否大於 0，如果大於 0 就直接返回錯誤
                    decimal offsetMoney = reader.IsDBNull(reader.GetOrdinal("offsetMoney")) ? 0 : reader.GetDecimal(reader.GetOrdinal("offsetMoney"));
                    
                    if (offsetMoney > 0)
                    {
                        reader.Close();
                        await transaction.RollbackAsync();
                        responseObject.SetErrorCode(SelfErrorCode.SHIPPING_ORDER_ALREADY_OFFSET_CANNOT_RECALCULATE);
                        return responseObject;
                    }
                    
                    shippingOrdersToUpdate.Add(new Dictionary<string, object>
                    {
                        ["id"] = reader.GetInt64(reader.GetOrdinal("id")),
                        ["customerId"] = reader.GetInt64(reader.GetOrdinal("customerId")),
                        ["projectId"] = reader.GetInt64(reader.GetOrdinal("projectId")),
                        ["productId"] = reader.GetInt64(reader.GetOrdinal("productId"))
                    });
                }

                reader.Close();

                Debug.WriteLine($"[CustomerPriceService] 找到 {shippingOrdersToUpdate.Count} 筆需要退回的出貨單");

                // 2. 批量更新所有 shippingOrder 的 price = 0
                int updatedCount = 0;

                if (shippingOrdersToUpdate.Any())
                {
                    // 建立 IN 條件用的 ID 列表
                    var shippingOrderIds = shippingOrdersToUpdate.Select(so => so["id"].ToString()).ToList();
                    var idParams = string.Join(",", shippingOrderIds.Select((id, index) => $"@id{index}"));

                    var updateQuery = $@"
                        UPDATE shippingOrder 
                        SET price = 0, 
                            modifiedOn = @modifiedOn,
                            modifiedBy = @modifiedBy
                        WHERE id IN ({idParams})";

                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@modifiedOn", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@modifiedBy", userId);
                    
                    // 添加所有 ID 參數
                    for (int i = 0; i < shippingOrderIds.Count; i++)
                    {
                        updateCommand.Parameters.AddWithValue($"@id{i}", shippingOrderIds[i]);
                    }

                    updatedCount = await updateCommand.ExecuteNonQueryAsync();
                }

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[CustomerPriceService] 成功退回 {updatedCount} 筆出貨單價格");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = new
                {
                    totalProcessed = shippingOrdersToUpdate.Count,
                    updatedCount = updatedCount,
                    message = $"退回完成：共處理 {shippingOrdersToUpdate.Count} 筆，成功退回 {updatedCount} 筆"
                };

                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 應收帳款退回失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 建立應收帳款
        /// </summary>
        /// <param name="request">建立應收帳款請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns>響應對象，包含創建的出貨單ID列表</returns>
        public async Task<ResponseObject> CreateReceivable(CreateReceivableRequest request, long userId)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                // 檢查 Body
                if (request == null)
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "建立應收帳款請求不能為空";
                    return responseObject;
                }

                if (request.order == null || !request.order.Any())
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "訂單資料不能為空";
                    return responseObject;
                }

                if (request.shippingOrder == null || !request.shippingOrder.Any())
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "出貨單資料不能為空";
                    return responseObject;
                }

                // 創建統一的連接和事務
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 1. 建立 order
                // 1.1 為訂單生成編號
                await GenerateOrderNumber(connection, transaction, request.order);

                // 1.2 插入訂單數據
                List<Dictionary<string, object?>> orderList = new List<Dictionary<string, object?>> { request.order };
                var insertedOrderIds = await _genericRepository.CreateDataGeneric(
                    connection,
                    transaction,
                    userId,
                    "order",
                    orderList
                );

                if (insertedOrderIds == null || !insertedOrderIds.Any())
                {
                    await transaction.RollbackAsync();
                    responseObject.SetErrorCode(SelfErrorCode.INTERNAL_SERVER_ERROR);
                    responseObject.Message = "建立訂單失敗：無法獲取訂單ID";
                    return responseObject;
                }

                long orderId = insertedOrderIds.First();

                // 2. 將 orderId 寫入到 shippingOrder.orderId
                Dictionary<string, object?> shippingOrderCopy = new Dictionary<string, object?>(request.shippingOrder);
                shippingOrderCopy["orderId"] = orderId;

                // 3. 建立 shippingOrder
                // 3.1 為出貨單生成編號
                await GenerateShippingOrderNumber(connection, transaction, shippingOrderCopy);

                // 3.2 為出貨單生成 carIndex
                await GenerateCarIndex(connection, transaction, shippingOrderCopy);

                // 3.3 計算 outputTotalMeters 和 actualTotalMeters
                await CalculateTotalMeters(connection, transaction, shippingOrderCopy);

                // 3.4 插入出貨單數據
                List<Dictionary<string, object?>> shippingOrderList = new List<Dictionary<string, object?>> { shippingOrderCopy };
                var insertedShippingOrderIds = await _genericRepository.CreateDataGeneric(
                    connection,
                    transaction,
                    userId,
                    "shippingOrder",
                    shippingOrderList
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[CustomerPriceService] 成功建立應收帳款：orderId={orderId}, shippingOrderId={insertedShippingOrderIds.FirstOrDefault()}");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = insertedShippingOrderIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 建立應收帳款失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 為訂單生成編號
        /// </summary>
        private async Task GenerateOrderNumber(
            SqlConnection connection,
            SqlTransaction transaction,
            Dictionary<string, object?> order)
        {
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

            Debug.WriteLine($"[CustomerPriceService] 為日期 {groupKey} 生成訂單編號: {orderNumber}");
        }

        /// <summary>
        /// 為出貨單生成編號
        /// </summary>
        private async Task GenerateShippingOrderNumber(
            SqlConnection connection,
            SqlTransaction transaction,
            Dictionary<string, object?> shippingOrder)
        {
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

            Debug.WriteLine($"[CustomerPriceService] 為 orderId={orderId}, 日期 {groupKey} 生成出貨單編號: {shippingOrderNumber}");
        }

        /// <summary>
        /// 為出貨單生成 carIndex
        /// </summary>
        private async Task GenerateCarIndex(
            SqlConnection connection,
            SqlTransaction transaction,
            Dictionary<string, object?> shippingOrder)
        {
            var orderIdValue = shippingOrder.GetValueOrDefault("orderId");
            if (orderIdValue == null)
            {
                throw new CustomErrorCodeException(
                    SelfErrorCode.BAD_REQUEST,
                    "orderId 不能為空"
                );
            }

            long orderId = Convert.ToInt64(orderIdValue);

            // 查詢該 orderId 在數據庫中已存在的最大 carIndex
            string querySql = @"
                SELECT ISNULL(MAX(carIndex), 0) AS maxCarIndex
                FROM shippingOrder
                WHERE orderId = @OrderId AND type='1' AND isDelete = 0";

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

            // 生成新的 carIndex
            int newCarIndex = maxCarIndex + 1;
            shippingOrder["carIndex"] = newCarIndex;

            Debug.WriteLine($"[CustomerPriceService] 為 orderId={orderId} 生成 carIndex={newCarIndex}");
        }

        /// <summary>
        /// 計算出貨單的 outputTotalMeters 和 actualTotalMeters
        /// </summary>
        private async Task CalculateTotalMeters(
            SqlConnection connection,
            SqlTransaction transaction,
            Dictionary<string, object?> shippingOrder)
        {
            var orderIdValue = shippingOrder.GetValueOrDefault("orderId");
            if (orderIdValue == null)
            {
                throw new CustomErrorCodeException(
                    SelfErrorCode.BAD_REQUEST,
                    "orderId 不能為空"
                );
            }

            long orderId = Convert.ToInt64(orderIdValue);

            // 1. 查詢數據庫中已存在的相同 orderId 的統計數據
            string querySql = @"
                SELECT 
                    ISNULL(SUM(outputMeters), 0) AS totalOutputMeters,
                    ISNULL(SUM(outputMeters - returnMeters), 0) AS totalActualMeters
                FROM shippingOrder
                WHERE orderId = @OrderId AND type='1' AND isDelete = 0";

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

            // 2. 獲取當前出貨單的 outputMeters 和 returnMeters
            decimal outputMeters = GetDecimalValue(shippingOrder, "outputMeters");
            decimal returnMeters = GetDecimalValue(shippingOrder, "returnMeters");

            // 3. 計算包含當前記錄的總計
            decimal outputTotalMeters = existingOutputTotal + outputMeters;
            decimal actualTotalMeters = existingActualTotal + (outputMeters - returnMeters);

            // 4. 寫入計算結果
            shippingOrder["outputTotalMeters"] = outputTotalMeters;
            shippingOrder["actualTotalMeters"] = actualTotalMeters;

            Debug.WriteLine($"[CustomerPriceService] orderId={orderId} 計算完成: outputMeters={outputMeters}, returnMeters={returnMeters}, outputTotalMeters={outputTotalMeters}, actualTotalMeters={actualTotalMeters}");
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
        /// 取得應收帳款清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含應收帳款清單資料</returns>
        public async Task<ResponseObject> GetReceivableList(GetReceivableListRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 建立 SQL 查詢
                var sqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        shippingOrder.id as id,
                        shippingOrder.number as number,
                        shippingOrder.customerId as customerId,
                        shippingOrder.projectId as projectId,
                        shippingOrder.productId as productId,
                        shippingOrder.orderId as orderId,
                        shippingOrder.type as type,
                        shippingOrder.price as price,
                        [order].shippedDate as shippedDate,
                        customer.number as customerNumber,
                        customer.name as customerName,
                        project.number as projectNumber,
                        project.name as projectName,
                        [product].number as productNumber
                    FROM
                        shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    INNER JOIN project
                        ON shippingOrder.projectId = project.id
                    INNER JOIN [product]
                        ON shippingOrder.productId = [product].id
                    INNER JOIN customer
                        ON shippingOrder.customerId = customer.id
                    WHERE
                        shippingOrder.isDelete = 0
                        AND shippingOrder.type = '2'");

                var parameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (!string.IsNullOrEmpty(request.shippedDateStart))
                {
                    if (DateTime.TryParse(request.shippedDateStart, out DateTime shippedDateStart))
                    {
                        sqlBuilder.Append(" AND [order].shippedDate >= @shippedDateStart");
                        parameters.Add(new SqlParameter("@shippedDateStart", shippedDateStart.Date));
                    }
                }

                if (!string.IsNullOrEmpty(request.shippedDateEnd))
                {
                    if (DateTime.TryParse(request.shippedDateEnd, out DateTime shippedDateEnd))
                    {
                        // 結束日期需要包含當天的所有時間，所以加上 23:59:59
                        sqlBuilder.Append(" AND [order].shippedDate <= @shippedDateEnd");
                        parameters.Add(new SqlParameter("@shippedDateEnd", shippedDateEnd.Date.AddDays(1).AddSeconds(-1)));
                    }
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) => $"@customerId{index}"));
                    sqlBuilder.Append($" AND shippingOrder.customerId IN ({customerIdParams})");
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId{i}", request.customerIds[i]));
                    }
                }

                // 動態添加出貨單 ID 條件
                if (request.ids != null && request.ids.Any())
                {
                    var idParams = string.Join(",", request.ids.Select((id, index) => $"@id{index}"));
                    sqlBuilder.Append($" AND shippingOrder.id IN ({idParams})");
                    
                    for (int i = 0; i < request.ids.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@id{i}", request.ids[i]));
                    }
                }

                using var command = new SqlCommand(sqlBuilder.ToString(), connection);
                command.Parameters.AddRange(parameters.ToArray());

                var results = new List<Dictionary<string, object>>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    row["id"] = reader.IsDBNull(reader.GetOrdinal("id")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("id"));
                    row["number"] = reader.IsDBNull(reader.GetOrdinal("number")) ? null : reader.GetString(reader.GetOrdinal("number"));
                    row["customerId"] = reader.IsDBNull(reader.GetOrdinal("customerId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("customerId"));
                    row["projectId"] = reader.IsDBNull(reader.GetOrdinal("projectId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("projectId"));
                    row["productId"] = reader.IsDBNull(reader.GetOrdinal("productId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("productId"));
                    row["orderId"] = reader.IsDBNull(reader.GetOrdinal("orderId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("orderId"));
                    row["type"] = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString(reader.GetOrdinal("type"));
                    row["price"] = reader.IsDBNull(reader.GetOrdinal("price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("price"));
                    
                    // 格式化日期為 yyyy-MM-dd
                    if (!reader.IsDBNull(reader.GetOrdinal("shippedDate")))
                    {
                        DateTime shippedDate = reader.GetDateTime(reader.GetOrdinal("shippedDate"));
                        row["shippedDate"] = shippedDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        row["shippedDate"] = null;
                    }
                    
                    row["customerNumber"] = reader.IsDBNull(reader.GetOrdinal("customerNumber")) ? null : reader.GetString(reader.GetOrdinal("customerNumber"));
                    row["customerName"] = reader.IsDBNull(reader.GetOrdinal("customerName")) ? null : reader.GetString(reader.GetOrdinal("customerName"));
                    row["projectNumber"] = reader.IsDBNull(reader.GetOrdinal("projectNumber")) ? null : reader.GetString(reader.GetOrdinal("projectNumber"));
                    row["projectName"] = reader.IsDBNull(reader.GetOrdinal("projectName")) ? null : reader.GetString(reader.GetOrdinal("projectName"));
                    row["productNumber"] = reader.IsDBNull(reader.GetOrdinal("productNumber")) ? null : reader.GetString(reader.GetOrdinal("productNumber"));
                    
                    results.Add(row);
                }

                Debug.WriteLine($"[CustomerPriceService] 成功取得 {results.Count} 筆應收帳款清單資料");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = results;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 取得應收帳款清單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

        /// <summary>
        /// 刪除應收帳款
        /// </summary>
        /// <param name="request">刪除請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns>響應對象，包含處理結果</returns>
        public async Task<ResponseObject> DeleteReceivable(DeleteReceivableRequest request, long userId)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                // 檢查 Body
                if (request == null)
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "刪除請求不能為空";
                    return responseObject;
                }

                if (request.ids == null || !request.ids.Any())
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "要刪除的 ID 列表不能為空";
                    return responseObject;
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 檢查該出貨單是否已沖帳
                var idParams = string.Join(",", request.ids);
                string checkOffsetSql = $@"
                    SELECT 
                        shippingOrder.id as id,
                        shippingOrder.number as number,
                        [order].shippedDate as shippedDate
                    FROM shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    WHERE
                        shippingOrder.id IN ({idParams})
                        AND shippingOrder.type = '2'
                        AND (
                            (ISNULL(shippingOrder.price, 0) > 0 AND ISNULL(shippingOrder.offsetMoney, 0) > 0)
                            OR
                            (ISNULL(shippingOrder.price, 0) <= 0 
                                AND EXISTS (
                                    SELECT 1 
                                    FROM offsetRecord_shippingOrder
                                    WHERE offsetRecord_shippingOrder.shippingOrderId = shippingOrder.id
                                        AND offsetRecord_shippingOrder.isDelete = 0
                                )
                            )
                        )
                    ";

                var offsetShippingOrders = new List<Dictionary<string, object>>();
                using (var checkCommand = new SqlCommand(checkOffsetSql, connection, transaction))
                {
                    using var reader = await checkCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var shippingOrder = new Dictionary<string, object>();
                        shippingOrder["id"] = reader.GetInt64(reader.GetOrdinal("id"));
                        shippingOrder["number"] = reader.IsDBNull(reader.GetOrdinal("number")) ? null : reader.GetString(reader.GetOrdinal("number"));
                        
                        // 格式化日期為 yyyy-MM-dd
                        if (!reader.IsDBNull(reader.GetOrdinal("shippedDate")))
                        {
                            DateTime shippedDate = reader.GetDateTime(reader.GetOrdinal("shippedDate"));
                            shippingOrder["shippedDate"] = shippedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            shippingOrder["shippedDate"] = null;
                        }
                        
                        offsetShippingOrders.Add(shippingOrder);
                    }
                }

                // 如果取出來的筆數 > 0，將資料放到 errorData 並返回錯誤
                if (offsetShippingOrders.Any())
                {
                    await transaction.RollbackAsync();
                    responseObject.SetErrorCode(SelfErrorCode.RECEIVABLE_ALREADY_OFFSET_CANNOT_DELETE);
                    responseObject.ErrorData = offsetShippingOrders;
                    return responseObject;
                }

                // 將這些出貨單標記為已刪除（isDelete = 1）
                var idParamsForDelete = string.Join(",", request.ids.Select((id, index) => $"@id{index}"));
                string deleteSql = $@"
                    UPDATE shippingOrder 
                    SET isDelete = 1, 
                        modifiedBy = @UserId, 
                        modifiedOn = GETDATE()
                    WHERE id IN ({idParamsForDelete})
                    AND type = '2'
                    AND isDelete = 0";

                using var deleteCommand = new SqlCommand(deleteSql, connection, transaction);
                deleteCommand.Parameters.AddWithValue("@UserId", userId);
                
                for (int i = 0; i < request.ids.Count; i++)
                {
                    deleteCommand.Parameters.AddWithValue($"@id{i}", request.ids[i]);
                }

                int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[CustomerPriceService] 成功刪除 {rowsAffected} 筆應收帳款");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = new
                {
                    totalRequested = request.ids.Count,
                    deletedCount = rowsAffected,
                    message = $"刪除完成：共請求 {request.ids.Count} 筆，成功刪除 {rowsAffected} 筆"
                };

                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 刪除應收帳款失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 更新應收帳款
        /// </summary>
        /// <param name="request">更新請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns>響應對象</returns>
        public async Task<ResponseObject> UpdateReceivable(CreateReceivableRequest request, long userId)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                // 檢查 Body
                if (request == null)
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "更新請求不能為空";
                    return responseObject;
                }

                if (request.order == null || !request.order.Any())
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "訂單資料不能為空";
                    return responseObject;
                }

                if (request.shippingOrder == null || !request.shippingOrder.Any())
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "出貨單資料不能為空";
                    return responseObject;
                }

                // 檢查 order 和 shippingOrder 是否包含 id
                if (!request.order.ContainsKey("id") || request.order["id"] == null)
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "訂單資料必須包含 id";
                    return responseObject;
                }

                if (!request.shippingOrder.ContainsKey("id") || request.shippingOrder["id"] == null)
                {
                    responseObject.SetErrorCode(SelfErrorCode.MISSING_PARAMETERS);
                    responseObject.Message = "出貨單資料必須包含 id";
                    return responseObject;
                }

                // 創建統一的連接和事務
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 0. 檢查出貨單是否已沖帳過
                string checkOffsetSql = $@"
                    SELECT 
                        shippingOrder.id as id,
                        shippingOrder.number as number,
                        [order].shippedDate as shippedDate
                    FROM shippingOrder
                    INNER JOIN [order]
                        ON shippingOrder.orderId = [order].id
                    WHERE 
                        shippingOrder.id = {request.shippingOrder["id"]}
                        AND shippingOrder.type = '2'
                        AND (
                            (ISNULL(shippingOrder.price, 0) > 0 AND ISNULL(shippingOrder.offsetMoney, 0) > 0)
                            OR
                            (ISNULL(shippingOrder.price, 0) <= 0 
                                AND EXISTS (
                                    SELECT 1 
                                    FROM offsetRecord_shippingOrder
                                    WHERE offsetRecord_shippingOrder.shippingOrderId = shippingOrder.id
                                        AND offsetRecord_shippingOrder.isDelete = 0
                                )
                            )
                        )";

                var offsetShippingOrders = new List<Dictionary<string, object>>();
                using (var checkCommand = new SqlCommand(checkOffsetSql, connection, transaction))
                {
                    using var reader = await checkCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var shippingOrder = new Dictionary<string, object>();
                        shippingOrder["id"] = reader.GetInt64(reader.GetOrdinal("id"));
                        shippingOrder["number"] = reader.IsDBNull(reader.GetOrdinal("number")) ? null : reader.GetString(reader.GetOrdinal("number"));
                        
                        // 格式化日期為 yyyy-MM-dd
                        if (!reader.IsDBNull(reader.GetOrdinal("shippedDate")))
                        {
                            DateTime shippedDate = reader.GetDateTime(reader.GetOrdinal("shippedDate"));
                            shippingOrder["shippedDate"] = shippedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            shippingOrder["shippedDate"] = null;
                        }
                        
                        offsetShippingOrders.Add(shippingOrder);
                    }
                }

                // 如果取出來的筆數 > 0，將資料放到 errorData 並返回錯誤
                if (offsetShippingOrders.Any())
                {
                    await transaction.RollbackAsync();
                    responseObject.SetErrorCode(SelfErrorCode.RECEIVABLE_ALREADY_OFFSET_CANNOT_EDIT);
                    responseObject.ErrorData = offsetShippingOrders;
                    return responseObject;
                }

                // 1. 更新 order
                // 轉換 Dictionary<string, object?> 為 Dictionary<string, object>
                Dictionary<string, object> orderDict = new Dictionary<string, object>();
                foreach (var kvp in request.order)
                {
                    orderDict[kvp.Key] = kvp.Value ?? (object)DBNull.Value;
                }

                TableDatas orderTableDatas = new TableDatas();
                orderTableDatas.Datasheet = "order";
                orderTableDatas.DataStructure = new List<Dictionary<string, object>> { orderDict };
                
                await _genericRepository.GenericUpdate(
                    userId,
                    orderTableDatas,
                    connection,
                    transaction
                );

                // 2. 更新 shippingOrder
                // 轉換 Dictionary<string, object?> 為 Dictionary<string, object>
                Dictionary<string, object> shippingOrderDict = new Dictionary<string, object>();
                foreach (var kvp in request.shippingOrder)
                {
                    shippingOrderDict[kvp.Key] = kvp.Value ?? (object)DBNull.Value;
                }

                TableDatas shippingOrderTableDatas = new TableDatas();
                shippingOrderTableDatas.Datasheet = "shippingOrder";
                shippingOrderTableDatas.DataStructure = new List<Dictionary<string, object>> { shippingOrderDict };
                
                await _genericRepository.GenericUpdate(
                    userId,
                    shippingOrderTableDatas,
                    connection,
                    transaction
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[CustomerPriceService] 成功更新應收帳款：orderId={request.order["id"]}, shippingOrderId={request.shippingOrder["id"]}");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = "更新成功";
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 更新應收帳款失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 取得客戶應收統計
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含客戶應收統計資訊</returns>
        public async Task<ResponseObject> CustomerReceivables(CustomerReceivablesRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 解析日期
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (!string.IsNullOrEmpty(request.shippedDateStart))
                {
                    if (DateTime.TryParse(request.shippedDateStart, out DateTime parsedStartDate))
                    {
                        startDate = parsedStartDate.Date;
                    }
                }

                if (!string.IsNullOrEmpty(request.shippedDateEnd))
                {
                    if (DateTime.TryParse(request.shippedDateEnd, out DateTime parsedEndDate))
                    {
                        endDate = parsedEndDate.Date.AddDays(1).AddSeconds(-1);
                    }
                }

                // 構建 SQL 查詢
                var sqlBuilder = new System.Text.StringBuilder(@"
WITH
/** 顧客時間區間內總出貨米數 */
customerShippingOrder AS (
    SELECT
        shippingOrder.customerId AS customerId,
        SUM(shippingOrder.remaining) AS totalOutputMeters,
        SUM(
            CASE 
                WHEN [product].number = '水車' THEN ISNULL(shippingOrder.price, 0)
                ELSE 0
            END
        ) AS waterCarTotalPrice,
        SUM(
            CASE 
                WHEN shippingOrder.type = 1 THEN
                    CASE 
                        WHEN [product].number = '水車' THEN 1 * ISNULL(shippingOrder.price, 0)
                        ELSE ISNULL(shippingOrder.remaining, 0) * ISNULL(shippingOrder.price, 0)
                    END
                ELSE 0
            END
        ) AS totalShippingPrice,
        SUM(
            CASE 
                WHEN shippingOrder.type = 1 THEN
                    CASE 
                        WHEN [product].number = '水車' THEN 1 * ISNULL(shippingOrder.price, 0)
                        ELSE ISNULL(shippingOrder.remaining, 0) * ISNULL(shippingOrder.price, 0)
                    END
                WHEN shippingOrder.type = 2 THEN
                    ISNULL(shippingOrder.price, 0)
                ELSE 0
            END
        ) AS totalPrice
    FROM shippingOrder
    INNER JOIN [order]
        ON shippingOrder.orderId = [order].id
    INNER JOIN [product]
        ON shippingOrder.productId = [product].id
    WHERE
        shippingOrder.isDelete = 0");

                var parameters = new List<SqlParameter>();

                // 動態添加日期條件
                if (startDate.HasValue)
                {
                    sqlBuilder.Append(" AND [order].shippedDate >= @startShippedDate");
                    parameters.Add(new SqlParameter("@startShippedDate", startDate.Value));
                }

                if (endDate.HasValue)
                {
                    sqlBuilder.Append(" AND [order].shippedDate <= @endShippedDate");
                    parameters.Add(new SqlParameter("@endShippedDate", endDate.Value));
                }

                // 動態添加客戶 ID 條件到第一個 CTE
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) => $"@customerId{index}"));
                    sqlBuilder.Append($" AND shippingOrder.customerId IN ({customerIdParams})");
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId{i}", request.customerIds[i]));
                    }
                }

                sqlBuilder.Append(@"
    GROUP BY
        shippingOrder.customerId
),
/** 顧客上期應收 (不分時間區間，只要有未沖帳的出貨單就加總) */
customerUnPay AS (
    SELECT
        shippingOrder.customerId AS customerId,
        SUM(
            CASE 
                WHEN shippingOrder.type = 1 THEN
                    CASE 
                        WHEN [product].number = '水車' THEN 1 * ISNULL(shippingOrder.price, 0)
                        ELSE ISNULL(shippingOrder.remaining, 0) * ISNULL(shippingOrder.price, 0)
                    END
                WHEN shippingOrder.type = 2 THEN
                    ISNULL(shippingOrder.price, 0)
                ELSE 0
            END
        ) AS totalUnpaid
    FROM shippingOrder
    INNER JOIN [product]
        ON shippingOrder.productId = [product].id
    WHERE
        shippingOrder.isDelete = 0
        AND shippingOrder.offsetMoney = 0");

                // 動態添加客戶 ID 條件到第二個 CTE
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams2 = string.Join(",", request.customerIds.Select((id, index) => $"@customerId2_{index}"));
                    sqlBuilder.Append($" AND shippingOrder.customerId IN ({customerIdParams2})");
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId2_{i}", request.customerIds[i]));
                    }
                }

                sqlBuilder.Append(@"
    GROUP BY 
        shippingOrder.customerId
)
SELECT
    customer.id AS customerId,
    customer.name AS customerName,
    customer.number AS customerNumber,
    customer.nickName AS customerNickName,
    ISNULL(customerShippingOrder.totalOutputMeters, 0) AS totalOutputMeters,
    ISNULL(customerShippingOrder.waterCarTotalPrice, 0) AS waterCarTotalPrice,
    ISNULL(customerShippingOrder.totalShippingPrice, 0) AS totalShippingPrice,
    ISNULL(customerShippingOrder.totalPrice, 0) AS totalPrice,
    ISNULL(customerUnPay.totalUnpaid, 0) AS totalUnpaid
FROM customerShippingOrder
INNER JOIN customer
    ON customerShippingOrder.customerId = customer.id
LEFT JOIN customerUnPay
    ON customerShippingOrder.customerId = customerUnPay.customerId");

                // 可選：在主查詢中也添加客戶 ID 過濾（確保數據一致性）
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams3 = string.Join(",", request.customerIds.Select((id, index) => $"@customerId3_{index}"));
                    sqlBuilder.Append($" WHERE customer.id IN ({customerIdParams3})");
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId3_{i}", request.customerIds[i]));
                    }
                }

                var results = new List<Dictionary<string, object>>();
                using (var command = new SqlCommand(sqlBuilder.ToString(), connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    using var reader = await command.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        row["customerId"] = reader.GetInt64(reader.GetOrdinal("customerId"));
                        row["customerName"] = reader.IsDBNull(reader.GetOrdinal("customerName")) ? null : reader.GetString(reader.GetOrdinal("customerName"));
                        row["customerNumber"] = reader.IsDBNull(reader.GetOrdinal("customerNumber")) ? null : reader.GetString(reader.GetOrdinal("customerNumber"));
                        row["customerNickName"] = reader.IsDBNull(reader.GetOrdinal("customerNickName")) ? null : reader.GetString(reader.GetOrdinal("customerNickName"));
                        row["totalOutputMeters"] = reader.IsDBNull(reader.GetOrdinal("totalOutputMeters")) ? 0 : reader.GetDecimal(reader.GetOrdinal("totalOutputMeters"));
                        row["waterCarTotalPrice"] = reader.IsDBNull(reader.GetOrdinal("waterCarTotalPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("waterCarTotalPrice"));
                        row["totalShippingPrice"] = reader.IsDBNull(reader.GetOrdinal("totalShippingPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("totalShippingPrice"));
                        row["totalPrice"] = reader.IsDBNull(reader.GetOrdinal("totalPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("totalPrice"));
                        row["totalUnpaid"] = reader.IsDBNull(reader.GetOrdinal("totalUnpaid")) ? 0 : reader.GetDecimal(reader.GetOrdinal("totalUnpaid"));
                        
                        results.Add(row);
                    }
                }

                Debug.WriteLine($"[CustomerPriceService] 取得 {results.Count} 筆客戶應收統計資料");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = results;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomerPriceService] 取得客戶應收統計失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }

    }
}
