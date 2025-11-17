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
    }
}

