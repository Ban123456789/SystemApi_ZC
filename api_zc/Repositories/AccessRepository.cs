using Accura_MES.Interfaces.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;

namespace Accura_MES.Repositories
{
    public class AccessRepository : IAccessRepository
    {
        private readonly string _connectionString;

        private AccessRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 靜態工廠方法
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static AccessRepository CreateRepository(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            // 返回此物件
            return new AccessRepository(connectionString);
        }

        public async Task<List<long>> CreateOrDeleteAccess(string user, List<Dictionary<string, object>> access)
        {
            var resultIds = new List<long>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var item in access)
                    {
                        string name = item.GetValueOrDefault("name")?.ToString() ?? string.Empty;
                        long identityId = long.Parse(item.GetValueOrDefault("identityId")?.ToString() ?? string.Empty);
                        bool selected = Convert.ToBoolean(item.GetValueOrDefault("selected")?.ToString() ?? string.Empty);

                        if (selected)
                        {
                            // Check if record exists
                            var existingId = await GetExistingRecordId(connection, name, identityId);

                            if (existingId == null)
                            {
                                // Insert new record
                                long newId = await InsertRecord(connection, user, item);
                                resultIds.Add(newId);

                            }
                        }
                        else
                        {
                            // Delete record if it exists
                            var existingId = await GetExistingRecordId(connection, name, identityId);

                            if (existingId != null)
                            {
                                // Delete record
                                await DeleteRecord(connection, existingId.Value);
                                resultIds.Add(existingId.Value);

                            }
                        }
                    }
                }

                return resultIds;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 檢查記錄是否存在
        /// </summary>
        /// <param name="_connection"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>
        /// 確保共用同一個連線 <see cref="CreateOrDeleteAccess(string, List{Dictionary{string, object}})"/>
        /// </remarks>
        private async Task<long?> GetExistingRecordId(SqlConnection? _connection, string name, long identityId)
        {
            const string query = "SELECT id FROM access WHERE name = @Name AND identityId = @identityId";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@identityId", identityId);

                    var result = await command.ExecuteScalarAsync();
                    return result == null ? null : Convert.ToInt64(result);
                }
            }
        }


        public async Task<long> InsertRecord(SqlConnection? _connection, string user, Dictionary<string, object> access)
        {
            var columns = access.Keys.ToList();
            columns.Remove("selected");

            // 加上user欄位
            columns.Add("createdBy");
            columns.Add("modifiedBy");

            // build query
            string insertQuery = QueryBuilder.BuildInsertQuery("access", columns);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(insertQuery, connection))
                {
                    // 參數注入
                    foreach (var column in columns)
                    {
                        // 每個欄位建立對應的參數名稱，例如 @id、@companyId 等
                        string parameterName = $"@{column}";

                        // 注入參數值
                        if (column == "createdBy" || column == "modifiedBy")
                        {
                            command.Parameters.AddWithValue(parameterName, user);
                        }
                        else
                        {
                            command.Parameters.AddWithValue(parameterName, access.GetValueOrDefault(column).ToString());
                        }

                    }

                    long insertedId = 0;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // get inserted id
                            insertedId = reader.GetInt64(0);
                        }
                    }


                    return insertedId;
                }
            }
        }

        /// <summary>
        /// 刪除資料
        /// </summary>
        /// <param name="_connection"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <remarks>
        /// 確保共用同一個連線 <see cref="CreateOrDeleteAccess(string, List{Dictionary{string, object}})"/>
        /// </remarks>
        private async Task DeleteRecord(SqlConnection? _connection, long id)
        {
            const string deleteQuery = "DELETE FROM Access WHERE Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

    }
}
