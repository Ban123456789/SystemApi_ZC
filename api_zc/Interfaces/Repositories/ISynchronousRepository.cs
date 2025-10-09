using Microsoft.Data.SqlClient;

namespace Accura_MES.Interfaces.Repositories
{
    public interface ISynchronousRepository : IRepository
    {
        /// <summary>
        /// 檢查是否同步過了。
        /// <para></para>
        /// 方法: 檢查資料庫中是否存在符合一般欄位和 JSON 鍵值對的資料
        /// </summary>
        /// <param name="normalColumns">key:欄位名;value:欄位值</param>
        /// <param name="jsonKVP">JSON 的鍵值對</param>
        /// <param name="startTime">如果找到符合條件的資料，則返回該筆資料的 [startTime] 欄位的值</param>
        /// <returns>是否存在符合條件的資料</returns>
        bool IsSyncRecordExist(
            SqlConnection connection,
            SqlTransaction? transaction,
            Dictionary<string, string> normalColumns,
            Dictionary<string, string> jsonKVP,
            out string? startTime);

        /// <summary>
        /// 生產排程維護作業
        /// </summary>
        /// <param name="inspectionId"></param>
        /// <param name="User"></param>
        /// <param name="isManual">true:"手動"; false:"匯入 Excel"</param>
        /// <returns></returns>
        Task<ResponseObject> ProductionSchedule(long user, bool isManual, List<long> moIds);

        /// <summary>
        /// 生產日報維護作業
        /// </summary>
        /// <param name="totalProductionTime">如果沒有提供，會去資料庫從回報紀錄抓</param>
        /// <param name="isModifyHole">true: 修改穴數時呼叫; false: 製令'暫停'or'完工'時呼叫</param>
        /// <returns></returns>
        Task<ResponseObject> ProductionDaily(long user, long moId, bool isModifyHole);

        /// <summary>
        /// FQC品質記錄維護作業
        /// </summary>
        /// <param name="inspectionId"></param>
        /// <returns></returns>
        Task<ResponseObject> FQC(long user, long inspectionId);
    }
}
