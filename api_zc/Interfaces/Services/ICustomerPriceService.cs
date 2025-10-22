namespace Accura_MES.Interfaces.Services
{
    public interface ICustomerPriceService : IService
    {
        /// <summary>
        /// 建立客戶價格資料
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="customerPriceObject">客戶價格資料列表</param>
        /// <returns></returns>
        Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> customerPriceObject);
    }
}
