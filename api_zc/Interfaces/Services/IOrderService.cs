namespace Accura_MES.Interfaces.Services
{
    public interface IOrderService : IService
    {
        /// <summary>
        /// 建立 [order]
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="orderObject">Order Object</param>
        /// <returns></returns>
        Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> orderObject);

        /// <summary>
        /// 查詢訂單列表
        /// </summary>
        /// <param name="searchParams">查詢參數（ids, shippedDate）</param>
        /// <returns></returns>
        Task<ResponseObject> GetList(Dictionary<string, object?> searchParams);
    }
}

