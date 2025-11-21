namespace Accura_MES.Models
{
    /// <summary>
    /// 取得沖帳紀錄請求模型
    /// </summary>
    public class GetOffsetRecordsRequest
    {
        /// <summary>
        /// 開始沖帳日期
        /// </summary>
        public string? offsetDateStart { get; set; }

        /// <summary>
        /// 結束沖帳日期
        /// </summary>
        public string? offsetDateEnd { get; set; }

        /// <summary>
        /// 客戶 ID 陣列
        /// </summary>
        public List<long> customerIds { get; set; } = new List<long>();
    }
}

