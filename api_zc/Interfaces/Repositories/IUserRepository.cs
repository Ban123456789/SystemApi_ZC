using Microsoft.Data.SqlClient;

namespace Accura_MES.Interfaces.Repositories
{
    public interface IUserRepository : IRepository
    {
        /// <summary>
        /// 內部管理連線
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<long> Create(Dictionary<string, object?> UserInfo, long? user);

        /// <summary>
        /// 接收外部連線
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="UserInfo"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<long> Create(SqlConnection connection, SqlTransaction transaction, Dictionary<string, object?> UserInfo, long? user);

        Task<bool> Update(Dictionary<string, object?> UserInfo, long? user);

        /// <summary>
        /// 接收外部連線
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="UserInfo"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> Update(SqlConnection connection, SqlTransaction transaction, Dictionary<string, object?> UserInfo, long? user);

        Task<bool> UpdateCreatedByAndModifiedBy(Dictionary<string, object?> UserInfo);

        /// <summary>
        /// 取得系統 id
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns>user.id</returns>
        Task<long> GetSystemId(SqlConnection connection, SqlTransaction? transaction);

        /// <summary>
        /// 變更密碼
        /// </summary>
        /// <param name="user"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        Task<bool> ChangePassWord(SqlConnection connection, SqlTransaction? transaction,
            long user, string oldPassword, string newPassword);
    }
}
