using Accura_MES.Models;

namespace Accura_MES.Interfaces.Services
{
    public interface IReceiptService : IService
    {
        /// <summary>
        /// 建立收款單
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="receiptObject">收款單資料列表</param>
        /// <returns></returns>
        Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> receiptObject);

        /// <summary>
        /// 取得收款單清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        Task<ResponseObject> GetReceiptList(GetReceiptListRequest request);

        /// <summary>
        /// 刪除收款單
        /// </summary>
        /// <param name="request">刪除請求</param>
        /// <param name="userId">使用者 ID</param>
        /// <returns></returns>
        Task<ResponseObject> DeleteReceipts(DeleteReceiptsRequest request, long userId);
    }
}

