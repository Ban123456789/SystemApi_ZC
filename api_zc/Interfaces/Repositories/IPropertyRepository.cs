using Accura_MES.Models;
using Microsoft.Data.SqlClient;

namespace Accura_MES.Interfaces.Repositories
{
    public interface IPropertyRepository : IRepository
    {
        /// <summary>
        /// 從[property]取得指定資料表的資訊
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        List<PropertyModel> GetProperty(string tableName);

        /// <summary>
        /// 從[property]取得指定資料表的資訊
        /// </summary>
        /// <param name="tableNamem"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        /// <remarks>
        /// 接收外部資料庫連線，並自動新增條件 itemtype and property isDelete = 0
        /// </remarks>
        List<PropertyModel> GetProperty(string tableNamem, SqlConnection connection, SqlTransaction? transaction);

        /// <summary>
        /// 從[droplist]取得來源
        /// </summary>
        /// <param name="menuListName">[droplist]連繫之[menulist]的name欄位索引</param>
        /// <returns>所有可能出現之不重複之來源名</returns>
        List<string> GetDataSource(string menuListName);

        /// <summary>
        /// 從[property]取得指定資料表必填欄位、與每個欄位的自訂預設值
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// 同GetProperty，只是資料取得比較少
        /// </remarks>
        Task<List<PropertyInputValidItem>> GetPropertiesAsync(string tableName);

        /// <summary>
        /// Retrieve detailed information about each column in all tables within the current database
        /// </summary>
        /// <returns>(Table name, list of(Column Name, Column Value))</returns>
        Dictionary<string, List<Dictionary<string, object>>> GetDataBaseSystemInfo();

        /// <summary>
        /// 依據資料表名稱，找出它在[ItemType]的[id]
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        long GetItemTypeId(string tableName);

        /// <summary>
        /// 初始化[itemType]
        /// </summary>
        /// <param name="systemInfo"></param>
        /// <returns></returns>
        Task<bool> InitializeItemType(Dictionary<string, List<Dictionary<string, object>>> systemInfo, long user);

        /// <summary>
        /// 接收外部連線
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="systemInfo"></param>
        /// <returns>(name, inserted id)</returns>
        Task<Dictionary<string, long>> InitializeItemType(SqlConnection connection, SqlTransaction transaction,
            Dictionary<string, List<Dictionary<string, object>>> systemInfo, long user);


        /// <summary>
        /// 初始化[property]
        /// </summary>
        /// <param name="systemInfo"></param>
        /// <param name="itemtypeInfo">紀錄itemtype.id的kvp</param>
        /// <returns></returns>
        Task<bool> InitializeProperty(Dictionary<string, List<Dictionary<string, object>>> systemInfo);

        /// <summary>
        /// 接收外部連線
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="systemInfo"></param>
        /// <returns></returns>
        Task<bool> InitializeProperty(SqlConnection connection, SqlTransaction transaction,
            Dictionary<string, List<Dictionary<string, object>>> systemInfo, Dictionary<string, long> itemtypeInfo, long user);

        /// <summary>
        /// 初始化 [menulist] and [droplist]
        /// </summary>
        /// <param name="user">基本上是 system 的 user.id</param>
        /// <returns></returns>
        Task<bool> InitializeMenuListAndDropList(long user);

        /// <summary>
        /// 接收外部連線
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> InitializeMenuListAndDropList(SqlConnection connection, SqlTransaction transaction,
            List<Mapping_MenuListAndDropList> mappingItems, long user);

        /// <summary>
        /// 使用 SqlBulkCopy 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="mappingItems"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> InitializeMenuListAndDropList_useTempTable(SqlConnection connection, SqlTransaction transaction,
            List<Dictionary<string, object>> dropListSystemInfo,
            List<Mapping_MenuListAndDropList> mappingItems, long user);

        /// <summary>
        /// 設定 [property].datasource
        /// </summary>
        /// <param name="mappingItems"></param>
        /// <returns></returns>
        Task<bool> UpdateDataSource(List<MappingItem_propertyAndMenuList> mappingItems);

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

    }
}
