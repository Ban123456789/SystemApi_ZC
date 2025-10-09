using Accura_MES.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Accura_MES.Interfaces.Repositories
{

    public interface IGenericRepository : IRepository
    {
        /// <summary>
        /// 通用一般搜尋
        /// </summary>
        /// <param name="innerSearch"></param>
        /// <returns></returns>
        List<Dictionary<string, object>> GenericGet(InnerSearch innerSearch);

        /// <summary>
        /// 計算資料表的資料筆數
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// 接收外部資料庫連線，並且查詢條件自動新增 isDelete = 0
        /// </remarks>
        int CountRows(SqlConnection connection, SqlTransaction? transaction, string tableName);

        /// <summary>
        /// 通用一般搜尋
        /// </summary>
        /// <param name="innerSearch"></param>
        /// <param name="sqlConnection"></param>
        /// <returns>所有符合條件的資料; 如果條件全空，回傳空的陣列</returns>
        /// <remarks>
        /// 接收外部資料庫連線，並且查詢條件自動新增 isDelete = 0
        /// </remarks>
        List<Dictionary<string, object?>> GenericGetNotDelete(InnerSearch innerSearch, SqlConnection sqlConnection, SqlTransaction? sqlTransaction);

        /// <summary>
        /// 通用批次更新
        /// </summary>
        /// <param name="shareInfo"></param>
        /// <remarks>
        /// key: id
        /// </remarks>
        /// <returns>affected rows number</returns>
        Task<int> GenericUpdate(long user, TableDatas shareInfo, SqlConnection sqlConnection, SqlTransaction? sqlTransaction);

        /// <summary>
        /// 通用 更新欄位值，將字串加到欄位前 or 後
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="user"></param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="columnsToUpdate">Key:欄位名, value:想要加入的字串</param>
        /// <param name="whereCondition">資料行查詢條件 Key:欄位名, value:欄位值</param>
        /// <param name="isFront">true:將字串加到欄位前; false:將字串加到欄位後</param>
        /// <returns>Success or not</returns>
        Task<ResponseObject> GenericConcat(
            SqlConnection connection, SqlTransaction? transaction,
            long user,
            string tableName,
            Dictionary<string, string> columnsToUpdate,
            Dictionary<string, object> whereCondition,
            bool isFront);

        /// <summary>
        /// 通用單次 UPSERT
        /// </summary>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="primaryKeys">主鍵</param>
        /// <param name="values">Key:欄位名;Value:欄位值</param>
        /// <param name="user"></param>
        /// <param name="connection">不可為空</param>
        /// <param name="transaction">可為空</param>
        /// <returns>影響到的資料行的 id; 如果沒有插入或更新的記錄返回 null</returns>
        Task<long?> Upsert(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            long user,
            SqlConnection connection,
            SqlTransaction? transaction = null);

        /// <summary>
        /// 批次 UPSERT
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataTable">包含欄位與對應型別</param>
        /// <param name="primaryKeys">比對欄位</param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task Upsert_SqlBulkCopy(
            string tableName,
            DataTable dataTable,
            HashSet<string> primaryKeys,
            SqlConnection connection,
            SqlTransaction? transaction = null);

        /// <summary>
        /// 批次 UPSERT + DELETE
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataTable">包含欄位與對應型別</param>
        /// <param name="primaryKeys">比對欄位</param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task Upsert_Delete_SqlBulkCopy(
            string tableName,
            DataTable dataTable,
            HashSet<string> primaryKeys,
            SqlConnection connection,
            SqlTransaction? transaction = null);

        /// <summary>
        /// 通用單次 UPDATE
        /// </summary>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="primaryKeys">比對欄位</param>
        /// <param name="values">Key:欄位名;Value:欄位值</param>
        /// <param name="user"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns>affectedRows</returns>
        Task<int> Update(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            long user,
            SqlConnection connection,
            SqlTransaction? transaction = null);

        /// <summary>
        /// 通用單次 重新計算並更新"日期"欄位的值
        /// </summary>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="primaryKeys">主鍵</param>
        /// <param name="values">Key:欄位名;Value:欄位值</param>
        /// <param name="datePart">日期的部分的運算元</param>
        /// <param name="user"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns>affectedRows</returns>
        Task<int> UpdateDate(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            string datePart,
            long user,
            SqlConnection connection,
            SqlTransaction? transaction = null);

        /// <summary>
        /// 通用進階搜尋
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        List<Dictionary<string, object?>> GenericAdvancedGet(AdvancedSearch advancedSearch);

        /// <summary>
        /// 通用進階搜尋
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        List<Dictionary<string, object?>> GenericAdvancedGet(SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch);

        /// <summary>
        /// 通用進階搜尋。
        /// 自動新增判斷 isDelete = 0
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        List<Dictionary<string, object>> GenericAdvancedGetNotDelete(SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch);

        /// <summary>
        /// 通用進階搜尋，只查詢第一筆資料: Top(1)。
        /// 自動新增判斷 isDelete = 0
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        Dictionary<string, object> GenericAdvancedGetTop(SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch);

        /// <summary>
        /// 通用 多資料建立
        /// </summary>
        /// <param name="user"></param>
        /// <param name="tableName">資料表名</param>
        /// <param name="input">使用者輸入</param>
        /// <remarks>
        /// 會去 [property] 抓欄位資訊
        /// </remarks>
        /// <returns>所有建立的資料行id</returns>
        Task<List<long>> CreateDataGeneric(long user, string tableName, List<Dictionary<string, object?>> input);

        /// <summary>
        /// 通用 多資料建立
        /// 接收外部連線
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="user"></param>
        /// <param name="tableName"></param>
        /// <param name="input"></param>
        /// <remarks>
        /// 會去 [property] 抓欄位資訊
        /// </remarks>
        /// <returns>所有建立的資料行id</returns>
        Task<List<long>> CreateDataGeneric(SqlConnection connection, SqlTransaction? transaction,
            long user, string tableName, List<Dictionary<string, object?>> input);

        /// <summary>
        /// 通用 多資料建立
        /// 接收外部連線, 欄位名稱
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="user">id</param>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="columnNames">資料表必要欄位名稱</param>
        /// <param name="input">欄位名,欄位值</param>
        /// <remarks>
        /// 不會去 [property] 抓欄位資訊
        /// </remarks>
        /// <returns>Success: [true: 全部輸入都成功建立;false: 否], Data: [所有建立的資料行 id => List&lt;long&gt;]</returns>
        Task<List<long>> CreateDataGeneric(SqlConnection connection, SqlTransaction? transaction,
            long user, string tableName, HashSet<string> columnNames, List<Dictionary<string, object?>> input);

        /// <summary>
        /// 檢查資料庫中是否存在符合一般欄位的資料，自動新增判斷 isDelete = 0
        /// </summary>
        /// <param name="columns">key:欄位名;value:欄位值</param>
        /// <returns>是否存在符合條件的資料</returns>
        bool IsRecordExist(
            SqlConnection connection,
            SqlTransaction? transaction,
            string tableName,
            Dictionary<string, object> columns);

        /// <summary>
        /// 通用 多資料刪除(隱藏)
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="user"></param>
        /// <param name="shareInfo"></param>
        /// <remarks>
        /// 需提供目標資料 id
        /// </remarks>
        /// <returns>受影響列數</returns>
        Task<int> IsDelete(SqlConnection connection, SqlTransaction? transaction,
            long user, TableDatas shareInfo);

        /// <summary>
        /// 通用 多資料刪除(真刪除)
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="user"></param>
        /// <param name="tableName"></param>
        /// <param name="ids"></param>
        /// <remarks>
        /// 需提供目標資料 id
        /// </remarks>
        /// <returns>受影響列數</returns>
        Task<int> Delete(SqlConnection connection, SqlTransaction? transaction,
            long user, string tableName, List<long> ids);

        /// <summary>
        /// 通用 多資料刪除(真刪除)
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="values"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<int> Delete(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            SqlConnection connection,
            SqlTransaction? transaction = null);

        /// <summary>
        /// 從資料庫取得巢狀資料
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <param name="localization">多國語言字串</param>
        /// <returns>nested data, null if no data</returns>
        List<Dictionary<string, object>>? GetNestedStructureData(AdvancedSearch advancedSearch, string localization);

        /// <summary>
        /// 從資料庫取得巢狀資料
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <param name="localization">多國語言</param>
        /// <returns>nested data, null if no data</returns>
        /// <remarks>
        /// 1.輸入之 Value 是 object。
        /// </remarks>
        List<Dictionary<string, object>>? GetNestedStructureData(AdvancedSearchObj advancedSearch, string localization);

        /// <summary>
        /// 從資料庫取得巢狀資料
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <param name="localization">多國語言</param>
        /// <param name="sqlConnection"></param>
        /// <returns>nested data, null if no data</returns>
        /// <remarks>
        /// 1.接收外部資料庫連線
        /// </remarks>
        List<Dictionary<string, object>>? GetNestedStructureData(AdvancedSearch advancedSearch, string localization, SqlConnection sqlConnection, SqlTransaction? sqlTransaction);

        /// <summary>
        /// 從資料庫取得巢狀資料
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <param name="localization"></param>
        /// <param name="sqlConnection"></param>
        /// <param name="sqlTransaction"></param>
        /// <returns></returns>
        /// <remarks>
        /// 1.接收外部資料庫連線。
        /// 2.輸入之 Value 是 object。
        /// </remarks>
        List<Dictionary<string, object>> GetNestedStructureData(AdvancedSearchObj advancedSearch, string localization, SqlConnection sqlConnection, SqlTransaction? sqlTransaction);

        /// <summary>
        /// FOR 設變功能
        /// 通用 複製一份未生效版本的資料
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        /// <param name="oldKeyValues">主鍵與對應的值，用來決定要複製哪一些資料</param>
        /// <param name="newKeyValues">主鍵與對應的值，複製的資料的主鍵新值</param>
        /// <returns></returns>
        Task<ResponseObject> CloneInactiveRecord(
            SqlConnection connection,
            SqlTransaction? transaction,
            long user,
            string tableName,
            Dictionary<string, object>? oldKeyValues,
            Dictionary<string, object>? newKeyValues);
    }
}
