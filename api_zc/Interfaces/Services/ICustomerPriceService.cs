using Accura_MES.Models;

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

        /// <summary>
        /// 取得客戶別簡表
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        Task<ResponseObject> GetAccountingByCustomer(GetAccountingByCustomerRequest request);

        /// <summary>
        /// 出貨單轉應收帳款
        /// </summary>
        /// <param name="request">轉換請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns></returns>
        Task<ResponseObject> ConverToReceivable(ConverToReceivableRequest request, long userId);

        /// <summary>
        /// 應收帳款退回
        /// </summary>
        /// <param name="request">退回請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns></returns>
        Task<ResponseObject> RollbackReceivable(RollbackReceivableRequest request, long userId);

        /// <summary>
        /// 建立應收帳款
        /// </summary>
        /// <param name="request">建立應收帳款請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns></returns>
        Task<ResponseObject> CreateReceivable(CreateReceivableRequest request, long userId);

        /// <summary>
        /// 取得應收帳款清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        Task<ResponseObject> GetReceivableList(GetReceivableListRequest request);

        /// <summary>
        /// 刪除應收帳款
        /// </summary>
        /// <param name="request">刪除請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns></returns>
        Task<ResponseObject> DeleteReceivable(DeleteReceivableRequest request, long userId);
    }
}
