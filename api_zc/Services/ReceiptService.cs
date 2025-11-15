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
    public class ReceiptService : IReceiptService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;
        private ISequenceService _sequenceService;

        private ReceiptService(string connectionString, IGenericRepository genericRepository, ISequenceService sequenceService)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
            _sequenceService = sequenceService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 ReceiptService 實例，並接收外部資料庫連線
        /// </summary>
        /// <param name="connectionString">資料庫連線字串</param>
        /// <returns>ReceiptService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static ReceiptService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);
            
            // 建立序列號服務
            ISequenceService sequenceService = SequenceService.CreateService(connectionString);

            // 接收外部資料庫連線
            return new ReceiptService(connectionString, genericRepository, sequenceService);
        }

        /// <summary>
        /// 創建收款單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="receiptObject">收款單資料列表</param>
        /// <returns>響應對象，包含創建的收款單ID列表</returns>
        public async Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> receiptObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 為每個收款單生成編號
                await GenerateReceiptNumbers(connection, transaction, receiptObject);

                // 插入收款單數據
                var insertedIds = await _genericRepository.CreateDataGeneric(
                    connection, 
                    transaction, 
                    userId, 
                    "receipt", 
                    receiptObject
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[ReceiptService] 成功創建 {insertedIds.Count} 個收款單");

                // 成功返回，直接返回ID列表
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = insertedIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReceiptService] 創建收款單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 為收款單生成編號
        /// 規則：根據 receiptDate 分組，格式 0001, 0002, 0003...
        /// </summary>
        private async Task GenerateReceiptNumbers(
            SqlConnection connection, 
            SqlTransaction transaction, 
            List<Dictionary<string, object?>> receiptObject)
        {
            foreach (var receipt in receiptObject)
            {
                // 獲取收款日期作為分組鍵
                var receiptDate = receipt.GetValueOrDefault("receiptDate");
                
                if (receiptDate == null)
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        "receiptDate 不能為空"
                    );
                }

                // 將日期轉換為字符串作為分組鍵
                string groupKey;
                
                // 處理 JsonElement 類型（ASP.NET Core JSON 反序列化）
                if (receiptDate is JsonElement jsonElement)
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
                                $"receiptDate 格式不正確: {dateString}"
                            );
                        }
                    }
                    else
                    {
                        throw new CustomErrorCodeException(
                            SelfErrorCode.BAD_REQUEST, 
                            $"receiptDate 必須是日期字符串，當前類型: {jsonElement.ValueKind}"
                        );
                    }
                }
                else if (receiptDate is DateTime dateValue)
                {
                    groupKey = dateValue.ToString("yyyy-MM-dd");
                }
                else if (receiptDate is string dateString)
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
                            $"receiptDate 格式不正確: {dateString}"
                        );
                    }
                }
                else
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        $"receiptDate 類型不正確，當前類型: {receiptDate.GetType().Name}"
                    );
                }

                // 生成收款單編號（格式：0001, 0002, 0003...）
                string receiptNumber = await _sequenceService.GetNextNumberAsync(
                    connection,
                    transaction,
                    tableName: "receipt",
                    groupKey: groupKey,
                    numberFormat: "0000"  // 4位數格式
                );

                // 將生成的編號寫入數據
                receipt["number"] = receiptNumber;

                Debug.WriteLine($"[ReceiptService] 為日期 {groupKey} 生成收款單編號: {receiptNumber}");
            }
        }

        /// <summary>
        /// 取得收款單清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含收款單清單資料</returns>
        public async Task<ResponseObject> GetReceiptList(GetReceiptListRequest request)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 建立 SQL 查詢
                var sqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        receipt.id as id,
                        receipt.number as number,
                        receipt.receiptDate as receiptDate,
                        receipt.customerId as customerId,
                        customer.nickName as customerNickName,
                        receipt.paymentDate as paymentDate,
                        receipt.paymentType as paymentType,
                        receipt.price as price,
                        offsetRecord.id as offsetRecordId,
                        ISNULL(offsetRecord.shouldBeOffsetMoney, 0) as shouldBeOffsetMoney
                    FROM
                        receipt
                    LEFT JOIN customer
                        ON receipt.customerId = customer.id
                    LEFT JOIN offsetRecord_receipt
                        ON receipt.id = offsetRecord_receipt.receiptId AND offsetRecord_receipt.isDelete = 0
                    LEFT JOIN offsetRecord
                        ON offsetRecord.id = offsetRecord_receipt.offsetRecordId AND offsetRecord.isDelete = 0
                    WHERE
                        receipt.isDelete = 0");

                var parameters = new List<SqlParameter>();

                // 動態添加收款單 ID 條件
                if (request.ids != null && request.ids.Any())
                {
                    var idParams = string.Join(",", request.ids.Select((id, index) => $"@id{index}"));
                    sqlBuilder.Append($" AND receipt.id IN ({idParams})");
                    
                    for (int i = 0; i < request.ids.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@id{i}", request.ids[i]));
                    }
                }

                // 動態添加客戶 ID 條件
                if (request.customerIds != null && request.customerIds.Any())
                {
                    var customerIdParams = string.Join(",", request.customerIds.Select((id, index) => $"@customerId{index}"));
                    sqlBuilder.Append($" AND receipt.customerId IN ({customerIdParams})");
                    
                    for (int i = 0; i < request.customerIds.Count; i++)
                    {
                        parameters.Add(new SqlParameter($"@customerId{i}", request.customerIds[i]));
                    }
                }

                // 動態添加收款日期開始條件
                if (!string.IsNullOrEmpty(request.receiptDateStart))
                {
                    if (DateTime.TryParse(request.receiptDateStart, out DateTime receiptDateStart))
                    {
                        sqlBuilder.Append(" AND receipt.receiptDate >= @receiptDateStart");
                        parameters.Add(new SqlParameter("@receiptDateStart", receiptDateStart.Date));
                    }
                }

                // 動態添加收款日期結束條件
                if (!string.IsNullOrEmpty(request.receiptDateEnd))
                {
                    if (DateTime.TryParse(request.receiptDateEnd, out DateTime receiptDateEnd))
                    {
                        // 結束日期需要包含當天的所有時間，所以加上 23:59:59
                        sqlBuilder.Append(" AND receipt.receiptDate <= @receiptDateEnd");
                        parameters.Add(new SqlParameter("@receiptDateEnd", receiptDateEnd.Date.AddDays(1).AddSeconds(-1)));
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
                    // 格式化日期為 yyyy-MM-dd
                    if (!reader.IsDBNull(reader.GetOrdinal("receiptDate")))
                    {
                        DateTime receiptDate = reader.GetDateTime(reader.GetOrdinal("receiptDate"));
                        row["receiptDate"] = receiptDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        row["receiptDate"] = null;
                    }
                    
                    row["customerId"] = reader.IsDBNull(reader.GetOrdinal("customerId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("customerId"));
                    row["customerNickName"] = reader.IsDBNull(reader.GetOrdinal("customerNickName")) ? null : reader.GetString(reader.GetOrdinal("customerNickName"));
                    
                    // 格式化日期為 yyyy-MM-dd
                    if (!reader.IsDBNull(reader.GetOrdinal("paymentDate")))
                    {
                        DateTime paymentDate = reader.GetDateTime(reader.GetOrdinal("paymentDate"));
                        row["paymentDate"] = paymentDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        row["paymentDate"] = null;
                    }
                    
                    row["paymentType"] = reader.IsDBNull(reader.GetOrdinal("paymentType")) ? null : reader.GetString(reader.GetOrdinal("paymentType"));
                    row["price"] = reader.IsDBNull(reader.GetOrdinal("price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("price"));
                    row["offsetRecordId"] = reader.IsDBNull(reader.GetOrdinal("offsetRecordId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("offsetRecordId"));
                    row["shouldBeOffsetMoney"] = reader.IsDBNull(reader.GetOrdinal("shouldBeOffsetMoney")) ? 0 : reader.GetDecimal(reader.GetOrdinal("shouldBeOffsetMoney"));
                    
                    // 根據 offsetRecordId 判斷 offset 值
                    bool offset = !reader.IsDBNull(reader.GetOrdinal("offsetRecordId"));
                    row["offset"] = offset;
                    
                    results.Add(row);
                }

                Debug.WriteLine($"[ReceiptService] 成功取得 {results.Count} 筆收款單清單資料");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = results;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReceiptService] 取得收款單清單失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, null);
            }
        }
    }
}

