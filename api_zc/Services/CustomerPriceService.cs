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

    }
}
