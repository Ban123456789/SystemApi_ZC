namespace Accura_MES.Interfaces.Repositories
{
    public interface IPreferencesRepository : IRepository
    {
        /// <summary>
        /// 從所有資料庫的[preferences] 取得同步任務。
        /// 判斷 enable = 1 and isDelete = 0
        /// </summary>
        /// <returns> dict(database, dict(taskName, taskValue)) </returns>
        Task<Dictionary<string, Dictionary<string, string>>> GetSyncTaskForEachDataBase();

        /// <summary>
        /// 從所有資料庫的[preferences] 取得CNC 自動換班回報任務。
        /// 判斷 enable = 1 and isDelete = 0
        /// </summary>
        /// <returns>dict(database, dict(taskName, taskValue))</returns>
        Task<ResponseObject> GetCNCAutoShiftReportSettingEachDataBase();
    }
}
