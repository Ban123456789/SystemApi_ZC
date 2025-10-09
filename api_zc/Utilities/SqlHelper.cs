using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace Accura_MES.Utilities
{
    /// <summary>
    /// sql 相關的公用工具類別
    /// </summary>
    public class SqlHelper
    {

        /// <summary>
        /// 通用執行異步交易方法。
        /// 內部管理 SqlConnection 且不允許外部傳入。
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<T> ExecuteTransactionAsync<T>(string connectionString, Func<SqlTransaction, SqlCommand, Task<T>> action)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string is required.");
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;

                            // 執行使用者提供的邏輯並返回結果
                            var result = await action(transaction, command);

                            // 成功提交交易
                            await transaction.CommitAsync();

                            return result;
                        }
                    }
                    catch
                    {
                        // 回滾交易
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 通用執行異步交易方法。
        /// 強制呼叫者負責提供 SqlConnection。
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<T> ExecuteTransactionAsync<T>(SqlConnection connection, Func<SqlTransaction, SqlCommand, Task<T>> action)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection), "A valid SQL connection is required.");
            }

            if (connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("The provided connection must be open.");
            }

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        var result = await action(transaction, command);

                        await transaction.CommitAsync();

                        return result;
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        /// <summary>
        /// 通用連線與交易管理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="connectionString"></param>
        /// <param name="externalConnection"></param>
        /// <param name="externalTransaction"></param>
        /// <returns></returns>
        /// <remarks>
        /// 可接收外部資料庫連線與交易，若無則內部管理連線與交易
        /// </remarks>
        public async Task<T> ExecuteWithTransactionAsync<T>(
            Func<SqlConnection, SqlTransaction, Task<T>> operation,
            string? connectionString,
            SqlConnection? externalConnection = null,
            SqlTransaction? externalTransaction = null)
        {
            // 檢查輸入的連線與字串狀態
            if (connectionString == null && externalConnection == null)
            {
                throw new ArgumentNullException("Either connection string or external connection must be provided.");
            }


            bool manageConnection = externalConnection == null;
            SqlConnection? connection = externalConnection;

            try
            {
                if (manageConnection)
                {
                    // 如果沒有提供外部連線，建立新連線
                    connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                }

                // 確保有一個有效的 Transaction
                SqlTransaction? transaction = externalTransaction ?? connection?.BeginTransaction();

                try
                {
                    // 執行業務邏輯
                    T result = await operation(connection!, transaction!);

                    // 如果是方法內部管理的 Transaction，提交
                    if (externalTransaction == null)
                    {
                        await transaction!.CommitAsync();
                    }

                    return result;
                }
                catch
                {
                    // 如果是方法內部管理的 Transaction，回滾
                    if (externalTransaction == null)
                    {
                        transaction?.Rollback();
                    }
                    throw;
                }
            }
            finally
            {
                // 如果是方法內部管理的連線，關閉連線
                if (manageConnection && connection != null)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// 嘗試回滾並釋放交易資源。
        /// 不會報錯，僅在 Debug 模式下輸出錯誤訊息。
        /// </summary>
        /// <param name="transaction">要回滾和釋放的交易</param>
        /// <returns></returns>
        public static async Task RollbackAndDisposeTransactionAsync(SqlTransaction? transaction)
        {
            if (transaction != null)
            {
                try
                {
                    if (transaction.Connection != null && transaction.Connection.State == System.Data.ConnectionState.Open)
                    {
                        await transaction.RollbackAsync();
                    }
                }
                catch (Exception rollbackEx)
                {
                    Debug.WriteLine($"Transaction rollback failed: {rollbackEx.Message}");
                }
                finally
                {
                    try
                    {
                        await transaction.DisposeAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        Debug.WriteLine("Transaction already disposed.");
                    }
                }
            }
        }


        /// <summary>
        /// 嘗試回滾並釋放交易資源。
        /// 不會報錯，僅在 Debug 模式下輸出錯誤訊息。
        /// </summary>
        /// <param name="transaction">要回滾和釋放的交易</param>
        /// <returns></returns>
        public static void RollbackAndDisposeTransaction(SqlTransaction? transaction)
        {
            if (transaction != null)
            {
                try
                {
                    if (transaction.Connection != null && transaction.Connection.State == System.Data.ConnectionState.Open)
                    {
                        transaction.Rollback();
                    }
                }
                catch (Exception rollbackEx)
                {
                    Debug.WriteLine($"Transaction rollback failed: {rollbackEx.Message}");
                }
                finally
                {
                    try
                    {
                        transaction.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        Debug.WriteLine("Transaction already disposed.");
                    }
                }
            }
        }


        /// <summary>
        /// 通用注入參數方法，幫助判斷參數 object 型別
        /// </summary>
        /// <param name="command">參數注入之目標指令</param>
        /// <param name="param">Key : 參數名, Value : 參數值</param>
        public static void AddCommandParameters(SqlCommand command, KeyValuePair<string, object?> param)
        {
            object? value = param.Value;

            if (value is DateTime dateTimeValue)
            {
                // 日期類型
                DateTime adjustedDateTime = new DateTime(
                    dateTimeValue.Year,
                    dateTimeValue.Month,
                    dateTimeValue.Day,
                    dateTimeValue.Hour,
                    dateTimeValue.Minute,
                    dateTimeValue.Second    // 只取到秒，避免秒後位數被資料庫四捨五入
                );

                command.Parameters.Add(param.Key, SqlDbType.DateTime).Value = adjustedDateTime;

            }
            else if (value is TimeSpan TimeSpanValue)
            {
                // 日期類型
                command.Parameters.Add(param.Key, SqlDbType.Time).Value = TimeSpanValue;
            }
            else if (value is int intValue)
            {
                // 整數類型
                command.Parameters.Add(param.Key, SqlDbType.Int).Value = intValue;
            }
            else if (value is decimal decimalValue)
            {
                // 小數類型
                command.Parameters.Add(param.Key, SqlDbType.Decimal).Value = decimalValue;
            }
            else if (value is double doubleValue)
            {
                // 雙精度浮點數類型
                command.Parameters.Add(param.Key, SqlDbType.Float).Value = doubleValue;
            }
            else if (value is bool boolValue)
            {
                // 布林類型
                command.Parameters.Add(param.Key, SqlDbType.Bit).Value = boolValue;
            }
            else if (value is string stringValue)
            {
                // 字串類型
                command.Parameters.Add(param.Key, SqlDbType.NVarChar).Value = stringValue;
            }
            else if (value == null || value == DBNull.Value)
            {
                // Null 值
                command.Parameters.AddWithValue(param.Key, DBNull.Value);
            }
            else if (value is Dictionary<string, object> dictionaryValue)
            {
                // Dictionary 類型，轉換為 JSON 字串
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 避免編碼特殊字元
                };

                string jsonString = System.Text.Json.JsonSerializer.Serialize(dictionaryValue, options);
                command.Parameters.Add(param.Key, SqlDbType.NVarChar).Value = jsonString;
            }
            else if (value.GetType().IsAnonymousType())
            {
                // 匿名類型，轉換為 JSON 字串
                string jsonString = System.Text.Json.JsonSerializer.Serialize(value);
                command.Parameters.Add(param.Key, SqlDbType.NVarChar).Value = jsonString;
            }
            else if (value is JsonElement jsonElement)
            {
                // jsonElement 類型，根據其值類型進行處理
                AddJsonElement(jsonElement);
            }
            else
            {
                command.Parameters.AddWithValue(param.Key, value);  // 預設處理方式
            }

            // 處理 JsonElement 類型的參數
            void AddJsonElement(JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        command.Parameters.Add(param.Key, SqlDbType.NVarChar).Value = jsonElement.GetString();
                        break;
                    case JsonValueKind.Number:
                        {
                            if (jsonElement.TryGetInt32(out int intValue2))
                            {
                                command.Parameters.Add(param.Key, SqlDbType.Int).Value = intValue2;
                            }
                            else if (jsonElement.TryGetDecimal(out decimal decimalValue2))
                            {
                                command.Parameters.Add(param.Key, SqlDbType.Decimal).Value = decimalValue2;
                            }
                            else if (jsonElement.TryGetDouble(out double doubleValue2))
                            {
                                command.Parameters.Add(param.Key, SqlDbType.Float).Value = doubleValue2;
                            }
                            else
                            {
                                command.Parameters.AddWithValue(param.Key, value); // 預設處理方式
                            }

                            break;
                        }

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        command.Parameters.Add(param.Key, SqlDbType.Bit).Value = jsonElement.GetBoolean();
                        break;
                    case JsonValueKind.Null:
                        command.Parameters.AddWithValue(param.Key, DBNull.Value);
                        break;
                    default:
                        command.Parameters.AddWithValue(param.Key, value);  // 預設處理方式
                        break;

                }
            }
        }

        /// <summary>
        /// 通用注入參數方法，幫助判斷參數 object 型別
        /// </summary>
        /// <param name="dr">參數注入之目標</param>
        /// <param name="columnName">欄位名稱</param>
        /// <param name="value">欄位的值</param>
        public static void AddCommandParameters(DataRow dr, string columnName, object? value)
        {
            if (value is DateTime dateTimeValue)
            {
                // 日期類型
                DateTime adjustedDateTime = new DateTime(
                    dateTimeValue.Year,
                    dateTimeValue.Month,
                    dateTimeValue.Day,
                    dateTimeValue.Hour,
                    dateTimeValue.Minute,
                    dateTimeValue.Second    // 只取到秒，避免秒後位數被資料庫四捨五入
                );

                dr[columnName] = adjustedDateTime;
            }
            else if (value is TimeSpan TimeSpanValue)
            {
                // 日期類型
                dr[columnName] = TimeSpanValue;
            }
            else if (value is int intValue)
            {
                // 整數類型
                dr[columnName] = intValue;
            }
            else if (value is decimal decimalValue)
            {
                // 小數類型
                dr[columnName] = decimalValue;
            }
            else if (value is double doubleValue)
            {
                // 雙精度浮點數類型
                dr[columnName] = doubleValue;
            }
            else if (value is bool boolValue)
            {
                // 布林類型
                dr[columnName] = boolValue;
            }
            else if (value is string stringValue)
            {
                // 字串類型
                dr[columnName] = stringValue;
            }
            else if (value == null || value == DBNull.Value)
            {
                // Null 值
                dr[columnName] = DBNull.Value;
            }
            else if (value is Dictionary<string, object> dictionaryValue)
            {
                // Dictionary 類型，轉換為 JSON 字串
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 避免編碼特殊字元
                };

                string jsonString = System.Text.Json.JsonSerializer.Serialize(dictionaryValue, options);
                dr[columnName] = jsonString;
            }
            else if (value.GetType().IsAnonymousType())
            {
                // 匿名類型，轉換為 JSON 字串
                string jsonString = System.Text.Json.JsonSerializer.Serialize(value);
                dr[columnName] = jsonString;
            }
            else if (value is JsonElement jsonElement)
            {
                // jsonElement 類型，根據其值類型進行處理
                AddJsonElement(jsonElement);
            }
            else
            {
                dr[columnName] = value;  // 預設處理方式
            }

            // 處理 JsonElement 類型的參數
            void AddJsonElement(JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        dr[columnName] = jsonElement.GetString();
                        break;
                    case JsonValueKind.Number:
                        {
                            if (jsonElement.TryGetInt32(out int intValue2))
                            {
                                dr[columnName] = intValue2;
                            }
                            else if (jsonElement.TryGetDecimal(out decimal decimalValue2))
                            {
                                dr[columnName] = decimalValue2;
                            }
                            else if (jsonElement.TryGetDouble(out double doubleValue2))
                            {
                                dr[columnName] = doubleValue2;
                            }
                            else
                            {
                                dr[columnName] = value; // 預設處理方式
                            }

                            break;
                        }

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        dr[columnName] = jsonElement.GetBoolean();
                        break;
                    case JsonValueKind.Null:
                        dr[columnName] = DBNull.Value;
                        break;
                    default:
                        dr[columnName] = value;  // 預設處理方式
                        break;

                }
            }
        }

    }


    // 擴充方法檢測是否為匿名類型
    public static class TypeExtensions
    {
        public static bool IsAnonymousType(this Type type)
        {
            return type.IsGenericType
                && (type.Name.Contains("AnonymousType") || type.Name.StartsWith("<>"))
                && (type.Attributes & System.Reflection.TypeAttributes.Public) == 0;
        }
    }
}
