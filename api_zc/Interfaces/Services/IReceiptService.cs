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
    }
}

