using Accura_MES.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Accura_MES.Interfaces.Services
{
    public interface IGenericService : IService
    {
        /// <summary>
        /// 通用 多資料刪除(隱藏)
        /// </summary>
        /// <param name="user"></param>
        /// <param name="shareInfo"></param>
        /// <returns></returns>
        Task<int> IsDelete(long user, TableDatas shareInfo);

        /// <summary>
        /// 通用 取資料表資料，若取不到就依輸入建立一筆
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys">查詢用鍵名</param>
        /// <param name="input">kvp of 欄位名, 欄位值</param>
        /// <param name="user"></param>
        /// <returns>這筆資料的所有資訊</returns>
        Task<Dictionary<string, object?>> GetOrCreate(SqlConnection connection, SqlTransaction? transaction,
            string tableName, HashSet<string> primaryKeys, Dictionary<string, object?> input, long user);

        /// <summary>
        /// 批次 UPSERT + DELETE
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataTable">包含欄位與對應型別</param>
        /// <param name="primaryKeys">比對欄位</param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<ResponseObject> Upsert_Delete_SqlBulkCopy(
            string tableName,
            DataTable dataTable,
            HashSet<string> primaryKeys);
    }
}
