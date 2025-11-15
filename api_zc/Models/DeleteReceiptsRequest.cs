namespace Accura_MES.Models
{
    /// <summary>
    /// 刪除收款單請求模型
    /// </summary>
    public class DeleteReceiptsRequest
    {
        /// <summary>
        /// 收款單 ID 陣列
        /// </summary>
        public List<long> ids { get; set; } = new List<long>();
    }
}

