namespace Accura_MES.Models
{
    /// <summary>
    /// 取得收款單清單請求模型
    /// </summary>
    public class GetReceiptListRequest
    {
        /// <summary>
        /// 收款單 ID 陣列
        /// </summary>
        public List<long> ids { get; set; } = new List<long>();

        /// <summary>
        /// 客戶 ID 陣列
        /// </summary>
        public List<long> customerIds { get; set; } = new List<long>();

        /// <summary>
        /// 收款日期開始
        /// </summary>
        public string? receiptDateStart { get; set; }

        /// <summary>
        /// 收款日期結束
        /// </summary>
        public string? receiptDateEnd { get; set; }
    }
}

