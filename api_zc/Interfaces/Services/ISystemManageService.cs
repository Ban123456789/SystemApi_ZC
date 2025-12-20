namespace Accura_MES.Interfaces.Services
{
    public interface ISystemManageService : IService
    {
        /// <summary>
        /// 將 [tableColumnSetting] 的資料新增、編輯、(真)刪除成輸入物件的樣子
        /// </summary>
        ///<param name="user"></param>
        /// <param name="tableColumnSettingObject">long id, long userId, string tableName, string columnName, int sequence, bool isShow</param>
        /// <remarks>
        /// 用 userId, tableName, columnName  比對新舊資料
        /// </remarks>
        /// <returns></returns>
        Task<ResponseObject> TableSetting(long user, List<Dictionary<string, object?>> tableColumnSettingObject);

        /// <summary>
        /// 備份資料庫
        /// </summary>
        /// <param name="backupPath">備份文件存儲路徑（相對路徑）</param>
        /// <param name="commandTimeout">SQL 命令超時時間（秒）</param>
        /// <param name="retentionCount">保留備份檔案數量（<=0 表示不清理）</param>
        /// <returns>備份文件的路徑</returns>
        Task<string> BackupSQL(string backupPath, int commandTimeout, int retentionCount);
    }
}
