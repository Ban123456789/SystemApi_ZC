namespace Accura_MES.Models
{
    /// <summary>
    /// 刪除應收帳款請求模型
    /// </summary>
    public class DeleteReceivableRequest
    {
        /// <summary>
        /// 出貨單 ID 陣列
        /// </summary>
        public List<long> ids { get; set; } = new List<long>();
    }
}

