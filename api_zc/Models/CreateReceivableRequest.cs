namespace Accura_MES.Models
{
    /// <summary>
    /// 建立應收帳款請求模型
    /// </summary>
    public class CreateReceivableRequest
    {
        /// <summary>
        /// 訂單資料
        /// </summary>
        public Dictionary<string, object?> order { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// 出貨單資料
        /// </summary>
        public Dictionary<string, object?> shippingOrder { get; set; } = new Dictionary<string, object?>();
    }
}

