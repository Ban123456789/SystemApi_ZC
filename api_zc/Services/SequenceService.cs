using Accura_MES.Interfaces.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Accura_MES.Services
{
    /// <summary>
    /// 序列号服务实现
    /// </summary>
    public class SequenceService : ISequenceService
    {
        private readonly string _connectionString;

        private SequenceService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// 创建服务实例
        /// </summary>
        public static ISequenceService CreateService(string connectionString)
        {
            return new SequenceService(connectionString);
        }

        /// <summary>
        /// 获取下一个序列号（自动创建连接和事务）
        /// </summary>
        public async Task<string> GetNextNumberAsync(string tableName, string groupKey, string numberFormat = "0000")
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await GetNextNumberAsync(connection, transaction, tableName, groupKey, numberFormat);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 获取下一个序列号（使用现有连接和事务）
        /// </summary>
        public async Task<string> GetNextNumberAsync(
            SqlConnection connection,
            SqlTransaction? transaction,
            string tableName,
            string groupKey,
            string numberFormat = "0000")
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("表名不能为空", nameof(tableName));
            
            if (string.IsNullOrEmpty(groupKey))
                throw new ArgumentException("分组键不能为空", nameof(groupKey));

            try
            {
                int currentNum;

                // 1. 尝试更新现有记录并获取新编号（使用锁）
                string updateSql = @"
                    UPDATE sequenceNumbers WITH (UPDLOCK, HOLDLOCK)
                    SET currentNumber = currentNumber + 1,
                        modifiedOn = GETDATE()
                    OUTPUT INSERTED.currentNumber
                    WHERE tableName = @TableName AND groupKey = @GroupKey";

                using (var updateCommand = new SqlCommand(updateSql, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@TableName", tableName);
                    updateCommand.Parameters.AddWithValue("@GroupKey", groupKey);

                    var result = await updateCommand.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                    {
                        // 更新成功，获得新编号
                        currentNum = Convert.ToInt32(result);
                    }
                    else
                    {
                        // 2. 记录不存在，插入新记录
                        string insertSql = @"
                            INSERT INTO sequenceNumbers (tableName, groupKey, currentNumber, numberFormat, createdOn, modifiedOn)
                            VALUES (@TableName, @GroupKey, 1, @NumberFormat, GETDATE(), GETDATE())";

                        using (var insertCommand = new SqlCommand(insertSql, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@TableName", tableName);
                            insertCommand.Parameters.AddWithValue("@GroupKey", groupKey);
                            insertCommand.Parameters.AddWithValue("@NumberFormat", numberFormat);

                            await insertCommand.ExecuteNonQueryAsync();
                        }

                        currentNum = 1;
                    }
                }

                // 3. 格式化编号
                string formattedNumber = currentNum.ToString().PadLeft(numberFormat.Length, '0');

                Debug.WriteLine($"[SequenceService] 生成序列号成功: {tableName}.{groupKey} = {formattedNumber}");

                return formattedNumber;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SequenceService] 获取序列号失败: {ex.Message}");
                throw new Exception($"获取序列号失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重置指定分组的序列号
        /// </summary>
        public async Task ResetSequenceAsync(string tableName, string groupKey, int resetTo = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE sequenceNumbers 
                SET currentNumber = @ResetTo, 
                    modifiedOn = GETDATE()
                WHERE tableName = @TableName AND groupKey = @GroupKey";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@GroupKey", groupKey);
            command.Parameters.AddWithValue("@ResetTo", resetTo);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected == 0)
            {
                throw new Exception($"重置序列号失败: 找不到对应的记录 (TableName={tableName}, GroupKey={groupKey})");
            }

            Debug.WriteLine($"[SequenceService] 重置序列号成功: {tableName}.{groupKey} -> {resetTo}");
        }

        /// <summary>
        /// 获取当前序列号（不递增）
        /// </summary>
        public async Task<int> GetCurrentNumberAsync(string tableName, string groupKey)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT currentNumber 
                FROM sequenceNumbers WITH (NOLOCK)
                WHERE tableName = @TableName AND groupKey = @GroupKey";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@GroupKey", groupKey);

            var result = await command.ExecuteScalarAsync();
            
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}

