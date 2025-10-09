using Microsoft.Data.SqlClient;

namespace Accura_MES.Interfaces.Services
{
    public interface ISynchronousService : IService
    {
        #region syncERPtoMES
        /// <summary>
        /// 同步 ERP 例外日，
        /// 同步輸入的當年的所有例外日
        /// </summary>
        /// <param name="date">年</param>
        /// <param name="user"></param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncExceoptionDays(int date, long user, bool isAutoSync);

        /// <summary>
        /// 同步 ERP 工序範本
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncProcessTemplate(DateTime? date, long user, bool isAutoSync);

        /// <summary>
        /// 同步 ERP 工作站
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncWorkstation(DateTime? date, long user, bool isAutoSync);

        /// <summary>
        /// 這個方法已被新的方法 <see cref="SyncPart_Batch"/> 取代。
        /// </summary>
        /// <remarks>
        /// 此方法效率較低，不建議使用。
        /// </remarks>
        [Obsolete("Use SyncPart_Batch instead.")]
        Task<(ResponseObject, List<ResponseObject>)> SyncPart(DateTime? date, long user, bool isAutoSyn);

        /// <summary>
        /// 批量同步 ERP 料件
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncPart_Batch(DateTime? date, long user, bool isAutoSync);

        /// <summary>
        /// 同步 線程
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <param name="isAutoSync">true:"排程" ; false:"手動"</param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncThread(DateTime? date, long user, bool isAutoSync);

        /// <summary>
        /// 同步 機器
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <param name="isAutoSync">true:"排程" ; false:"手動"</param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncMachine(DateTime? date, long user, bool isAutoSync);

        /// <summary>
        /// 同步 ERP 工藝(製程)範本
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncCraft(DateTime? date, long user, bool isAutoSync);

        /// <summary>
        /// 同步 ERP 生產訂單
        /// </summary>
        /// <param name="date">date(年-月-日) 可為空</param>
        /// <param name="user"></param>
        /// <param name="isAutoSync">true:"排程" ; false:"手動"</param>
        /// <returns>第一筆 : 紀錄整個流程的狀況 ; 第二筆 : 記錄所有個別同步狀況</returns>
        Task<(ResponseObject, List<ResponseObject>)> SyncPO(DateTime? date, long user, bool isAutoSync);
        #endregion

        #region syncMEStoERP
        /// <summary>
        /// 建立/更新 同步歷程
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="syncHistoryId">無提供:Create; 有提供:Update</param>
        /// <param name="user"></param>
        /// <param name="syncAction"></param>
        /// <param name="status"></param>
        /// <param name="startTime">建立時必填</param>
        /// <param name="endTime">建議更新時填</param>
        /// <param name="explain"></param>
        /// <remarks>
        /// 會自動注入工廠名稱
        /// </remarks>
        /// <returns>syncHistoryId => long</returns>
        Task<ResponseObject> UpsertSyncHistory_MEStoERP(SqlConnection connection, SqlTransaction? transaction,
            long? syncHistoryId,
            long user,
            string syncAction,
            string status,
            DateTime? startTime,
            DateTime? endTime,
            Dictionary<string, object?> explain);

        /// <summary>
        /// 手動拋轉 MES 製令回報至 ERP
        /// </summary>
        /// <param name="user"></param>
        /// <param name="moReportHistory">要拋過去的物件</param>
        /// <returns></returns>
        Task<ResponseObject> TossERPMOReportHistory(long user, Dictionary<string, object?> moReportHistory);


        #endregion
    }
}
