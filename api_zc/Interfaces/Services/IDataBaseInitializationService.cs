using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Interfaces.Services
{
    public interface IDataBaseInitializationService : IService
    {
        /// <summary>
        /// 設定 [property].datasource
        /// </summary>
        /// <param name="mappingItems"></param>
        /// <returns></returns>
        Task<bool> UpdateDataSource();

        /// <summary>
        /// 初始化 [itemtype] and [property]
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        Task<IActionResult> InitializeItemTypeAndPropertyAndDataSourceAsync(ControllerBase controller);

        /// <summary>
        /// 初始化 [menulist] and [droplist]
        /// </summary>
        /// <returns></returns>
        Task<bool> InitializeMenuListAndDropList();

        /// <summary>
        /// 初始化 [menulist] and [droplist]
        /// </summary>
        /// <remarks>
        /// 使用 SqlBulkCopy
        /// </remarks>
        /// <returns></returns>
        Task<bool> InitializeMenuListAndDropList_userTempTable();

        /// <summary>
        /// 建立與初始化資料庫
        /// </summary>
        /// <param name="dataBaseName">新資料庫名</param>
        /// <param name="filePath">實體檔案位置</param>
        /// <param name="logFilePath">log 檔案位置</param>
        /// <returns></returns>
        /// <remarks>
        /// 只有建立 schema，沒有建立資料
        /// </remarks>
        Task<bool> CreateDataBase(string dataBaseName, string filePath, string logFilePath);

        /// <summary>
        /// 設定系統初始預設資料
        /// </summary>
        /// <returns></returns>
        Task<bool> InitializeDataBase();
    }
}
