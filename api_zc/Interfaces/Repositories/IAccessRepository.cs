using Microsoft.Data.SqlClient;

namespace Accura_MES.Interfaces.Repositories
{
    public interface IAccessRepository : IRepository
    {
        /// <summary>
        /// 建立/刪除權限
        /// </summary>
        /// <param name="user"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        Task<List<long>> CreateOrDeleteAccess(string user, List<Dictionary<string, object>> access);

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="_connection"></param>
        /// <param name="access"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <remarks>
        /// 可以確保共用同一個連線
        /// </remarks>
        Task<long> InsertRecord(SqlConnection? _connection, string user, Dictionary<string, object> access);
    }
}
