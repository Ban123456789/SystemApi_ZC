using Accura_MES.Models;

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

        /// <summary>
        /// 刪除訂單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="request">刪除請求</param>
        /// <returns>響應對象</returns>
        Task<ResponseObject> DeleteOrders(long userId, DeleteOrdersRequest request);

        /// <summary>
        /// 編輯訂單
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="orderObject">訂單資料（需包含 id）</param>
        /// <returns>響應對象</returns>
        Task<ResponseObject> UpdateOrder(long userId, Dictionary<string, object?> orderObject);
    }
}

