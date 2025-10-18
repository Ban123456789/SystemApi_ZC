namespace Accura_MES.Interfaces.Services
{
    public interface IShippingOrderService : IService
    {
        /// <summary>
        /// 建立 [shippingOrder]
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="shippingOrderObject">ShippingOrder Object</param>
        /// <returns></returns>
        Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> shippingOrderObject);

        /// <summary>
        /// 查詢出貨單列表
        /// </summary>
        /// <param name="searchParams">查詢參數（ids, orderId）</param>
        /// <returns></returns>
        Task<ResponseObject> GetList(Dictionary<string, object?> searchParams);
    }
}

