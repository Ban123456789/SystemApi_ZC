using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Accura_MES.Services
{
    public class CustomerPriceService : ICustomerPriceService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;

        private CustomerPriceService(string connectionString, IGenericRepository genericRepository)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
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

            return new CustomerPriceService(connectionString, genericRepository);
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
                        productNumber = reader.IsDBNull(reader.GetOrdinal("productNumber")) ? null : reader.GetString(reader.GetOrdinal("productNumber"))
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
                        shippingOrder.isDelete = 0";

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

    }
}
