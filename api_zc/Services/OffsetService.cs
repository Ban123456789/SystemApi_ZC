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

        private OffsetService(string connectionString, IGenericRepository genericRepository)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
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

            return new OffsetService(connectionString, genericRepository);
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
                var shippingOrderSqlBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        shippingOrder.id as id,
                        shippingOrder.type as type,
                        shippingOrder.price as price,
                        shippingOrder.outputMeters as outputMeters,
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
                        AND ISNULL(shippingOrder.price, 0) > 0
                        AND (
                            (shippingOrder.type = '1' 
                             AND ISNULL(shippingOrder.price, 0) * ISNULL(shippingOrder.outputMeters, 0) > ISNULL(shippingOrder.offsetMoney, 0))
                            OR
                            (shippingOrder.type = '2' 
                             AND ISNULL(shippingOrder.price, 0) > ISNULL(shippingOrder.offsetMoney, 0))
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
    }
}

