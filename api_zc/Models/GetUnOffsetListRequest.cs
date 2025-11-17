namespace Accura_MES.Models
{
    /// <summary>
    /// 取得未沖帳清單請求模型
    /// </summary>
    public class GetUnOffsetListRequest
    {
        /// <summary>
        /// 收款日期
        /// </summary>
        public string? receiptDate { get; set; }

        /// <summary>
        /// 開始出貨日期
        /// </summary>
        public string? shippedDateStart { get; set; }

        /// <summary>
        /// 結束出貨日期
        /// </summary>
        public string? shippedDateEnd { get; set; }

        /// <summary>
        /// 客戶 ID
        /// </summary>
        public long? customerId { get; set; }

        /// <summary>
        /// 工程 ID 陣列
        /// </summary>
        public List<long> projectIds { get; set; } = new List<long>();

        /// <summary>
        /// 是否需要搜尋未沖銷的
        /// </summary>
        public bool needToSearchUnoffsetted { get; set; } = true;
    }
}

