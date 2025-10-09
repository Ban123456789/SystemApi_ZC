using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Repositories;
using Microsoft.Data.SqlClient;

namespace Accura_MES.Services
{
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        private UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 UserService 實例，並初始化資料庫連線
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>UserService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static UserService CreateService(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            return new UserService(connectionString);
        }

        public async Task<long> Create(Dictionary<string, object?> UserInfo, long? user)
        {
            try
            {
                IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                return await userRepository.Create(UserInfo, user);
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> Update(Dictionary<string, object?> UserInfo, long? user)
        {
            try
            {
                IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                return await userRepository.Update(UserInfo, user);
            }
            catch
            {
                throw;
            }
        }

        public async Task<ResponseObject> ChangePassWord(long user, string oldPassword, string newPassword)
        {
            ResponseObject response = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG,
                null, null,
                "變更使用者密碼發生未知錯誤");

            IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

            SqlTransaction? transaction = null;
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                bool isSuccess =
                    await userRepository.ChangePassWord(connection, transaction, user, oldPassword, newPassword);

                if (!isSuccess)
                {
                    response.SetErrorCode(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, "變更使用者密碼失敗");
                    return response;
                }

                // commit transaction
                transaction.Commit();

                // return success
                response.SetErrorCode(SelfErrorCode.SUCCESS);
                return response;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex, transaction);
            }
        }

        public async Task<bool> UpdateCreatedByAndModifiedBy(Dictionary<string, object?> UserInfo)
        {
            try
            {
                IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                return await userRepository.UpdateCreatedByAndModifiedBy(UserInfo);
            }
            catch
            {
                throw;
            }
        }
    }
}
