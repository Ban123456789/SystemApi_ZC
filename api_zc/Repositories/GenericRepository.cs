using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Models;
using Accura_MES.Service;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Utilities.Zlib;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Accura_MES.Repositories
{

    public class GenericRepository : IGenericRepository
    {
        private IPropertyRepository _propertyRepository;
        private readonly string _connectionString;

        private GenericRepository(IPropertyRepository propertyRepository, string connectionString)
        {
            _propertyRepository = propertyRepository;
            _connectionString = connectionString;
        }

        /// <summary>
        /// 靜態工廠方法
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static GenericRepository CreateRepository(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            var propertyRepository = PropertyRepository.CreateRepository(connectionString);

            // 返回此物件
            return new GenericRepository(propertyRepository, connectionString);
        }

        /// <summary>
        /// 靜態工廠方法
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="dataSheet"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static GenericRepository CreateRepository(string? connectionString, string? dataSheet)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            var propertyRepository = PropertyRepository.CreateRepository(connectionString);

            // 返回此物件
            return new GenericRepository(propertyRepository, connectionString);
        }


        public List<Dictionary<string, object>> GenericGet(InnerSearch innerSearch)
        {
            // 如果輸入條件是空的
            if (innerSearch.Datas is null || !innerSearch.Datas.Any() || innerSearch.Datas.All(string.IsNullOrWhiteSpace))
            {
                // 返回空的陣列
                return new List<Dictionary<string, object>>();
            }


            // 使用 SqlConnection 建立連接
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // 構建 SQL 查詢
                string query = $"SELECT * FROM [{innerSearch.Datasheet}] WHERE [{innerSearch.Dataname}] IN ({string.Join(",", innerSearch.Datas.Select((_, index) => $"@data{index}"))})";

                // 使用 SqlCommand 並傳入查詢和連接對象
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // 添加查詢參數，使用參數來避免 SQL 注入
                    for (int i = 0; i < innerSearch.Datas.Count; i++)
                    {

                        //所有輸入改變數?
                        command.Parameters.AddWithValue($"@data{i}", innerSearch.Datas[i]);
                    }

                    // 打開連接
                    connection.Open();

                    // 使用 SqlDataReader 來讀取查詢結果
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var sqlresult = new List<Dictionary<string, object>>();

                        // 讀取結果
                        while (reader.Read())
                        {
                            var dic = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                // 濾掉密碼
                                if (!reader.GetName(i).Equals("password"))
                                {
                                    dic.Add(reader.GetName(i), reader.GetValue(i));
                                }
                            }

                            sqlresult.Add(dic);
                        }


                        return sqlresult;

                    }
                }
            }
        }

        public int CountRows(SqlConnection connection, SqlTransaction? transaction, string tableName)
        {
            int result = 0;

            // 構建 SQL 查詢
            string query = $"SELECT COUNT(*) FROM [{tableName}] WHERE isDelete = 0";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // 讀取結果
                    while (reader.Read())
                    {
                        result = reader.GetInt32(0);
                    }
                }
            }

            return result;
        }


        public List<Dictionary<string, object?>> GenericGetNotDelete(InnerSearch innerSearch, SqlConnection sqlConnection, SqlTransaction? sqlTransaction)
        {
            // 如果輸入條件是空的
            if (innerSearch.Datas is null || !innerSearch.Datas.Any() || innerSearch.Datas.All(string.IsNullOrWhiteSpace))
            {
                // 返回空的陣列
                return new List<Dictionary<string, object?>>();
            }

            // 構建 SQL 查詢
            string query = $"SELECT *" +
                $" FROM [{innerSearch.Datasheet}]" +
                $" WHERE [{innerSearch.Dataname}] IN ({string.Join(",", innerSearch.Datas.Select((_, index) => $"@data{index}"))})" +
                $" AND isDelete = 0";


            // 使用 SqlCommand 並傳入查詢和連接對象
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                if (sqlTransaction != null)
                {
                    command.Transaction = sqlTransaction;
                }

                // 添加查詢參數，使用參數來避免 SQL 注入
                for (int i = 0; i < innerSearch.Datas?.Count; i++)
                {

                    //所有輸入改變數?
                    command.Parameters.AddWithValue($"@data{i}", innerSearch.Datas[i]);
                }

                // 使用 SqlDataReader 來讀取查詢結果
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    var sqlresult = new List<Dictionary<string, object?>>();

                    // 讀取結果
                    while (reader.Read())
                    {
                        var dic = new Dictionary<string, object?>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            // 濾掉密碼
                            if (!reader.GetName(i).Equals("password"))
                            {
                                dic.Add(reader.GetName(i), reader.GetValue(i));
                            }
                        }

                        sqlresult.Add(dic);
                    }


                    return sqlresult;

                }
            }

        }

        public async Task<int> GenericUpdate(long user, TableDatas shareInfo, SqlConnection sqlConnection, SqlTransaction? sqlTransaction)
        {
            try
            {
                int affectedNumber = 0;

                var queries = new List<string>();
                var allParameters = new Dictionary<string, object>();

                int rowIndex = 0;
                foreach (var row in shareInfo.DataStructure)
                {
                    var setClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    row["modifiedBy"] = user;

                    foreach (var kvp in row)
                    {
                        if (!InputFilters.NonEditableFields.Contains(kvp.Key))
                        {
                            string paramKey = $"@{kvp.Key}_{rowIndex}";

                            if (kvp.Key != "id")
                            {
                                setClauses.Add($"{kvp.Key} = {paramKey}");
                            }

                            parameters[paramKey] = kvp.Value;
                        }
                    }

                    string query = $"UPDATE [{shareInfo.Datasheet}] SET {string.Join(", ", setClauses)} WHERE Id = @Id_{rowIndex}";
                    queries.Add(query);

                    foreach (var param in parameters)
                    {
                        allParameters[param.Key] = param.Value;
                    }

                    rowIndex++;
                }

                string combinedQuery = string.Join("; ", queries);

                using (SqlCommand command = new SqlCommand(combinedQuery, sqlConnection))
                {
                    if (sqlTransaction != null)
                    {
                        command.Transaction = sqlTransaction;
                    }

                    foreach (var param in allParameters)
                    {
                        object value = param.Value;

                        SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(param.Key, value));

                    }


                    affectedNumber += await command.ExecuteNonQueryAsync();
                }


                return affectedNumber;
            }
            catch
            {
                throw;
            }
        }

        public async Task<ResponseObject> GenericConcat(
            SqlConnection connection, SqlTransaction? transaction,
            long user,
            string tableName,
            Dictionary<string, string> columnsToUpdate,
            Dictionary<string, object> whereCondition,
            bool isFront)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, null, null, "通用 Concat 發生未知錯誤");
            try
            {
                // 動態生成 SQL 更新語句
                var setClauses = new List<string>();
                foreach (var column in columnsToUpdate)
                {
                    string setClause = isFront
                        ? $"{column.Key} = CONCAT(@AppendString_{column.Key}, {column.Key})"  // 接在前面
                        : $"{column.Key} = CONCAT({column.Key}, @AppendString_{column.Key})"; // 接在後面
                    setClauses.Add(setClause);
                }

                var whereClauses = new List<string>();
                foreach (var column in whereCondition)
                {
                    string whereClause = $"{column.Key} = {column.Value}";

                    whereClauses.Add(whereClause);
                }

                string query = $"UPDATE {tableName} " +
                    $"SET {string.Join(", ", setClauses)} " +
                    $"WHERE {string.Join("AND ", whereClauses)};";

                Debug.WriteLine(query);

                using (var command = new SqlCommand(query, connection, transaction))
                {
                    // 添加參數
                    foreach (var column in columnsToUpdate)
                    {
                        // 根據輸入在不同欄位接上不同的字串
                        SqlHelper.AddCommandParameters(
                            command, new KeyValuePair<string, object?>($"@AppendString_{column.Key}", column.Value));
                    }
                    foreach (var column in whereCondition)
                    {
                        // 設定更新條件
                        SqlHelper.AddCommandParameters(
                            command, new KeyValuePair<string, object?>($"@{column.Key}", column.Value));
                    }

                    // 執行更新操作
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                }

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                return responseObject;
            }
            catch (CustomErrorCodeException ex) // 接到自訂的 error code
            {

                responseObject.SetErrorCode(ex.SelfErrorCode, $"通用 Concat 發生自訂例外: {ex}");

                return responseObject;
            }
            catch (Exception ex)    // 其餘 error code
            {

                responseObject.SetErrorCode(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, $"通用 Concat 發生系統例外: {ex}");

                return responseObject;
            }
        }

        public async Task<long?> Upsert(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            long user,
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            try
            {
                // 將 values.Keys 轉為忽略大小寫的集合
                var keysIgnoreCase = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);

                // 檢查輸入參數 : values.Keys 必須包含 primaryKeys
                if (!primaryKeys.All(key => keysIgnoreCase.Contains(key)))
                {
                    throw new ArgumentException(
                        $"發生於 [通用 UPSERT]:" +
                        $"參數 values [{string.Join(", ", values.Keys)}] 必須包含 primaryKeys [{string.Join(", ", primaryKeys)}]");
                }


                // Generate column, parameter names, and key conditions
                var columnNames = string.Join(", ", values.Where(dict => dict.Key != "id").Select(dict => $"[{dict.Key}]")) + ", [createdBy]" + ", [modifiedBy]"; // 排除 'id'，避免 IDENTITY_INSERT 錯誤
                var sourceColumns = string.Join(", ", values.Keys.Select(key => $"@{key} AS [{key}]")) + ", @createdBy AS [createdBy] , @modifiedBy AS [modifiedBy]";
                var parameterNames = string.Join(", ", values.Where(dict => dict.Key != "id").Select(dict => "@" + dict.Key)) + ", @createdBy" + ", @modifiedBy"; // 排除 'id'，避免 IDENTITY_INSERT 錯誤
                var updateSetClause = string.Join(", ", values.Where(dict => !primaryKeys.Contains(dict.Key)).Select(dict => $"[{dict.Key}] = @{dict.Key}")) + ", [modifiedBy] = @modifiedBy"; // 排除主鍵欄位
                var keyConditions = new StringBuilder();

                foreach (var key in primaryKeys)
                {
                    if (keyConditions.Length > 0)
                    {
                        keyConditions.Append(" AND ");
                    }

                    keyConditions.Append($"Target.[{key}] = Source.[{key}]");
                }

                // Build the SQL UPSERT query
                string query = $@"
                    DECLARE @UpsertedIds TABLE (Id BIGINT);

                    MERGE INTO [{tableName}] AS Target
                    USING (SELECT {sourceColumns}) AS Source
                        ON {keyConditions}
                    WHEN MATCHED THEN
                        UPDATE SET {updateSetClause}
                    WHEN NOT MATCHED THEN
                        INSERT ({columnNames})
                        VALUES ({parameterNames})
                    OUTPUT INSERTED.id INTO @UpsertedIds(Id);

                    SELECT DISTINCT Id FROM @UpsertedIds;
                    ";

                using var command = new SqlCommand(query, connection, transaction);

                // Add parameters to the command
                foreach (var kvp in values)
                {
                    SqlHelper.AddCommandParameters(command, kvp);
                }

                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("createdBy", user));
                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("modifiedBy", user));

                List<long> insertedIds = new List<long>();

                // 執行命令並讀取返回數據
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader[0] != DBNull.Value)
                        {
                            insertedIds.Add(reader.GetInt64(0));
                        }
                    }
                }

                // 如果沒有插入或更新的記錄
                if (insertedIds.Count == 0)
                {
                    return null;
                }
                else
                {
                    return insertedIds.First(); // 返回 id
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        public async Task Upsert_SqlBulkCopy(
            string tableName,
            DataTable dataTable,
            HashSet<string> primaryKeys,
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            string tempTableName = $"#{tableName.Substring(0, Math.Min(tableName.Length, 50))}_Temp_{Guid.NewGuid()}";

            try
            {
                // 建立臨時表
                using (var command = new SqlCommand(GenericSqlQueryGenerator.GenerateCreateTableStatement(dataTable, tempTableName), connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 檢查臨時表是否成功建立
                string checkTempTableSql = $"IF OBJECT_ID('tempdb..[{tempTableName}]') IS NULL THROW 50000, 'Temporary table not created.', 1;";
                using (var command = new SqlCommand(checkTempTableSql, connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 使用 SqlBulkCopy 將資料插入到臨時表
                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tempTableName;

                    // 明確指定欄位對應
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                }

                // 3. 使用 MERGE 語句進行 UPSERT
                string mergeSql = $@"
                    MERGE INTO {tableName} AS Target
                    USING (SELECT DISTINCT * FROM [{tempTableName}]) AS Source
                    ON {string.Join(" AND ", primaryKeys.Select(pk => $"Target.{pk} = Source.{pk}"))}
                    WHEN MATCHED THEN
                        UPDATE SET {string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => !primaryKeys.Contains(c.ColumnName)).Select(c => $"{c.ColumnName} = Source.{c.ColumnName}"))} -- 排除主鍵欄位
                    WHEN NOT MATCHED THEN
                        INSERT ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "id").Select(c => c.ColumnName))})   -- 排除 'id'，避免 IDENTITY_INSERT 錯誤
                        VALUES ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "id").Select(c => $"Source.{c.ColumnName}"))});
                ";
                Debug.WriteLine(mergeSql);
                using (var mergeCmd = new SqlCommand(mergeSql, connection, transaction))
                {
                    await mergeCmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                // 清理臨時表
                string dropTempTableSql = $"DROP TABLE IF EXISTS [{tempTableName}];";
                using (var command = new SqlCommand(dropTempTableSql, connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task Upsert_Delete_SqlBulkCopy(
            string tableName,
            DataTable dataTable,
            HashSet<string> primaryKeys,
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            // 這裡的 tableName 會被用來建立臨時表的名稱
            // 所以要確保它不會太長
            string tempTableName = $"#{tableName.Substring(0, Math.Min(tableName.Length, 50))}_Temp_{Guid.NewGuid()}";

            try
            {
                // 建立臨時表
                using (var command = new SqlCommand(GenericSqlQueryGenerator.GenerateCreateTableStatement(dataTable, tempTableName), connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 檢查臨時表是否成功建立
                string checkTempTableSql = $"IF OBJECT_ID('tempdb..[{tempTableName}]') IS NULL THROW 50000, 'Temporary table not created.', 1;";
                using (var command = new SqlCommand(checkTempTableSql, connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // 使用 SqlBulkCopy 將資料插入到臨時表
                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tempTableName;

                    // 明確指定欄位對應
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                }

                // 3. 使用 MERGE 語句進行 UPSERT + DELETE
                string mergeSql = $@"
                    MERGE INTO {tableName} AS Target
                    USING (SELECT DISTINCT * FROM [{tempTableName}]) AS Source
                    ON {string.Join(" AND ", primaryKeys.Select(pk => $"Target.{pk} = Source.{pk}"))}
                    WHEN MATCHED THEN                   -- 1.source 和 target 對應上 → 做更新
                        UPDATE SET {string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => !primaryKeys.Contains(c.ColumnName)).Select(c => $"{c.ColumnName} = Source.{c.ColumnName}"))} -- 排除主鍵欄位
                    WHEN NOT MATCHED BY TARGET THEN     -- 2.source 有但 target 沒有 → 做新增
                        INSERT ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "id").Select(c => c.ColumnName))}) -- 排除 'id'，避免 IDENTITY_INSERT 錯誤
                        VALUES ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "id").Select(c => $"Source.{c.ColumnName}"))})
                    WHEN NOT MATCHED BY SOURCE THEN     -- 3.target 有但 source 沒有 → 做刪除
                        DELETE;
                ";

                using (var mergeCmd = new SqlCommand(mergeSql, connection, transaction))
                {
                    await mergeCmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                // 清理臨時表
                string dropTempTableSql = $"DROP TABLE IF EXISTS [{tempTableName}];";
                using (var command = new SqlCommand(dropTempTableSql, connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> Update(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            long user,
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            try
            {
                // 將 values.Keys 轉為忽略大小寫的集合
                var keysIgnoreCase = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);

                // 檢查輸入參數 : values.Keys 必須包含 primaryKeys
                if (!primaryKeys.All(key => keysIgnoreCase.Contains(key)))
                {
                    throw new ArgumentException(
                        $"發生於 [通用 UPDATE]:" +
                        $"參數 values [{string.Join(", ", values.Keys)}] 必須包含 primaryKeys [{string.Join(", ", primaryKeys)}]");
                }


                // Generate the SET clause and WHERE condition
                var updateSetClause =
                    string.Join(", ",
                    values
                    .Where(dict => !primaryKeys.Contains(dict.Key))    // 從 value 濾掉條件 key
                    .Select(dict => $"[{dict.Key}] = @{dict.Key}"))
                    + ", [modifiedBy] = @modifiedBy";

                var whereClause = string.Join(" AND ", primaryKeys.Select(key => $"[{key}] = @{key}"));


                // Build the SQL UPDATE query
                string query = $@"
                    UPDATE [{tableName}]
                    SET {updateSetClause}
                    WHERE {whereClause};";

                using var command = new SqlCommand(query, connection, transaction);

                // Add parameters to the command
                foreach (var kvp in values)
                {
                    SqlHelper.AddCommandParameters(command, kvp);
                }

                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("modifiedBy", user));

                // Execute the update query and return the affected rows
                var affectedRows = await command.ExecuteNonQueryAsync();
                return affectedRows;

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> UpdateDate(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            string datePart,
            long user,
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            try
            {
                // 將 values.Keys 轉為忽略大小寫的集合
                var keysIgnoreCase = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);

                // 檢查輸入參數 : values.Keys 必須包含 primaryKeys
                if (!primaryKeys.All(key => keysIgnoreCase.Contains(key)))
                {
                    throw new ArgumentException(
                        $"發生於 [通用 UpdateDate]:" +
                        $"參數 values [{string.Join(", ", values.Keys)}] 必須包含 primaryKeys [{string.Join(", ", primaryKeys)}]");
                }

                // 過濾掉 primaryKeys，只保留要更新的 "日期" 欄位
                var dateColumns = values.Keys.Except(primaryKeys).ToList();
                if (!dateColumns.Any())
                {
                    throw new ArgumentException("沒有可更新的日期欄位");
                }

                // 建構 SET 子句
                var updateSetClause = string.Join(", ", dateColumns.Select(column =>
                    $"[{column}] = DATEADD({datePart}, @{column}, [{column}])"
                ));

                // 增加 modifiedBy 欄位
                updateSetClause += ", [modifiedBy] = @modifiedBy";

                // 建構 WHERE 子句
                var whereClause = string.Join(" AND ", primaryKeys.Select(key => $"[{key}] = @{key}"));

                // Build the SQL UPDATE query
                string query = $@"
                    UPDATE [{tableName}]
                    SET {updateSetClause}
                    WHERE {whereClause};";

                using var command = new SqlCommand(query, connection, transaction);

                // Add parameters to the command
                foreach (var kvp in values)
                {
                    SqlHelper.AddCommandParameters(command, kvp);
                }

                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("modifiedBy", user));

                // Execute the update query and return the affected rows
                var affectedRows = await command.ExecuteNonQueryAsync();
                return affectedRows;

            }
            catch (Exception)
            {
                throw;
            }
        }


        public List<Dictionary<string, object?>> GenericAdvancedGet(AdvancedSearch advancedSearch)
        {
            try
            {
                // 使用 SqlConnection 建立連接
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // 構建 SQL 查詢
                    string query = $"SELECT * FROM [{advancedSearch.Datasheet}] ";

                    if (advancedSearch.And is not null || advancedSearch.Or is not null)
                    {
                        query += "WHERE ";
                    }

                    query += Utilities.Utils.QueryText(advancedSearch.And, advancedSearch.Or, advancedSearch.Order);

                    // 使用 SqlCommand 並傳入查詢和連接對象
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        //And設定
                        int index = 0;
                        if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                        {
                            List<string> andConditions = new List<string>();
                            foreach (QueryObject AndQueryItem in advancedSearch.And)
                            {
                                foreach (string value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                                {
                                    // 添加查詢參數，使用參數來避免 SQL 注入
                                    command.Parameters.AddWithValue($"@data{index++}", value);
                                    query += $"\nvalue{value}";
                                }
                            }
                        }
                        //Or設定
                        if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                        {
                            List<string> andConditions = new List<string>();
                            foreach (QueryObject OrQueryItem in advancedSearch.Or)
                            {
                                foreach (string value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                                {
                                    // 添加查詢參數，使用參數來避免 SQL 注入
                                    command.Parameters.AddWithValue($"@data{index++}", value);
                                    query += $"\nvalue{value}";
                                }
                            }
                        }
                        // 打開連接
                        connection.Open();
                        // 使用 SqlDataReader 來讀取查詢結果
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var sqlresult = new List<Dictionary<string, object?>>();

                            // 讀取結果
                            while (reader.Read())
                            {
                                var sqldata = new Dictionary<string, object?>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    // 濾掉密碼
                                    if (!reader.GetName(i).Equals("password"))
                                    {
                                        sqldata.Add(reader.GetName(i), reader.GetValue(i) == DBNull.Value ? null : reader.GetValue(i));
                                    }
                                }
                                sqlresult.Add(sqldata);
                            }

                            return sqlresult;

                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<Dictionary<string, object?>> GenericAdvancedGet(
            SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch)
        {
            try
            {
                // 構建 SQL 查詢
                string query = $"SELECT * FROM [{advancedSearch.Datasheet}] ";

                if (advancedSearch.And is not null || advancedSearch.Or is not null)
                {
                    query += "WHERE ";
                }

                query += Utilities.Utils.QueryText(advancedSearch.And, advancedSearch.Or, advancedSearch.Order);

                // 使用 SqlCommand 並傳入查詢和連接對象
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    //And設定
                    int index = 0;
                    if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                    {
                        List<string> andConditions = new List<string>();
                        foreach (QueryObject AndQueryItem in advancedSearch.And)
                        {
                            foreach (string value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                            {
                                // 添加查詢參數，使用參數來避免 SQL 注入
                                command.Parameters.AddWithValue($"@data{index++}", value);
                                query += $"\nvalue{value}";
                            }
                        }
                    }
                    //Or設定
                    if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                    {
                        List<string> andConditions = new List<string>();
                        foreach (QueryObject OrQueryItem in advancedSearch.Or)
                        {
                            foreach (string value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                            {
                                // 添加查詢參數，使用參數來避免 SQL 注入
                                command.Parameters.AddWithValue($"@data{index++}", value);
                                query += $"\nvalue{value}";
                            }
                        }
                    }

                    // 使用 SqlDataReader 來讀取查詢結果
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var sqlresult = new List<Dictionary<string, object?>>();

                        // 讀取結果
                        while (reader.Read())
                        {
                            var sqldata = new Dictionary<string, object?>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                // 濾掉密碼
                                if (!reader.GetName(i).Equals("password"))
                                {
                                    sqldata.Add(reader.GetName(i), reader.GetValue(i) == DBNull.Value ? null : reader.GetValue(i));
                                }
                            }
                            sqlresult.Add(sqldata);
                        }

                        return sqlresult;

                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<Dictionary<string, object>> GenericAdvancedGetNotDelete(
            SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch)
        {
            try
            {
                // 構建 SQL 查詢
                string query = $"SELECT * FROM [{advancedSearch.Datasheet}] WHERE isDelete = 0 ";

                if (advancedSearch.And is not null || advancedSearch.Or is not null)
                {
                    query += " AND ";
                }

                query += Utilities.Utils.QueryText(advancedSearch.And, advancedSearch.Or, advancedSearch.Order);

                // 使用 SqlCommand 並傳入查詢和連接對象
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    //And設定
                    int index = 0;
                    if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                    {
                        List<string> andConditions = new List<string>();
                        foreach (QueryObject AndQueryItem in advancedSearch.And)
                        {
                            foreach (string value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                            {
                                // 添加查詢參數，使用參數來避免 SQL 注入
                                //command.Parameters.AddWithValue($"@data{index++}", value);
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>($"@data{index++}", value));
                                query += $"\nvalue{value}";
                            }
                        }
                    }
                    //Or設定
                    if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                    {
                        List<string> andConditions = new List<string>();
                        foreach (QueryObject OrQueryItem in advancedSearch.Or)
                        {
                            foreach (string value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                            {
                                // 添加查詢參數，使用參數來避免 SQL 注入
                                //command.Parameters.AddWithValue($"@data{index++}", value);
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>($"@data{index++}", value));
                                query += $"\nvalue{value}";
                            }
                        }
                    }

                    // 使用 SqlDataReader 來讀取查詢結果
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var sqlresult = new List<Dictionary<string, object>>();

                        // 讀取結果
                        while (reader.Read())
                        {
                            var sqldata = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {

                                // 濾掉密碼
                                if (!reader.GetName(i).Equals("password"))
                                {
                                    sqldata.Add(reader.GetName(i), reader.GetValue(i));
                                }
                            }
                            sqlresult.Add(sqldata);
                        }

                        return sqlresult;

                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public Dictionary<string, object?> GenericAdvancedGetTop(SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch)
        {
            try
            {
                // 使用 SqlConnection 建立連接
                // 構建 SQL 查詢
                string query = $"SELECT TOP 1 * FROM [{advancedSearch.Datasheet}] WHERE isDelete = 0 ";


                if (advancedSearch.And is not null || advancedSearch.Or is not null)
                {
                    query += " AND ";
                }

                query += Utilities.Utils.QueryText(advancedSearch.And, advancedSearch.Or, advancedSearch.Order);


                // 使用 SqlCommand 並傳入查詢和連接對象
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    //And設定
                    int index = 0;
                    if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                    {
                        List<string> andConditions = new List<string>();
                        foreach (QueryObject AndQueryItem in advancedSearch.And)
                        {
                            foreach (string value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                            {
                                // 添加查詢參數，使用參數來避免 SQL 注入
                                command.Parameters.AddWithValue($"@data{index++}", value);
                                query += $"\nvalue{value}";
                            }
                        }
                    }
                    //Or設定
                    if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                    {
                        List<string> andConditions = new List<string>();
                        foreach (QueryObject OrQueryItem in advancedSearch.Or)
                        {
                            foreach (string value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                            {
                                // 添加查詢參數，使用參數來避免 SQL 注入
                                command.Parameters.AddWithValue($"@data{index++}", value);
                                query += $"\nvalue{value}";
                            }
                        }
                    }

                    // 使用 SqlDataReader 來讀取查詢結果
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var sqlresult = new List<Dictionary<string, object>>();

                        // 讀取結果
                        while (reader.Read())
                        {
                            var sqldata = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {

                                // 濾掉密碼
                                if (!reader.GetName(i).Equals("password"))
                                {
                                    sqldata.Add(reader.GetName(i), reader.GetValue(i));
                                }
                            }
                            sqlresult.Add(sqldata);
                        }

                        return sqlresult.Any() ? sqlresult.First() : new Dictionary<string, object>();

                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<List<long>> CreateDataGeneric(long user, string tableName, List<Dictionary<string, object?>> input)
        {
            try
            {
                return await SqlHelper.ExecuteTransactionAsync(_connectionString, async (transaction, command) =>
                {
                    var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);

                    var columns = propertyItems
                        .Select(propertyItem => propertyItem.Name)
                        .Except(InputFilters.CreateFilters)
                        .ToList();

                    // 加上user欄位
                    columns.Add("createdBy");
                    columns.Add("modifiedBy");

                    string query = Utilities.QueryBuilder.BuildMultiInsertQuery(tableName, columns, input.Count, out var parametersList);

                    // 設定查詢和參數
                    command.CommandText = query;

                    // 參數注入
                    for (int rowIndex = 0; rowIndex < input.Count; rowIndex++)
                    {
                        // 輸入的值
                        Dictionary<string, object?> inputRow = input[rowIndex];

                        int colIndex = 0;
                        foreach (var column in columns)
                        {
                            // 每個欄位建立對應的參數名稱，例如 @id_0、@companyId_0 等
                            string parameterName = parametersList[rowIndex][colIndex];

                            // 注入參數值
                            if (column == "createdBy" || column == "modifiedBy")
                            {
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(parameterName, user));
                            }
                            else
                            {
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(parameterName, inputRow.GetValueOrDefault(column)));
                            }

                            colIndex++;
                        }
                    }

                    var result = new List<long>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // get all inserted id
                            result.Add(reader.GetInt64(0));
                        }
                    }

                    return result;
                });
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<long>> CreateDataGeneric(SqlConnection connection, SqlTransaction? transaction,
            long user, string tableName, List<Dictionary<string, object?>> input)
        {
            try
            {
                if (input is null || !input.Any())
                {
                    return new List<long>();
                }

                using var command = new SqlCommand();
                command.Connection = connection;

                if (transaction != null)
                    command.Transaction = transaction;

                var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);

                var columns = propertyItems
                    .Select(propertyItem => propertyItem.Name)
                    .Except(InputFilters.CreateFilters)
                    .ToList();

                // 加上user欄位
                columns.Add("createdBy");
                columns.Add("modifiedBy");

                string query = Utilities.QueryBuilder.BuildMultiInsertQuery(tableName, columns, input.Count, out var parametersList);

                // 設定查詢和參數
                command.CommandText = query;

                // 參數注入
                for (int rowIndex = 0; rowIndex < input.Count; rowIndex++)
                {
                    // 輸入的值
                    Dictionary<string, object?> inputRow = input[rowIndex];

                    int colIndex = 0;
                    foreach (var column in columns)
                    {
                        // 每個欄位建立對應的參數名稱，例如 @id_0、@companyId_0 等
                        string parameterName = parametersList[rowIndex][colIndex];

                        // 注入參數值
                        if (column == "createdBy" || column == "modifiedBy")
                        {
                            SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(parameterName, user));
                        }
                        else
                        {
                            SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(parameterName, inputRow.GetValueOrDefault(column)));
                        }

                        colIndex++;
                    }
                }

                var result = new List<long>();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // get all inserted id
                        result.Add(reader.GetInt64(0));
                    }
                }

                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<long>> CreateDataGeneric(SqlConnection connection, SqlTransaction? transaction,
            long user, string tableName, HashSet<string> columnNames, List<Dictionary<string, object?>> input)
        {
            var responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, null, null, "通用建立發生未知錯誤");

            var columns = columnNames
                .Select(name => name)
                .Except(InputFilters.CreateFilters)
                .ToList();

            // 加上user欄位
            columns.Add("createdBy");
            columns.Add("modifiedBy");

            // 建立 query
            string query = Utilities.QueryBuilder.BuildMultiInsertQuery(tableName, columns, input.Count, out var parametersList);

            var command = new SqlCommand(query, connection, transaction);

            // 參數注入
            for (int rowIndex = 0; rowIndex < input.Count; rowIndex++)
            {
                // 輸入的值
                Dictionary<string, object?> inputRow = input[rowIndex];

                int colIndex = 0;
                foreach (var column in columns)
                {
                    // 每個欄位建立對應的參數名稱，例如 @id_0、@companyId_0 等
                    string parameterName = parametersList[rowIndex][colIndex];

                    // 注入參數值
                    if (column == "createdBy" || column == "modifiedBy")
                    {
                        SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(parameterName, user));
                    }
                    else
                    {
                        SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>(parameterName, inputRow.GetValueOrDefault(column)));
                    }

                    colIndex++;
                }
            }

            var insertedIds = new List<long>();

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    // get all inserted id
                    insertedIds.Add(reader.GetInt64(0));
                }
            }


            return insertedIds;
        }


        public bool IsRecordExist(
            SqlConnection connection,
            SqlTransaction? transaction,
            string tableName,
            Dictionary<string, object> columns)
        {
            // 檢查輸入參數
            if (columns == null || !columns.Any())
            {
                throw new ArgumentException("參數 columns 不能為空");
            }

            // 建立一般欄位條件
            var conditions = columns!.Select((kvp, index) =>
                $"{kvp.Key} = @NormalParam{index}").ToList();


            // 組合 SQL 查詢語句
            string query = $@"
                SELECT COUNT(1)
                FROM {tableName}
                WHERE {string.Join(" AND ", conditions)} AND isDelete = 0";

            try
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (transaction != null)
                    {
                        command.Transaction = transaction;
                    }

                    // 設定一般欄位參數
                    int normalIndex = 0;
                    foreach (var kvp in columns!)
                    {
                        SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>($"@NormalParam{normalIndex}", kvp.Value));
                        normalIndex++;
                    }

                    // 執行查詢
                    int count = (int)command.ExecuteScalar();

                    // 如果 count > 0，表示資料存在
                    return count > 0;
                }

            }
            catch
            {
                throw;
            }
        }


        public async Task<int> IsDelete(SqlConnection connection, SqlTransaction? transaction,
            long user, TableDatas shareInfo)
        {
            int affectedRows = 0;   // 受影響列數
            try
            {
                // 初始化批量更新的 SQL 查詢
                var updateCases = new List<string>();
                var idList = new List<string>();
                var modifiedBy = user;

                foreach (var row in shareInfo.DataStructure)
                {
                    var id = row["id"].ToString();
                    var isDelete = row["isDelete"].ToString();

                    // 構建 CASE WHEN 子句
                    updateCases.Add($"WHEN Id = @Id_{id} THEN @IsDelete_{id}");
                    idList.Add($"@Id_{id}");

                }

                // 最終組合的 SQL 查詢
                string query = $@"
                    UPDATE [{shareInfo.Datasheet}]
                    SET IsDelete = CASE {string.Join(" ", updateCases)} END,
                        modifiedBy = @modifiedBy
                    WHERE Id IN ({string.Join(", ", idList)});";

                Debug.WriteLine(query);

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    // 為每個記錄設置參數
                    foreach (var row in shareInfo.DataStructure)
                    {
                        var id = row["id"].ToString();
                        var isDelete = row["isDelete"].ToString();

                        command.Parameters.AddWithValue($"@Id_{id}", id);
                        command.Parameters.AddWithValue($"@IsDelete_{id}", isDelete);
                    }
                    command.Parameters.AddWithValue("@modifiedBy", modifiedBy);

                    // 執行批量更新
                    affectedRows += await command.ExecuteNonQueryAsync();
                }

                return affectedRows;
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> Delete(SqlConnection connection, SqlTransaction? transaction,
            long user, string tableName, List<long> ids)
        {
            int affectedRows = 0;   // 受影響列數
            try
            {
                if (ids is null || !ids.Any())
                {
                    return 0;
                }

                var idList = new List<string>();
                foreach (var id in ids)
                {
                    idList.Add($"@Id_{id}");
                }

                string query = $"DELETE FROM [{tableName}] " +
                    $"WHERE id IN ({string.Join(", ", idList)})";


                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    // 為每個 `id` 添加參數
                    foreach (var id in ids)
                    {
                        command.Parameters.AddWithValue($"@Id_{id}", id);
                    }

                    // 執行批量刪除
                    affectedRows += await command.ExecuteNonQueryAsync();

                }


                return affectedRows;
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> Delete(
            string tableName,
            HashSet<string> primaryKeys,
            Dictionary<string, object?> values,
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            try
            {
                // 將 values.Keys 轉為忽略大小寫的集合
                var keysIgnoreCase = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);

                // 檢查輸入參數 : values.Keys 必須包含 primaryKeys
                if (!primaryKeys.All(key => keysIgnoreCase.Contains(key)))
                {
                    throw new ArgumentException(
                        $"發生於 [通用 Delete]:" +
                        $"參數 values [{string.Join(", ", values.Keys)}] 必須包含 primaryKeys [{string.Join(", ", primaryKeys)}]");
                }


                // Generate the WHERE condition
                var whereClause = string.Join(" AND ", primaryKeys.Select(key => $"[{key}] = @{key}"));


                // Build the SQL DELETE query
                string query = $@"
                    DELETE [{tableName}]
                    WHERE {whereClause};";

                using var command = new SqlCommand(query, connection, transaction);

                // Add parameters to the command
                foreach (var kvp in values)
                {
                    SqlHelper.AddCommandParameters(command, kvp);
                }

                // Execute the query and return the affected rows
                var affectedRows = await command.ExecuteNonQueryAsync();
                return affectedRows;
            }
            catch
            {
                throw;
            }
        }

        #region NestStructureQuery process
        /// <summary>
        /// 巢狀結構關聯表建立SELECT的欄位query helper
        /// SELECT tableName.* => 轉換成顯式選取
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tableName">關聯表名稱</param>
        /// <param name="alias">關聯表別名</param>
        /// <param name="localization">多國語言</param>
        /// <param name="isLocalize">true:本地化;false:保持原樣</param>
        /// <param name="selectColumns">空的: 查詢所有欄位; otherwise查詢指定欄位</param>
        /// <returns></returns>
        /// <remarks>
        /// Helper of <see cref="BuildNestStructureQuery"/> and <see cref="BuildNestStructureQuery_itemlist"/>
        /// </remarks>
        private string BuildQueryForRelateTable(
            SqlConnection connection, SqlTransaction? transaction, string tableName, string alias, string localization, bool isLocalize,
            HashSet<string>? selectColumns = null)
        {
            var queryBuilder = new StringBuilder();

            // 取得關聯表詳細資訊
            var properties = _propertyRepository.GetProperty(tableName, connection, transaction);

            // 遍歷所有欄位資訊
            int rowIndex = 0;
            foreach (var property in properties)
            {
                string row = property.Name;

                // 如果指定了查詢欄位，則檢查當前欄位是否在指定的欄位中
                if (selectColumns != null && !selectColumns.Contains(row))
                {
                    continue;
                }

                // 處理多國語言
                if (property.PropertyType.Equals("i18n") && isLocalize)
                {
                    // 舉例:ISNULL(JSON_VALUE(JSON_QUERY(identity0.label), '$."zh-TW"'), identity0.id) AS label
                    // -- 這個部分會把字串轉成JSON，然後從JSON取出value，預設值是id
                    queryBuilder.Append($"            ISNULL(JSON_VALUE(JSON_QUERY({alias}.{row}), '$.\"{localization}\"'), {alias}.id) AS \"{row}\"");
                }

                // 一般欄位
                else
                {
                    queryBuilder.Append($"            {alias}.{row} AS \"{row}\"");
                }


                // finally.判斷要不要加上逗號
                // finally.A.如果沒有指定查詢欄位:
                if (selectColumns is null || selectColumns.Count == 0)
                {
                    // 若不是最後一個欄位，則添加逗號
                    if (rowIndex < properties.Count - 1)
                        queryBuilder.AppendLine(",");
                }
                // finally.B.如果有指定查詢欄位:
                else
                {
                    // 若不是最後一個欄位，則添加逗號
                    if (rowIndex < selectColumns.Count - 1)
                        queryBuilder.AppendLine(",");
                }

                rowIndex++;
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// 巢狀結構 query 建立 WHERE 跟 Order by 子句
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Helper of <see cref="BuildNestStructureQuery"/> and <see cref="BuildNestStructureQuery_itemlist"/>
        /// </remarks>
        private static string QueryTextForNest(AdvancedSearch advancedSearch)
        {
            List<QueryObject>? AndQuery = advancedSearch.And;
            List<QueryObject>? OrQuery = advancedSearch.Or;
            List<QueryObject>? OrderQuery = advancedSearch.Order;
            string? SQL = advancedSearch.SQL;   // 特殊關鍵字

            string QueryStr = string.Empty;
            int index = 0;
            if (AndQuery != null && AndQuery.Count > 0)
            {
                List<string> andConditions = new List<string>();
                foreach (QueryObject AndQueryItem in AndQuery)
                {
                    // 特殊運算元
                    if (AndQueryItem.Operate == "empty")
                    {
                        // 舉例:(main.name IS NULL or main.name = '')
                        andConditions.Add($"(main.{Utilities.Utils.RemoveSpecialCharacters(AndQueryItem.Field)} IS NULL OR main.{Utilities.Utils.RemoveSpecialCharacters(AndQueryItem.Field)} = '')");
                    }

                    // 一般運算元
                    else
                    {
                        andConditions.Add($"main.{Utilities.Utils.RemoveSpecialCharacters(AndQueryItem.Field)} {Utilities.Utils.OperateCase(AndQueryItem.Operate, AndQueryItem.Value, ref index)}");
                    }
                }

                QueryStr += string.Join(" AND ", andConditions);
            }
            if (OrQuery != null && OrQuery.Count > 0)
            {
                List<string> orConditions = new List<string>();

                if (!string.IsNullOrEmpty(QueryStr))
                {
                    orConditions.Add(QueryStr);
                }
                foreach (QueryObject OrQueryItem in OrQuery)
                {
                    // 特殊運算元
                    if (OrQueryItem.Operate == "empty")
                    {
                        // 舉例:(main.name IS NULL or main.name = '')
                        orConditions.Add($"(main.{Utilities.Utils.RemoveSpecialCharacters(OrQueryItem.Field)} IS NULL OR main.{Utilities.Utils.RemoveSpecialCharacters(OrQueryItem.Field)} = '')");
                    }

                    // 一般運算元
                    else
                    {
                        orConditions.Add($"main.{Utilities.Utils.RemoveSpecialCharacters(OrQueryItem.Field)} {Utilities.Utils.OperateCase(OrQueryItem.Operate, OrQueryItem.Value, ref index)}");
                    }
                }

                QueryStr = string.Join(" OR ", orConditions);
            }

            // 特殊關鍵字
            if (advancedSearch.Datasheet.Equals("exceptiondays", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(SQL))
            {
                QueryStr = QueryStr + " " + SQL;
            }

            // 如果有 WHERE 子句，則添加 WHERE 到字串開頭
            if (!string.IsNullOrEmpty(QueryStr))
            {
                QueryStr = " WHERE " + QueryStr;
            }

            if (OrderQuery != null && OrderQuery.Any())
            {
                int orderIndex = 1; // 計算第幾個 order
                foreach (QueryObject OrderQueryItem in OrderQuery)
                {
                    if (!string.IsNullOrEmpty(OrderQueryItem.Value))
                    {
                        string sortOrder = OrderQueryItem.Value.ToUpper().Equals("ASC") ? "ASC" : "DESC";

                        if (orderIndex == 1)
                            QueryStr += $" ORDER BY main.{Utilities.Utils.RemoveSpecialCharacters(OrderQueryItem.Field)} {sortOrder}";
                        else
                            QueryStr += $" , main.{Utilities.Utils.RemoveSpecialCharacters(OrderQueryItem.Field)} {sortOrder}";
                    }

                    orderIndex++;
                }
            }

            return QueryStr;
        }

        /// <summary>
        /// 巢狀結構query建立 WHERE 子句
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Helper of <see cref="BuildNestStructureQuery"/> and <see cref="BuildNestStructureQuery_itemlist"/>
        /// </remarks>
        private static string QueryTextForNest(AdvancedSearchObj advancedSearch)
        {
            List<QueryObjectObj>? AndQuery = advancedSearch.And;
            List<QueryObjectObj>? OrQuery = advancedSearch.Or;
            List<QueryObjectObj>? OrderQuery = advancedSearch.Order;
            string? SQL = advancedSearch.SQL;   // 特殊關鍵字

            string QueryStr = string.Empty;
            int index = 0;
            if (AndQuery != null && AndQuery.Count > 0)
            {
                List<string> andConditions = new List<string>();
                foreach (var AndQueryItem in AndQuery)
                {
                    // 特殊運算元
                    if (AndQueryItem.Operate == "empty")
                    {
                        // 舉例:(main.name IS NULL or main.name = '')
                        andConditions.Add($"(main.{Utilities.Utils.RemoveSpecialCharacters(AndQueryItem.Field)} IS NULL OR main.{Utilities.Utils.RemoveSpecialCharacters(AndQueryItem.Field)} = '')");
                    }

                    // 一般運算元
                    else
                    {
                        andConditions.Add($"main.{Utilities.Utils.RemoveSpecialCharacters(AndQueryItem.Field)} {Utilities.Utils.OperateCase(AndQueryItem.Operate, AndQueryItem.Value, ref index)}");
                    }
                }

                QueryStr += string.Join(" AND ", andConditions);
            }
            if (OrQuery != null && OrQuery.Count > 0)
            {
                List<string> orConditions = new List<string>();

                if (!string.IsNullOrEmpty(QueryStr))
                {
                    orConditions.Add(QueryStr);
                }
                foreach (var OrQueryItem in OrQuery)
                {
                    // 特殊運算元
                    if (OrQueryItem.Operate == "empty")
                    {
                        // 舉例:(main.name IS NULL or main.name = '')
                        orConditions.Add($"(main.{Utilities.Utils.RemoveSpecialCharacters(OrQueryItem.Field)} IS NULL OR main.{Utilities.Utils.RemoveSpecialCharacters(OrQueryItem.Field)} = '')");
                    }

                    // 一般運算元
                    else
                    {
                        orConditions.Add($"main.{Utilities.Utils.RemoveSpecialCharacters(OrQueryItem.Field)} {Utilities.Utils.OperateCase(OrQueryItem.Operate, OrQueryItem.Value, ref index)}");
                    }
                }

                QueryStr = string.Join(" OR ", orConditions);
            }

            // 特殊關鍵字
            if (advancedSearch.Datasheet.Equals("exceptiondays", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(SQL))
            {
                QueryStr = QueryStr + " " + SQL;
            }

            // 如果有 WHERE 子句，則添加 WHERE 到字串開頭
            if (!string.IsNullOrEmpty(QueryStr))
            {
                QueryStr = " WHERE " + QueryStr;
            }


            if (OrderQuery != null && OrderQuery.Any())
            {
                int orderIndex = 1; // 計算第幾個 order
                foreach (var OrderQueryItem in OrderQuery)
                {
                    string? orderValue = OrderQueryItem.Value?.ToString();

                    if (!string.IsNullOrEmpty(orderValue))
                    {
                        string sortOrder = orderValue.ToUpper().Equals("ASC") ? "ASC" : "DESC";

                        if (orderIndex == 1)
                            QueryStr += $" ORDER BY main.{Utilities.Utils.RemoveSpecialCharacters(OrderQueryItem.Field)} {sortOrder}";
                        else
                            QueryStr += $" , main.{Utilities.Utils.RemoveSpecialCharacters(OrderQueryItem.Field)} {sortOrder}";
                    }

                    orderIndex++;
                }
            }


            return QueryStr;
        }

        #region 巢狀條件測試 W.I.P.
        public (string SqlQuery, List<SqlParameter> Parameters) BuildWhereClauseFromJson(string jsonString)
        {
            var conditionGroup = JsonSerializer.Deserialize<ConditionGroup>(jsonString);
            var parameters = new List<SqlParameter>();
            int paramCounter = 0; // 用於生成唯一參數名稱
            string whereClause = BuildConditions(conditionGroup.Conditions, parameters, ref paramCounter);
            return (whereClause, parameters);
        }

        private string BuildConditions(List<Condition> conditions, List<SqlParameter> parameters, ref int paramCounter)
        {
            var sqlConditions = new List<string>();

            foreach (var condition in conditions)
            {
                if (condition.Logic != null && condition.NestedConditions != null)
                {
                    // 處理嵌套條件
                    var nestedCondition = BuildConditions(condition.NestedConditions, parameters, ref paramCounter);
                    sqlConditions.Add($"({nestedCondition})");
                }
                else
                {
                    // 處理普通條件
                    if (condition.Operator.ToUpper() == "IN" && condition.Value is IEnumerable<object> enumerable)
                    {
                        var inParameters = new List<string>();

                        foreach (var value in enumerable)
                        {
                            var paramName = $"@Param{paramCounter++}"; // 確保 paramCounter 在此作用域內安全遞增
                            parameters.Add(new SqlParameter(paramName, value));
                            inParameters.Add(paramName);
                        }

                        sqlConditions.Add($"{condition.Field} IN ({string.Join(", ", inParameters)})");
                    }

                    else
                    {
                        // 處理其他操作符
                        string parameterName = $"@Param{paramCounter++}";
                        if (!condition.Operator.ToUpper().Contains("NULL"))
                        {
                            parameters.Add(new SqlParameter(parameterName, condition.Value));
                        }
                        sqlConditions.Add($"{condition.Field} {condition.Operator} {parameterName}");
                    }
                }
            }

            return string.Join($" {conditions.First().Logic ?? "AND"} ", sqlConditions);
        }

        #endregion

        /// <summary>
        /// 建立巢狀query，若欄位propertyType = 'item'or'itemlist'，則將欄位展開成JSON_QUERY
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="advancedSearch"></param>
        /// <param name="properties">資料表所有欄位的詳細資訊</param>
        /// <param name="localization">多國語言</param>
        /// <returns>回傳一般版query; 如果包含"itemlist"欄位則會回傳進階版巢狀結構query</returns>
        /// <remarks>
        /// Helper of <see cref="GetNestedStructureData"/>
        /// </remarks>
        private string BuildNestStructureQuery(SqlConnection connection, SqlTransaction? transaction, AdvancedSearch advancedSearch, List<PropertyModel> properties, string localization)
        {
            string? tableName = string.Empty;                                                    // 資料表名稱
            List<string> columns = new List<string>();                                          // 所有欄位名稱
            Dictionary<string, string> jsonDataSource_item = new Dictionary<string, string>();  // 記錄"item"欄位的reference table name

            // 1.紀錄表名
            tableName = advancedSearch.Datasheet;

            // 檢查欄位資訊(propertyType)
            int duplicateCount = 0;
            foreach (var rowProperty in properties)
            {
                // 2.紀錄該表所有欄位名
                columns.Add(rowProperty.Name);

                // 3.記錄所有reference table name
                if (rowProperty.PropertyType.Equals("item"))
                {
                    // 檢查jsonDataSource有沒有重複的table name，如果有則將表名後面接上的數字+1
                    if (jsonDataSource_item.ContainsValue(rowProperty.DataSource + $"-{duplicateCount}"))
                        duplicateCount++;

                    jsonDataSource_item.Add(rowProperty.Name, rowProperty.DataSource + $"-{duplicateCount}");
                }

                // ===有"itemlist"欄位===
                else if (rowProperty.PropertyType.Equals("itemlist"))
                {
                    // 另外製作query
                    return BuildNestStructureQuery_itemlist(
                        connection,
                        transaction,
                        advancedSearch,
                        properties,
                        _propertyRepository.GetDataSource(rowProperty.DataSource),
                        localization);
                }
            }

            // ===沒有"itemlist"欄位的情況===
            // 開始建構query
            var queryBuilder = new StringBuilder();

            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine($"    '{tableName}' AS \"objectType\",");

            // 構建每個欄位的查詢部分
            int rowIndex = 0;
            foreach (var property in properties)
            {
                string row = property.Name;

                // 判斷是否需要查詢指定欄位: 如果 advancedSearch.SelectColumns_First 不為空，則只查詢指定欄位
                if (advancedSearch.SelectPrimaryColumns != null && advancedSearch.SelectPrimaryColumns.Count > 0)
                {
                    // 檢查欄位是否在指定的欄位列表中
                    if (!advancedSearch.SelectPrimaryColumns.Contains(row))
                    {
                        continue;
                    }
                }

                // 遇到需要展開成JSON的欄位:"item"
                if (jsonDataSource_item.ContainsKey(row) && !string.IsNullOrEmpty(jsonDataSource_item[row]))
                {
                    var refTableName = jsonDataSource_item[row].Split("-")[0];                // datasource table name
                    var duplicateIndex = int.Parse(jsonDataSource_item[row].Split("-")[1]);   // 紀錄每個重複表名的index

                    // 使用 JSON_QUERY 包裝欄位
                    queryBuilder.Append($"    JSON_QUERY( \n" +
                        "        ( \n" +
                        "            SELECT \n" +
                        $"            '{refTableName}' AS \"objectType\", \n" +
                        $"{BuildQueryForRelateTable(connection, transaction, refTableName, refTableName + duplicateIndex, localization, advancedSearch.Localization, advancedSearch.SelectForeignColumns.GetValueOrDefault(row))} \n" +
                        "            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES \n" +
                        "        ) \n" +
                        $"    ) AS \"{row}\"");
                }

                // 處理多國語言
                else if (property.PropertyType.Equals("i18n") && advancedSearch.Localization)
                {
                    // 舉例:ISNULL(JSON_VALUE(JSON_QUERY(identity0.label), '$."zh-TW"'), identity0.id) AS label
                    // -- 這個部分會把字串轉成JSON，然後從JSON取出value，預設值是id
                    queryBuilder.Append($"    ISNULL(JSON_VALUE(JSON_QUERY(main.{row}), '$.\"{localization}\"'), main.id) AS \"{row}\"");
                }

                // 一般欄位
                else
                {
                    queryBuilder.Append($"    main.{row} AS \"{row}\"");
                }

                // finally.判斷要不要加上逗號
                // finally.A.如果沒有指定查詢欄位:
                if (advancedSearch.SelectPrimaryColumns is null || advancedSearch.SelectPrimaryColumns.Count == 0)
                {
                    // 若不是最後一個欄位，則添加逗號
                    if (rowIndex < columns.Count - 1)
                        queryBuilder.AppendLine(",");
                }
                // finally.B.如果有指定查詢欄位:
                else
                {
                    // 若不是最後一個欄位，則添加逗號
                    if (rowIndex < advancedSearch.SelectPrimaryColumns.Count - 1)
                        queryBuilder.AppendLine(",");
                }

                rowIndex++;
            }

            // 指定主要表
            queryBuilder.AppendLine();
            queryBuilder.AppendLine("FROM");
            queryBuilder.AppendLine($"    [{tableName}] AS main");

            // 指定json data source
            if (jsonDataSource_item.Count > 0)
                foreach (var dataSource in jsonDataSource_item)
                {
                    if (!string.IsNullOrEmpty(dataSource.Value))
                    {
                        string refTableName = dataSource.Value.Split("-")[0];
                        int duplicateIndex = int.Parse(dataSource.Value.Split("-")[1]);    // 避免重複表名的index

                        queryBuilder.AppendLine($"    LEFT JOIN [{refTableName}] AS {refTableName + duplicateIndex} ON {refTableName + duplicateIndex}.id = main.{dataSource.Key}");
                    }
                }

            // WHERE 子句
            string whereClause = QueryTextForNest(advancedSearch);
            if (whereClause.Length > 0)
                queryBuilder.AppendLine($"{whereClause}");

            // END of query
            queryBuilder.AppendLine("    FOR JSON PATH, INCLUDE_NULL_VALUES");

            return queryBuilder.ToString();
        }

        /// <summary>
        /// 建立巢狀query，若欄位propertyType = 'item'or'itemlist'，則將欄位展開成JSON_QUERY
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="advancedSearch"></param>
        /// <param name="properties">資料表所有欄位的詳細資訊</param>
        /// <param name="localization">多國語言</param>
        /// <returns>回傳一般版query; 如果包含"itemlist"欄位則會回傳進階版巢狀結構query</returns>
        /// <remarks>
        /// Helper of <see cref="GetNestedStructureData"/>
        /// </remarks>
        private string BuildNestStructureQuery(SqlConnection connection, SqlTransaction? transaction, AdvancedSearchObj advancedSearch, List<PropertyModel> properties, string localization)
        {
            string? tableName = string.Empty;                                                    // 資料表名稱
            List<string> columns = new List<string>();                                          // 所有欄位名稱
            Dictionary<string, string> jsonDataSource_item = new Dictionary<string, string>();  // 記錄"item"欄位的reference table name

            // 1.紀錄表名
            tableName = advancedSearch.Datasheet;

            // 檢查欄位資訊(propertyType)
            int duplicateCount = 0;
            foreach (var rowProperty in properties)
            {
                // 2.紀錄該表所有欄位名
                columns.Add(rowProperty.Name);

                // 3.記錄所有reference table name
                if (rowProperty.PropertyType.Equals("item"))
                {
                    // 檢查jsonDataSource有沒有重複的table name，如果有則將表名後面接上的數字+1
                    if (jsonDataSource_item.ContainsValue(rowProperty.DataSource + $"-{duplicateCount}"))
                        duplicateCount++;

                    jsonDataSource_item.Add(rowProperty.Name, rowProperty.DataSource + $"-{duplicateCount}");
                }

                // ===有"itemlist"欄位===
                else if (rowProperty.PropertyType.Equals("itemlist"))
                {
                    // 另外製作query
                    return BuildNestStructureQuery_itemlist(
                        connection,
                        transaction,
                        advancedSearch,
                        properties,
                        _propertyRepository.GetDataSource(rowProperty.DataSource),
                        localization);
                }
            }

            // ===沒有"itemlist"欄位的情況===
            // 開始建構query
            var queryBuilder = new StringBuilder();

            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine($"    '{tableName}' AS \"objectType\",");

            // 構建每個欄位的查詢部分
            int rowIndex = 0;
            foreach (var property in properties)
            {
                string row = property.Name;

                // 遇到需要展開成JSON的欄位:"item"
                if (jsonDataSource_item.ContainsKey(row) && !string.IsNullOrEmpty(jsonDataSource_item[row]))
                {
                    var refTableName = jsonDataSource_item[row].Split("-")[0];                // datasource table name
                    var duplicateIndex = int.Parse(jsonDataSource_item[row].Split("-")[1]);   // 紀錄每個重複表名的index

                    // 使用 JSON_QUERY 包裝欄位
                    queryBuilder.Append($"    JSON_QUERY( \n" +
                        "        ( \n" +
                        "            SELECT \n" +
                        $"            '{refTableName}' AS \"objectType\", \n" +
                        $"{BuildQueryForRelateTable(connection, transaction, refTableName, refTableName + duplicateIndex, localization, advancedSearch.Localization)} \n" +
                        "            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES \n" +
                        "        ) \n" +
                        $"    ) AS \"{row}\"");
                }

                // 處理多國語言
                else if (property.PropertyType.Equals("i18n") && advancedSearch.Localization)
                {
                    // 舉例:ISNULL(JSON_VALUE(JSON_QUERY(identity0.label), '$."zh-TW"'), identity0.id) AS label
                    // -- 這個部分會把字串轉成JSON，然後從JSON取出value，預設值是id
                    queryBuilder.Append($"    ISNULL(JSON_VALUE(JSON_QUERY(main.{row}), '$.\"{localization}\"'), main.id) AS \"{row}\"");
                }

                // 一般欄位
                else
                {
                    queryBuilder.Append($"    main.{row} AS \"{row}\"");
                }

                // 若不是最後一個欄位，則添加逗號
                if (rowIndex < columns.Count - 1)
                {
                    queryBuilder.AppendLine(",");
                }

                rowIndex++;
            }

            // 指定主要表
            queryBuilder.AppendLine();
            queryBuilder.AppendLine("FROM");
            queryBuilder.AppendLine($"    [{tableName}] AS main");

            // 指定json data source
            if (jsonDataSource_item.Count > 0)
                foreach (var dataSource in jsonDataSource_item)
                {
                    if (!string.IsNullOrEmpty(dataSource.Value))
                    {
                        string refTableName = dataSource.Value.Split("-")[0];
                        int duplicateIndex = int.Parse(dataSource.Value.Split("-")[1]);    // 避免重複表名的index

                        queryBuilder.AppendLine($"    LEFT JOIN [{refTableName}] AS {refTableName + duplicateIndex} ON {refTableName + duplicateIndex}.id = main.{dataSource.Key}");
                    }
                }

            // WHERE 子句
            string whereClause = QueryTextForNest(advancedSearch);
            if (whereClause.Length > 0)
                queryBuilder.AppendLine($"{whereClause}");

            // END of query
            queryBuilder.AppendLine("    FOR JSON PATH, INCLUDE_NULL_VALUES");

            return queryBuilder.ToString();
        }

        /// <summary>
        /// 有"itemlist"欄位時使用此 query，
        /// 把所有可能出現的sourceType取出來，再用CASE WHEN來選擇sourceType
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="advancedSearch"></param>
        /// <param name="properties">主表詳細資訊</param>
        /// <param name="DataSource">所有關聯表名</param>
        /// <param name="localization">多國語言</param>
        /// <returns></returns>
        /// <remarks>
        /// Helper of <see cref="BuildNestStructureQuery"/>
        /// </remarks>
        /// 
        private string BuildNestStructureQuery_itemlist(
            SqlConnection connection,
            SqlTransaction transaction,
            AdvancedSearch advancedSearch,
            List<PropertyModel> properties,
            List<string> DataSource,
            string localization)
        {
            var tableName = string.Empty;                                       // 資料表名稱
            var columnNames = new List<string>();                               // 所有欄位名稱
            var jsonDataSource_item = new Dictionary<string, string>();         // 記錄"item"欄位的reference table name

            // 1.紀錄表名
            tableName = advancedSearch.Datasheet;

            int duplicateCount = 0;
            foreach (var columnProperty in properties)
            {
                // 2.紀錄該表所有欄位名
                columnNames.Add(columnProperty.Name);

                // 3.記錄所有reference table name
                if (columnProperty.PropertyType.Equals("item"))
                {
                    // 檢查jsonDataSource有沒有重複的table name，如果有則將表名後面接上的數字+1
                    if (jsonDataSource_item.ContainsValue(columnProperty.DataSource + $"-{duplicateCount}"))
                        duplicateCount++;

                    jsonDataSource_item.Add(columnProperty.Name, columnProperty.DataSource + $"-{duplicateCount}");
                }

                else if (columnProperty.PropertyType.Equals("itemlist"))
                {
                    // "sourceId"欄位展開成JSON_QUERY
                    jsonDataSource_item.Add("sourceId", "sourceTable");
                }
            }

            // 開始建構query
            var queryBuilder = new StringBuilder();

            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine($"    '{tableName}' AS \"objectType\",");

            // 構建每個欄位的查詢部分
            int rowIndex = 0;
            foreach (var property in properties)
            {
                var row = property.Name;

                // 判斷是否需要查詢指定欄位: 如果 advancedSearch.SelectColumns_First 不為空，則只查詢指定欄位
                if (advancedSearch.SelectPrimaryColumns != null && advancedSearch.SelectPrimaryColumns.Count > 0)
                {
                    // 檢查欄位是否在指定的欄位列表中
                    if (!advancedSearch.SelectPrimaryColumns.Contains(row))
                    {
                        continue;
                    }
                }

                // 遇到需要展開成JSON的欄位
                if (jsonDataSource_item.ContainsKey(row) && !string.IsNullOrEmpty(jsonDataSource_item[row]))
                {
                    string refTableName = jsonDataSource_item[row].Split("-")[0];

                    // "itemlist"
                    if (refTableName == "sourceTable")
                    {
                        // start of JSON_QUERY
                        queryBuilder.Append("    JSON_QUERY( \n" +
                            "        ( \n" +
                            "            SELECT\n" +
                            $"           CASE WHEN main.sourceType = '{DataSource[0]}' THEN\n" +
                            $"               (SELECT '{DataSource[0]}' AS \"objectType\",\n" +
                            $"{BuildQueryForRelateTable(connection, transaction, DataSource[0], DataSource[0], localization, advancedSearch.Localization, advancedSearch.SelectForeignColumns.GetValueOrDefault(row))} \n" +
                            "                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)\n");

                        // other cases
                        for (int j = 1; j < DataSource.Count; j++)
                        {
                            queryBuilder.Append($"           WHEN main.sourceType = '{DataSource[j]}' THEN\n" +
                                $"               (SELECT '{DataSource[j]}' AS \"objectType\",\n" +
                                $"{BuildQueryForRelateTable(connection, transaction, DataSource[j], DataSource[j], localization, advancedSearch.Localization, advancedSearch.SelectForeignColumns.GetValueOrDefault(row))} \n" +
                                $"               FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)\n");
                        }


                        // end of JSON_QUERY
                        queryBuilder.Append("            END\n" +
                            "        ) \n" +
                            $"    ) AS \"{row}\"");
                    }
                    // "item"
                    else
                    {
                        int duplicateIndex = int.Parse(jsonDataSource_item[row].Split("-")[1]);
                        queryBuilder.Append($"    JSON_QUERY( \n" +
                            "        ( \n" +
                            "            SELECT \n" +
                            $"            '{refTableName}' AS \"objectType\", \n" +
                            $"{BuildQueryForRelateTable(connection, transaction, refTableName, refTableName + duplicateIndex, localization, advancedSearch.Localization, advancedSearch.SelectForeignColumns.GetValueOrDefault(row))} \n" +
                            "            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES \n" +
                            "        ) \n" +
                            $"    ) AS \"{row}\"");
                    }
                }

                // 處理多國語言
                else if (property.PropertyType.Equals("i18n") && advancedSearch.Localization)
                {
                    // 舉例:ISNULL(JSON_VALUE(JSON_QUERY(identity0.label), '$."zh-TW"'), identity0.id) AS label
                    // -- 這個部分會把字串轉成JSON，然後從JSON取出value，預設值是id
                    queryBuilder.Append($"    ISNULL(JSON_VALUE(JSON_QUERY(main.{row}), '$.\"{localization}\"'), main.id) AS \"{row}\"");
                }

                // 一般欄位
                else
                {
                    // 直接添加欄位
                    queryBuilder.Append($"    main.{row} AS \"{row}\"");
                }

                // finally.判斷要不要加上逗號
                // finally.A.如果沒有指定查詢欄位:
                if (advancedSearch.SelectPrimaryColumns is null || advancedSearch.SelectPrimaryColumns.Count == 0)
                {
                    // 若不是最後一個欄位，則添加逗號
                    if (rowIndex < columnNames.Count - 1)
                        queryBuilder.AppendLine(",");
                }
                // finally.B.如果有指定查詢欄位:
                else
                {
                    // 若不是最後一個欄位，則添加逗號
                    if (rowIndex < advancedSearch.SelectPrimaryColumns.Count - 1)
                        queryBuilder.AppendLine(",");
                }

                rowIndex++;
            }

            // 指定主要表
            queryBuilder.AppendLine();
            queryBuilder.AppendLine("FROM");
            queryBuilder.AppendLine($"    [{tableName}] AS main");

            // 指定json data source
            if (jsonDataSource_item.Count > 0)
                foreach (var dataSource in jsonDataSource_item)
                {
                    if (!string.IsNullOrEmpty(dataSource.Value))
                    {
                        string refTableName = dataSource.Value.Split("-")[0];

                        // "itemlist"
                        if (refTableName == "sourceTable")
                        {
                            // 因為每個表的"itemlist"欄位只會有一筆，在主要表與"item關聯表"都有別名的情況下，應該不會發生"itemlist關聯表"與其它表重名，所以"itemlist關聯表"不需要別名
                            for (int j = 0; j < DataSource.Count; j++)
                            {
                                queryBuilder.AppendLine($"    LEFT JOIN [{DataSource[j]}] ON {DataSource[j]}.id = main.sourceId AND main.sourceType = '{DataSource[j]}'");
                            }
                        }
                        // "item"
                        else
                        {
                            int duplicateIndex = int.Parse(dataSource.Value.Split("-")[1]);    // 避免重複表名的index
                            queryBuilder.AppendLine($"    LEFT JOIN [{refTableName}] AS {refTableName + duplicateIndex} ON {refTableName + duplicateIndex}.id = main.{dataSource.Key}");
                        }
                    }
                }

            // WHERE 子句
            if (advancedSearch.And is null)
                advancedSearch.And = new List<QueryObject>();

            // 加入外鍵關聯表到 WHERE 條件
            advancedSearch.And.Add(
                new QueryObject
                {
                    Field = "sourceType",
                    Operate = "in",
                    Value = string.Join(",", DataSource)
                }
            );

            // 解析並包裝 WHERE 條件
            string whereClaue = QueryTextForNest(advancedSearch);

            queryBuilder.AppendLine(whereClaue);

            // END of query
            queryBuilder.AppendLine("    FOR JSON PATH, INCLUDE_NULL_VALUES");


            return queryBuilder.ToString();
        }

        /// <summary>
        /// 把所有可能出現的sourceType取出來，再用CASE WHEN來選擇sourceType
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="advancedSearch"></param>
        /// <param name="properties">主表詳細資訊</param>
        /// <param name="DataSource">所有關聯表名</param>
        /// <param name="localization">多國語言</param>
        /// <returns></returns>
        /// <remarks>
        /// Helper of <see cref="BuildNestStructureQuery"/>
        /// </remarks>
        private string BuildNestStructureQuery_itemlist(
            SqlConnection connection,
            SqlTransaction? transaction,
            AdvancedSearchObj advancedSearch,
            List<PropertyModel> properties,
            List<string> DataSource,
            string localization)
        {
            var tableName = string.Empty;                                       // 資料表名稱
            var columnNames = new List<string>();                               // 所有欄位名稱
            var jsonDataSource_item = new Dictionary<string, string>();         // 記錄"item"欄位的reference table name

            // 1.紀錄表名
            tableName = advancedSearch.Datasheet;

            int duplicateCount = 0;
            foreach (var columnProperty in properties)
            {
                // 2.紀錄該表所有欄位名
                columnNames.Add(columnProperty.Name);

                // 3.記錄所有reference table name
                if (columnProperty.PropertyType.Equals("item"))
                {
                    // 檢查jsonDataSource有沒有重複的table name，如果有則將表名後面接上的數字+1
                    if (jsonDataSource_item.ContainsValue(columnProperty.DataSource + $"-{duplicateCount}"))
                        duplicateCount++;

                    jsonDataSource_item.Add(columnProperty.Name, columnProperty.DataSource + $"-{duplicateCount}");
                }

                else if (columnProperty.PropertyType.Equals("itemlist"))
                {
                    // "sourceId"欄位展開成JSON_QUERY
                    jsonDataSource_item.Add("sourceId", "sourceTable");
                }
            }

            // 開始建構query
            var queryBuilder = new StringBuilder();

            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine($"    '{tableName}' AS \"objectType\",");

            // 構建每個欄位的查詢部分
            int rowIndex = 0;
            foreach (var property in properties)
            {
                var row = property.Name;

                // 遇到需要展開成JSON的欄位
                if (jsonDataSource_item.ContainsKey(row) && !string.IsNullOrEmpty(jsonDataSource_item[row]))
                {
                    string refTableName = jsonDataSource_item[row].Split("-")[0];

                    // "itemlist"
                    if (refTableName == "sourceTable")
                    {
                        // start of JSON_QUERY
                        queryBuilder.Append("    JSON_QUERY( \n" +
                            "        ( \n" +
                            "            SELECT\n" +
                            $"           CASE WHEN main.sourceType = '{DataSource[0]}' THEN\n" +
                            $"               (SELECT '{DataSource[0]}' AS \"objectType\",\n" +
                            $"{BuildQueryForRelateTable(connection, transaction, DataSource[0], DataSource[0], localization, advancedSearch.Localization)} \n" +
                            "                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)\n");

                        // other cases
                        for (int j = 1; j < DataSource.Count; j++)
                        {
                            queryBuilder.Append($"           WHEN main.sourceType = '{DataSource[j]}' THEN\n" +
                                $"               (SELECT '{DataSource[j]}' AS \"objectType\",\n" +
                                $"{BuildQueryForRelateTable(connection, transaction, DataSource[j], DataSource[j], localization, advancedSearch.Localization)} \n" +
                                $"               FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)\n");
                        }


                        // end of JSON_QUERY
                        queryBuilder.Append("            END\n" +
                            "        ) \n" +
                            $"    ) AS \"{row}\"");
                    }
                    // "item"
                    else
                    {
                        int duplicateIndex = int.Parse(jsonDataSource_item[row].Split("-")[1]);
                        queryBuilder.Append($"    JSON_QUERY( \n" +
                            "        ( \n" +
                            "            SELECT \n" +
                            $"            '{refTableName}' AS \"objectType\", \n" +
                            $"{BuildQueryForRelateTable(connection, transaction, refTableName, refTableName + duplicateIndex, localization, advancedSearch.Localization)} \n" +
                            "            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES \n" +
                            "        ) \n" +
                            $"    ) AS \"{row}\"");
                    }
                }

                // 處理多國語言
                else if (property.PropertyType.Equals("i18n") && advancedSearch.Localization)
                {
                    // 舉例:ISNULL(JSON_VALUE(JSON_QUERY(identity0.label), '$."zh-TW"'), identity0.id) AS label
                    // -- 這個部分會把字串轉成JSON，然後從JSON取出value，預設值是id
                    queryBuilder.Append($"    ISNULL(JSON_VALUE(JSON_QUERY(main.{row}), '$.\"{localization}\"'), main.id) AS \"{row}\"");
                }

                // 一般欄位
                else
                {
                    // 直接添加欄位
                    queryBuilder.Append($"    main.{row} AS \"{row}\"");
                }

                // 若不是最後一個欄位，則添加逗號
                if (rowIndex < columnNames.Count - 1)
                {
                    queryBuilder.AppendLine(",");
                }

                rowIndex++;
            }

            // 指定主要表
            queryBuilder.AppendLine();
            queryBuilder.AppendLine("FROM");
            queryBuilder.AppendLine($"    [{tableName}] AS main");

            // 指定json data source
            if (jsonDataSource_item.Count > 0)
                foreach (var dataSource in jsonDataSource_item)
                {
                    if (!string.IsNullOrEmpty(dataSource.Value))
                    {
                        string refTableName = dataSource.Value.Split("-")[0];

                        // "itemlist"
                        if (refTableName == "sourceTable")
                        {
                            // 因為每個表的"itemlist"欄位只會有一筆，在主要表與"item關聯表"都有別名的情況下，應該不會發生"itemlist關聯表"與其它表重名，所以"itemlist關聯表"不需要別名
                            for (int j = 0; j < DataSource.Count; j++)
                            {
                                queryBuilder.AppendLine($"    LEFT JOIN [{DataSource[j]}] ON {DataSource[j]}.id = main.sourceId AND main.sourceType = '{DataSource[j]}'");
                            }
                        }
                        // "item"
                        else
                        {
                            int duplicateIndex = int.Parse(dataSource.Value.Split("-")[1]);    // 避免重複表名的index
                            queryBuilder.AppendLine($"    LEFT JOIN [{refTableName}] AS {refTableName + duplicateIndex} ON {refTableName + duplicateIndex}.id = main.{dataSource.Key}");
                        }
                    }
                }

            // WHERE 子句
            if (advancedSearch.And is null)
                advancedSearch.And = new List<QueryObjectObj>();

            // 加入外鍵關聯表到 WHERE 條件
            advancedSearch.And.Add(
                new QueryObjectObj
                {
                    Field = "sourceType",
                    Operate = "in",
                    Value = string.Join(",", DataSource)
                }
            );

            string whereClaue = QueryTextForNest(advancedSearch);

            queryBuilder.AppendLine(whereClaue);


            // END of query
            queryBuilder.AppendLine("    FOR JSON PATH, INCLUDE_NULL_VALUES");


            return queryBuilder.ToString();
        }
        #endregion


        public List<Dictionary<string, object>>? GetNestedStructureData(AdvancedSearch advancedSearch, string localization)
        {
            try
            {


                using (SqlConnection connection = new SqlConnection(_connectionString))
                {

                    // 打開連接
                    connection.Open();

                    // 取得主資料表詳細資訊
                    var tableInfo = _propertyRepository.GetProperty(advancedSearch.Datasheet, connection, null);
                    if (tableInfo == null || tableInfo.Count == 0)
                    {
                        throw new CustomErrorCodeException(SelfErrorCode.DATASHEET_NOT_ALLOW);
                    }

                    // 構建 SQL 查詢
                    string query = BuildNestStructureQuery(connection, null, advancedSearch, tableInfo, localization);

                    // 使用 SqlCommand 並傳入查詢和連接對象
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // And設定
                        int index = 0;
                        if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                        {
                            List<string> andConditions = new List<string>();
                            foreach (QueryObject AndQueryItem in advancedSearch.And)
                            {
                                // 特殊運算元
                                if (AndQueryItem.Operate == "empty") { continue; }

                                foreach (string value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                                {
                                    // 添加查詢參數，使用參數來避免 SQL 注入
                                    command.Parameters.AddWithValue($"@data{index++}", value);
                                }
                            }
                        }
                        // Or設定
                        if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                        {
                            List<string> andConditions = new List<string>();
                            foreach (QueryObject OrQueryItem in advancedSearch.Or)
                            {
                                // 特殊運算元
                                if (OrQueryItem.Operate == "empty") { continue; }

                                foreach (string value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                                {
                                    // 添加查詢參數，使用參數來避免 SQL 注入
                                    command.Parameters.AddWithValue($"@data{index++}", value);
                                }
                            }
                        }


                        // 使用 SqlDataReader 來讀取查詢結果
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var sqlResult_JSONString = new StringBuilder(); // 接收sql reader回傳，如果是 DBNull.Value，那麼 ToString() 會返回 null
                            var sqlResult_ALLJSON = new List<string>();     // 接收所有sqlResult_JSONString
                            var sqlResult_final = new List<List<Dictionary<string, object>>>();       // 接收最後結果

                            // Loop through each result set
                            // 雖然這個query只會有一個result set
                            do
                            {
                                sqlResult_JSONString.Clear();
                                var resultSet = new List<Dictionary<string, object>>();

                                // 用此方式能夠避免JSON內容太長(2033 characters limit，會被截斷成json array)
                                while (reader.Read())
                                {
                                    // 接收sql查詢結果
                                    sqlResult_JSONString.Append(reader.GetValue(0).ToString());
                                }

                                // Add this result set to the list of all results
                                if (sqlResult_JSONString.Length > 0)
                                    sqlResult_ALLJSON.Add(sqlResult_JSONString.ToString());

                            } while (reader.NextResult()); // Move to the next result set


                            // 資料庫查無資料
                            if (sqlResult_ALLJSON.Count == 0)
                            {
                                return null;
                            }
                            else
                            {
                                foreach (string sqlResult in sqlResult_ALLJSON)
                                {
                                    // Deserialize the JSON string to a List of Dictionary<string, object>
                                    var deserializedList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(sqlResult);

                                    // Remove sensitive fields
                                    foreach (var dict in deserializedList)
                                        Utilities.Utils.RemoveSensitiveData(dict);

                                    // Add the processed dictionary to the result
                                    sqlResult_final.Add(deserializedList);

                                }

                                // 因為sql的回傳只會有一筆，所以用[0]取第一筆資料就好
                                return sqlResult_final[0];
                            }

                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<Dictionary<string, object>>? GetNestedStructureData(AdvancedSearchObj advancedSearch, string localization)
        {
            try
            {

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // 打開連接
                    connection.Open();

                    // 取得主資料表詳細資訊
                    var tableInfo = _propertyRepository.GetProperty(advancedSearch.Datasheet, connection, null);
                    if (tableInfo == null || tableInfo.Count == 0)
                    {
                        throw new CustomErrorCodeException(SelfErrorCode.DATASHEET_NOT_ALLOW);
                    }

                    // 構建 SQL 查詢
                    string query = BuildNestStructureQuery(connection, null, advancedSearch, tableInfo, localization);

                    // 使用 SqlCommand 並傳入查詢和連接對象
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // And設定
                        int index = 0;
                        if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                        {
                            List<string> andConditions = new List<string>();
                            foreach (var AndQueryItem in advancedSearch.And)
                            {
                                // 特殊運算元
                                if (AndQueryItem.Operate == "empty") { continue; }

                                foreach (var value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                                {
                                    // 注入參數
                                    SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object>($"@data{index++}", value));
                                }
                            }
                        }
                        // Or設定
                        if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                        {
                            List<string> andConditions = new List<string>();
                            foreach (var OrQueryItem in advancedSearch.Or)
                            {
                                // 特殊運算元
                                if (OrQueryItem.Operate == "empty") { continue; }

                                foreach (var value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                                {
                                    // 注入參數
                                    SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object>($"@data{index++}", value));
                                }
                            }
                        }

                        // 使用 SqlDataReader 來讀取查詢結果
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var sqlResult_JSONString = new StringBuilder(); // 接收sql reader回傳
                            var sqlResult_ALLJSON = new List<string>();     // 接收所有sqlResult_JSONString
                            var sqlResult_final = new List<List<Dictionary<string, object>>>();       // 接收最後結果

                            // Loop through each result set
                            // 雖然這個query只會有一個result set
                            do
                            {
                                sqlResult_JSONString.Clear();
                                var resultSet = new List<Dictionary<string, object>>();

                                // 用此方式能夠避免JSON內容太長(2033 characters limit，會被截斷成json array)
                                while (reader.Read())
                                {
                                    // 接收sql查詢結果
                                    sqlResult_JSONString.Append(reader.GetValue(0).ToString());
                                }

                                // Add this result set to the list of all results
                                if (sqlResult_JSONString.Length > 0)
                                    sqlResult_ALLJSON.Add(sqlResult_JSONString.ToString());

                            } while (reader.NextResult()); // Move to the next result set


                            // 資料庫查無資料
                            if (sqlResult_ALLJSON.Count == 0)
                            {
                                return null;
                            }
                            else
                            {
                                foreach (string sqlResult in sqlResult_ALLJSON)
                                {
                                    // Deserialize the JSON string to a List of Dictionary<string, object>
                                    var deserializedList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(sqlResult);

                                    // Remove sensitive fields
                                    foreach (var dict in deserializedList)
                                        Utilities.Utils.RemoveSensitiveData(dict);

                                    // Add the processed dictionary to the result
                                    sqlResult_final.Add(deserializedList);

                                }

                                // 因為sql的回傳只會有一筆，所以用[0]取第一筆資料就好
                                return sqlResult_final[0];
                            }

                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<Dictionary<string, object>>? GetNestedStructureData(AdvancedSearch advancedSearch, string localization, SqlConnection connection, SqlTransaction? transaction)
        {
            try
            {
                // 取得主資料表詳細資訊
                var tableInfo = _propertyRepository.GetProperty(advancedSearch.Datasheet, connection, transaction);
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    throw new CustomErrorCodeException(SelfErrorCode.DATASHEET_NOT_ALLOW);
                }

                // 構建 SQL 查詢
                string query = BuildNestStructureQuery(connection, transaction, advancedSearch, tableInfo, localization);

                // 使用 SqlCommand 並傳入查詢和連接對象
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (transaction != null)
                    {
                        command.Transaction = transaction;
                    }

                    // And設定
                    int index = 0;
                    if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                    {
                        foreach (var AndQueryItem in advancedSearch.And)
                        {
                            // 特殊運算元
                            if (AndQueryItem.Operate == "empty") { continue; }

                            foreach (string value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value.ToString()))
                            {
                                // 注入參數
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object>($"@data{index++}", value));
                            }
                        }
                    }
                    // Or設定
                    if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                    {
                        foreach (var OrQueryItem in advancedSearch.Or)
                        {
                            // 特殊運算元
                            if (OrQueryItem.Operate == "empty") { continue; }

                            foreach (string value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value.ToString()))
                            {
                                // 注入參數
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object>($"@data{index++}", value));
                            }
                        }
                    }

                    // 使用 SqlDataReader 來讀取查詢結果
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var sqlResult_JSONString = new StringBuilder(); // 接收sql reader回傳
                        var sqlResult_ALLJSON = new List<string>();     // 接收所有sqlResult_JSONString
                        var sqlResult_final = new List<List<Dictionary<string, object>>>();       // 接收最後結果

                        // Loop through each result set
                        // 雖然這個query只會有一個result set
                        do
                        {
                            sqlResult_JSONString.Clear();
                            var resultSet = new List<Dictionary<string, object>>();

                            // 用此方式能夠避免JSON內容太長(2033 characters limit，會被截斷成json array)
                            while (reader.Read())
                            {
                                // 接收sql查詢結果
                                sqlResult_JSONString.Append(reader.GetValue(0).ToString());
                            }

                            // Add this result set to the list of all results
                            if (sqlResult_JSONString.Length > 0)
                                sqlResult_ALLJSON.Add(sqlResult_JSONString.ToString());

                        } while (reader.NextResult()); // Move to the next result set


                        // 資料庫查無資料
                        if (sqlResult_ALLJSON.Count == 0)
                        {
                            return null;
                        }
                        else
                        {
                            foreach (string sqlResult in sqlResult_ALLJSON)
                            {
                                // Deserialize the JSON string to a List of Dictionary<string, object>
                                var deserializedList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(sqlResult);

                                // Remove sensitive fields
                                foreach (var dict in deserializedList)
                                    Utilities.Utils.RemoveSensitiveData(dict);

                                // Add the processed dictionary to the result
                                sqlResult_final.Add(deserializedList);

                            }

                            // 因為sql的回傳只會有一筆，所以用[0]取第一筆資料就好
                            return sqlResult_final[0];
                        }

                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<Dictionary<string, object>> GetNestedStructureData(AdvancedSearchObj advancedSearch, string localization, SqlConnection connection, SqlTransaction? transaction)
        {
            try
            {
                // 取得主資料表詳細資訊
                var tableInfo = _propertyRepository.GetProperty(advancedSearch.Datasheet, connection, transaction);
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    throw new CustomErrorCodeException(SelfErrorCode.DATASHEET_NOT_ALLOW);
                }

                // 構建 SQL 查詢
                string query = BuildNestStructureQuery(connection, transaction, advancedSearch, tableInfo, localization);


                // 使用 SqlCommand 並傳入查詢和連接對象
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (transaction != null)
                    {
                        command.Transaction = transaction;
                    }

                    // And設定
                    int index = 0;
                    if (advancedSearch.And != null && advancedSearch.And.Count > 0)
                    {
                        foreach (var AndQueryItem in advancedSearch.And)
                        {
                            // 特殊運算元
                            if (AndQueryItem.Operate == "empty") { continue; }

                            foreach (object value in Utilities.Utils.ValueCount(AndQueryItem.Operate, AndQueryItem.Value))
                            {
                                // 添加查詢參數
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object>($"@data{index++}", value));
                            }
                        }
                    }
                    // Or設定
                    if (advancedSearch.Or != null && advancedSearch.Or.Count > 0)
                    {
                        foreach (var OrQueryItem in advancedSearch.Or)
                        {
                            // 特殊運算元
                            if (OrQueryItem.Operate == "empty") { continue; }

                            foreach (object value in Utilities.Utils.ValueCount(OrQueryItem.Operate, OrQueryItem.Value))
                            {
                                // 添加查詢參數，
                                SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object>($"@data{index++}", value));
                            }
                        }
                    }

                    // 使用 SqlDataReader 來讀取查詢結果
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var sqlResult_JSONString = new StringBuilder(); // 接收sql reader回傳
                        var sqlResult_ALLJSON = new List<string>();     // 接收所有sqlResult_JSONString
                        var sqlResult_final = new List<List<Dictionary<string, object>>>();       // 接收最後結果

                        // Loop through each result set
                        // 雖然這個query只會有一個result set
                        do
                        {
                            sqlResult_JSONString.Clear();
                            var resultSet = new List<Dictionary<string, object>>();

                            // 用此方式能夠避免JSON內容太長(2033 characters limit，會被截斷成json array)
                            while (reader.Read())
                            {
                                // 接收sql查詢結果
                                sqlResult_JSONString.Append(reader.GetValue(0).ToString());
                            }

                            // Add this result set to the list of all results
                            if (sqlResult_JSONString.Length > 0)
                                sqlResult_ALLJSON.Add(sqlResult_JSONString.ToString());

                        } while (reader.NextResult()); // Move to the next result set


                        // 資料庫查無資料
                        if (sqlResult_ALLJSON.Count == 0)
                        {
                            return new List<Dictionary<string, object>>();
                        }
                        else
                        {
                            foreach (string sqlResult in sqlResult_ALLJSON)
                            {
                                // Deserialize the JSON string to a List of Dictionary<string, object>
                                var deserializedList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(sqlResult);

                                // Remove sensitive fields
                                foreach (var dict in deserializedList)
                                    Utilities.Utils.RemoveSensitiveData(dict);

                                // Add the processed dictionary to the result
                                sqlResult_final.Add(deserializedList);

                            }

                            // 因為sql的回傳只會有一筆，所以用[0]取第一筆資料就好
                            return sqlResult_final[0];
                        }

                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ResponseObject> CloneInactiveRecord(
            SqlConnection connection,
            SqlTransaction? transaction,
            long user,
            string tableName,
            Dictionary<string, object>? oldKeyValues,
            Dictionary<string, object>? newKeyValues)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, null, null,
                "通用 複製一份未生效版本的資料發生未知錯誤");

            try
            {
                // 取得主資料表詳細資訊
                var tableInfo = _propertyRepository.GetProperty(tableName, connection, transaction);
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    throw new CustomErrorCodeException(SelfErrorCode.DATASHEET_NOT_ALLOW);
                }

                #region 檢查
                // 檢查必須要有鍵
                if (oldKeyValues == null || oldKeyValues.Count == 0)
                {
                    throw new CustomErrorCodeException(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, "請輸入主鍵");
                }

                // 檢查輸入的主鍵是否存在
                HashSet<string> currentColumnName = tableInfo.Select(t => t.Name).ToHashSet();  // 取出所有欄位名稱
                foreach (var oldKeyValue in oldKeyValues)
                {
                    if (!currentColumnName.Contains(oldKeyValue.Key))
                    {
                        throw new CustomErrorCodeException(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, "輸入的主鍵不存在", oldKeyValue.Key);
                    }
                }

                // 檢查輸入的兩個主鍵字典有沒有一致
                foreach (var oldKeyValue in oldKeyValues)
                {
                    // 如果是 id 欄位，則不檢查
                    if (oldKeyValue.Key == "id") continue;

                    if (newKeyValues == null || !newKeyValues.ContainsKey(oldKeyValue.Key))
                    {
                        throw new CustomErrorCodeException(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, "輸入的主鍵不一致");
                    }
                }
                #endregion

                // 構建 SQL 查詢
                string query = Utilities.QueryBuilder.BuildCloneSqlWithOutput(
                    tableName: tableName,
                    allColumns: tableInfo.Select(t => t.Name).ToList(),
                    identityColumn: "id",
                    primaryKeys: oldKeyValues.Keys.ToList(),
                    oldKeyValues: oldKeyValues,
                    newKeyValues: newKeyValues,
                    isEdit: true
                );

                Debug.WriteLine(query);

                List<long> newIds = new();
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    // 參數注入
                    SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("@versionFlag", 2));
                    SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("@outBoundBy", user));
                    SqlHelper.AddCommandParameters(command, new KeyValuePair<string, object?>("@cloneBy", user));

                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        // 讀取插入的 ID
                        if (reader["id"] != DBNull.Value)
                        {
                            newIds.Add((long)reader["id"]);
                        }
                    }
                }

                // 包裝返回物件
                var resultObj_List = new List<Dictionary<string, object>>()
                {
                    new Dictionary<string, object>()
                    {
                        { "tableName", tableName },
                        { "newIds", newIds }
                    }
                };

                // 成功返回
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = resultObj_List;
                return responseObject;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex);
            }


        }

    }
}
