namespace Accura_MES.Models
{
    /// <summary>
    /// 取得應收帳款清單請求模型
    /// </summary>
    public class GetReceivableListRequest
    {
        /// <summary>
        /// 出貨單 ID 陣列
        /// </summary>
        public List<long> ids { get; set; } = new List<long>();

        /// <summary>
        /// 開始出貨日期
        /// </summary>
        public string? shippedDateStart { get; set; }

        /// <summary>
        /// 結束出貨日期
        /// </summary>
        public string? shippedDateEnd { get; set; }

        /// <summary>
        /// 客戶 ID 陣列
        /// </summary>
        public List<long> customerIds { get; set; } = new List<long>();
    }
}

