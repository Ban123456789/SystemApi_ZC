using Accura_MES.Models;

namespace Accura_MES.Interfaces.Services
{
    public interface IOffsetService : IService
    {
        /// <summary>
        /// 取得未沖帳清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        Task<ResponseObject> GetUnOffsetList(GetUnOffsetListRequest request);

        /// <summary>
        /// 沖帳
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="requests">沖帳請求列表</param>
        /// <returns>響應對象</returns>
        Task<ResponseObject> Offset(long userId, List<OffsetRequest> requests);

        /// <summary>
        /// 取得沖帳紀錄
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含沖帳紀錄及其關聯的收款單和出貨單</returns>
        Task<ResponseObject> GetOffsetRecords(GetOffsetRecordsRequest request);

        /// <summary>
        /// 反沖銷
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="request">反沖銷請求</param>
        /// <returns>響應對象</returns>
        Task<ResponseObject> RollbackOffset(long userId, RollbackOffsetRequest request);

        /// <summary>
        /// 取得客戶結餘
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>響應對象，包含客戶結餘資訊</returns>
        Task<ResponseObject> GetCustomerBalance(GetCustomerBalanceRequest request);
    }
}

