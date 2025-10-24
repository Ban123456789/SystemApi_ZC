namespace Accura_MES.Models
{
    /// <summary>
    /// 取得客戶別簡表響應模型
    /// </summary>
    public class GetAccountingByCustomerResponse
    {
        /// <summary>
        /// 出貨單 ID
        /// </summary>
        public long? shippingOrderId { get; set; }

        /// <summary>
        /// 出貨公尺數
        /// </summary>
        public decimal outputMeters { get; set; }

        /// <summary>
        /// 剩餘數量
        /// </summary>
        public decimal remaining { get; set; }

        /// <summary>
        /// 價格
        /// </summary>
        public decimal price { get; set; }

        /// <summary>
        /// 客戶 ID
        /// </summary>
        public long? customerId { get; set; }

        /// <summary>
        /// 客戶編號
        /// </summary>
        public string customerNumber { get; set; }

        /// <summary>
        /// 客戶暱稱
        /// </summary>
        public string? customerNickName { get; set; } = string.Empty;

        /// <summary>
        /// 訂單 ID
        /// </summary>
        public long? orderId { get; set; }

        /// <summary>
        /// 出貨日期
        /// </summary>
        public string? shippedDate { get; set; }

        /// <summary>
        /// 專案 ID
        /// </summary>
        public long? projectId { get; set; }

        /// <summary>
        /// 專案編號
        /// </summary>
        public string? projectNumber { get; set; } = string.Empty;

        /// <summary>
        /// 專案名稱
        /// </summary>
        public string? projectName { get; set; } = string.Empty;

        /// <summary>
        /// 產品 ID
        /// </summary>
        public long? productId { get; set; }

        /// <summary>
        /// 產品編號
        /// </summary>
        public string? productNumber { get; set; } = string.Empty;
    }
}
