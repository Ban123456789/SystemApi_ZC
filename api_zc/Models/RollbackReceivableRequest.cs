namespace Accura_MES.Models
{
    /// <summary>
    /// 應收帳款退回請求模型
    /// </summary>
    public class RollbackReceivableRequest
    {
        /// <summary>
        /// 開始出貨日期
        /// </summary>
        public DateTime? startShippedDate { get; set; }

        /// <summary>
        /// 結束出貨日期
        /// </summary>
        public DateTime? endShippedDate { get; set; }

        /// <summary>
        /// 客戶 ID 陣列
        /// </summary>
        public List<long> customerIds { get; set; } = new List<long>();
    }
}
