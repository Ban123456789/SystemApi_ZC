namespace Accura_MES.Models
{
    /// <summary>
    /// 刪除訂單請求模型
    /// </summary>
    public class DeleteOrdersRequest
    {
        /// <summary>
        /// 訂單 ID 陣列
        /// </summary>
        public List<long> orderIds { get; set; } = new List<long>();
    }
}

