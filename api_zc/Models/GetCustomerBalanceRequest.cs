namespace Accura_MES.Models
{
    /// <summary>
    /// 取得客戶結餘請求模型
    /// </summary>
    public class GetCustomerBalanceRequest
    {
        /// <summary>
        /// 客戶 ID 陣列
        /// </summary>
        public List<long> customerIds { get; set; } = new List<long>();
    }
}

