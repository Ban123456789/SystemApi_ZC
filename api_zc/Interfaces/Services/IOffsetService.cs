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
    }
}

