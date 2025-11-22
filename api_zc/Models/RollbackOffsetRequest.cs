namespace Accura_MES.Models
{
    /// <summary>
    /// 反沖銷請求模型
    /// </summary>
    public class RollbackOffsetRequest
    {
        /// <summary>
        /// 沖帳紀錄 ID 陣列
        /// </summary>
        public List<long> offsetRecordIds { get; set; } = new List<long>();
    }
}

