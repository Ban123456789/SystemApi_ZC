using Accura_MES.Interfaces.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace Accura_MES.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        private UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 UserRepository 實例，並初始化資料庫連線
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>UserRepository 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static UserRepository CreateRepository(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            return new UserRepository(connectionString);
        }

        public async Task<long> Create(Dictionary<string, object?> UserInfo, long? user)
        {
            try
            {
                long userId = 0;
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {

                            var fieldNames = new List<string>();
                            var paramPlaceholders = new List<string>();
                            var parameters = new Dictionary<string, object>();

                            // 檢查並處理密碼的加解密
                            if (UserInfo.ContainsKey("password") && UserInfo["password"] is not null)
                            {
                                string passwordString = UserInfo["password"].ToString();

                                // 假設密碼以明文傳遞，則先進行 AES 解密再加密處理
                                string decryptedPassword = DecryptAES(passwordString);
                                string reEncryptedPassword = EncryptAES(decryptedPassword);
                                UserInfo["password"] = reEncryptedPassword;
                            }

                            UserInfo["createdBy"] = user;
                            UserInfo["modifiedBy"] = user;

                            foreach (var kvp in UserInfo)
                            {
                                fieldNames.Add(kvp.Key);
                                paramPlaceholders.Add("@" + kvp.Key);

                                if (kvp.Value != null) parameters.Add("@" + kvp.Key, kvp.Value.ToString());
                                else parameters.Add("@" + kvp.Key, DBNull.Value);
                            }

                            string query = $"INSERT INTO [user] ({string.Join(", ", fieldNames)}) VALUES ({string.Join(", ", paramPlaceholders)}) SELECT SCOPE_IDENTITY();";

                            using (SqlCommand command = new SqlCommand(query, connection, transaction))
                            {
                                foreach (var param in parameters)
                                {
                                    // 注入參數
                                    SqlHelper.AddCommandParameters(command, param);
                                }

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        userId = long.Parse(reader[0]?.ToString() ?? "0"); // 收集每次插入返回的 ID
                                    }
                                }
                            }

                            // 提交交易
                            await transaction.CommitAsync();

                            if (userId == 0)
                                throw new CustomErrorCodeException(SelfErrorCode.SQL_ERROR, "發生例外:返回 inserted id 為空");

                            return userId;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<long> Create(SqlConnection connection, SqlTransaction transaction, Dictionary<string, object?> UserInfo, long? user)
        {
            try
            {
                long userId = 0;
                var fieldNames = new List<string>();
                var paramPlaceholders = new List<string>();
                var parameters = new Dictionary<string, object?>();

                // 檢查並處理密碼的加解密
                if (UserInfo.ContainsKey("password") && UserInfo["password"] is not null)
                {
                    string passwordString = UserInfo["password"].ToString();

                    // 假設密碼以明文傳遞，則先進行 AES 解密再加密處理
                    string decryptedPassword = DecryptAES(passwordString);
                    string reEncryptedPassword = EncryptAES(decryptedPassword);
                    UserInfo["password"] = reEncryptedPassword;
                }

                UserInfo["createdBy"] = user;
                UserInfo["modifiedBy"] = user;

                foreach (var kvp in UserInfo)
                {
                    fieldNames.Add(kvp.Key);
                    paramPlaceholders.Add("@" + kvp.Key);

                    if (kvp.Value != null) parameters.Add("@" + kvp.Key, kvp.Value.ToString());
                    else parameters.Add("@" + kvp.Key, DBNull.Value);
                }

                string query = $"INSERT INTO [user] ({string.Join(", ", fieldNames)}) VALUES ({string.Join(", ", paramPlaceholders)}) SELECT SCOPE_IDENTITY();";

                Log.Debug(query);

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    foreach (var param in parameters)
                    {
                        // 注入參數
                        SqlHelper.AddCommandParameters(command, param);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            userId = long.Parse(reader[0]?.ToString() ?? "0"); // 收集每次插入返回的 ID
                        }
                    }
                }

                if (userId == 0)
                    throw new CustomErrorCodeException(SelfErrorCode.SQL_ERROR, "發生例外:返回 inserted id 為空");

                return userId;

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
                int affectedNumber = 0;

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var setClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (UserInfo.ContainsKey("password") && UserInfo["password"] is not null)
                    {
                        string passwordString = UserInfo["password"].ToString();

                        // 假設密碼以明文傳遞，則先進行 AES 解密再加密處理
                        string decryptedPassword = DecryptAES(passwordString);
                        string reEncryptedPassword = EncryptAES(decryptedPassword);
                        UserInfo["password"] = reEncryptedPassword;
                    }

                    UserInfo["modifiedBy"] = user;

                    // 動態構建 UPDATE 語句
                    foreach (var kvp in UserInfo)
                    {
                        if (kvp.Key != "id") setClauses.Add($"{kvp.Key} = @{kvp.Key}");
                        if (kvp.Value != null) parameters.Add("@" + kvp.Key, kvp.Value.ToString());
                        else parameters.Add("@" + kvp.Key, DBNull.Value);
                    }

                    string query = $"UPDATE [user] SET {string.Join(", ", setClauses)} WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        foreach (var param in parameters)
                        {
                            SqlHelper.AddCommandParameters(command, param);
                        }

                        affectedNumber = await command.ExecuteNonQueryAsync();
                    }

                    // 提交整個交易
                    await transaction.CommitAsync();

                    if (affectedNumber == 0)
                    {
                        return false;
                    }

                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> Update(SqlConnection connection, SqlTransaction transaction,
            Dictionary<string, object?> UserInfo, long? user)
        {
            try
            {
                int affectedNumber = 0;

                var setClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (UserInfo.ContainsKey("password") && UserInfo["password"] is not null)
                {
                    string passwordString = UserInfo["password"].ToString();

                    // 假設密碼以明文傳遞，則先進行 AES 解密再加密處理
                    string decryptedPassword = DecryptAES(passwordString);
                    string reEncryptedPassword = EncryptAES(decryptedPassword);
                    UserInfo["password"] = reEncryptedPassword;
                }

                UserInfo["modifiedBy"] = user;

                // 動態構建 UPDATE 語句
                foreach (var kvp in UserInfo)
                {
                    if (kvp.Key != "id") setClauses.Add($"{kvp.Key} = @{kvp.Key}");
                    if (kvp.Value != null) parameters.Add("@" + kvp.Key, kvp.Value.ToString());
                    else parameters.Add("@" + kvp.Key, DBNull.Value);
                }

                string query = $"UPDATE [user] SET {string.Join(", ", setClauses)} WHERE id = @id";

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    foreach (var param in parameters)
                    {
                        SqlHelper.AddCommandParameters(command, param);
                    }

                    affectedNumber = await command.ExecuteNonQueryAsync();
                }

                if (affectedNumber == 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> UpdateCreatedByAndModifiedBy(Dictionary<string, object?> UserInfo)
        {
            try
            {
                int affectedNumber = 0;

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var parameters = new Dictionary<string, object?>()
                    {
                        { "createdBy", UserInfo.ContainsKey("createdBy") ? UserInfo["createdBy"] : DBNull.Value },
                        { "modifiedBy", UserInfo.ContainsKey("modifiedBy") ? UserInfo["modifiedBy"] : DBNull.Value }
                    };

                    string query = $"UPDATE [user] SET createdBy = @createdBy, modifiedBy = @modifiedBy WHERE id = @id";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        foreach (var param in parameters)
                        {
                            SqlHelper.AddCommandParameters(command, param);
                        }

                        affectedNumber = await command.ExecuteNonQueryAsync();
                    }

                    // 提交整個交易
                    await transaction.CommitAsync();

                    if (affectedNumber == 0)
                    {
                        return false;
                    }

                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }


        public async Task<long> GetSystemId(SqlConnection connection, SqlTransaction? transaction)
        {
            try
            {
                IGenericRepository genericRepository = GenericRepository.CreateRepository(_connectionString, null);

                var users = genericRepository.GenericGetNotDelete(
                    new()
                    {
                        Datasheet = "user",
                        Dataname = "account",
                        Datas =
                        new()
                        {
                            "system"
                        }

                    },
                    connection,
                    transaction);

                if (!users.Any())
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "\"system\" 資料不存在於 [user]");
                }

                // 理論上只有一筆
                var user = users.First();

                return long.Parse(user["id"].ToString());

            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> ChangePassWord(SqlConnection connection, SqlTransaction? transaction,
            long user, string oldPassword, string newPassword)
        {
            IGenericRepository genericRepository = GenericRepository.CreateRepository(_connectionString, null);

            // 假設密碼以明文傳遞，則先進行 AES 解密再加密處理
            string decryptedPassword = DecryptAES(oldPassword);
            string reEncryptedOldPassword = EncryptAES(decryptedPassword);

            decryptedPassword = DecryptAES(newPassword);
            string reEncryptedNewPassword = EncryptAES(decryptedPassword);

            // verify old password
            string query = "SELECT COUNT(1) FROM [user] WHERE id = @userId AND password = @password";


            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                SqlHelper.AddCommandParameters(command, new("@userId", user));
                SqlHelper.AddCommandParameters(command, new("@password", reEncryptedOldPassword));

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetInt32(0) == 0)
                        {
                            throw new CustomErrorCodeException(SelfErrorCode.WRONG_PASSWORD, "舊密碼不正確");
                        }
                    }
                }
            }

            // update new password
            var result = await genericRepository.GenericUpdate(
                user,
                new Models.TableDatas
                {
                    Datasheet = "user",
                    DataStructure = new()
                    {
                        new()
                        {
                            { "id", user },
                            { "password", reEncryptedNewPassword }
                        }
                    }
                },
                connection,
                transaction);

            // if update failed
            if (result == 0)
            {
                throw new CustomErrorCodeException(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, "更新密碼失敗");
            }

            // return true if success
            return true;
        }

        #region 加密/解密
        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static string EncryptAES(string plainText)
        {
            // 密鑰
            byte[] key = Encoding.UTF8.GetBytes("a1B2c3D4e5F6g7H8");
            // 獲取 IV
            byte[] iv = Encoding.UTF8.GetBytes("Y5z6A7b8C9d0E1f2");

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv; // 設置靜態 IV

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // 將 IV 寫入到密文流中
                    msEncrypt.Write(iv, 0, iv.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray()); // 返回包含 IV 的密文
                }
            }
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        /// <param name="encryptedText"></param>
        /// <returns></returns>
        private static string DecryptAES(string encryptedText)
        {
            //密鑰
            byte[] key = Encoding.UTF8.GetBytes("=GC%'AmN/}2f9Q#u");

            // 從 Base64 編碼的加密字串轉換為 byte[]
            byte[] cipherTextCombined = Convert.FromBase64String(encryptedText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.Mode = CipherMode.CBC; // 和前端保持一致
                aesAlg.Padding = PaddingMode.PKCS7;

                // AES IV 長度總是 16 字節，從密文中提取 IV
                byte[] iv = new byte[16];
                byte[] cipherText = new byte[cipherTextCombined.Length - iv.Length];

                Array.Copy(cipherTextCombined, iv, iv.Length);
                Array.Copy(cipherTextCombined, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }


        #endregion
    }
}
