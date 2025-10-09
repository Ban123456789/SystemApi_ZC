using Accura_MES.Interfaces.Repositories;
using Accura_MES.Models;
using Accura_MES.Service;
using Accura_MES.Services;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Accura_MES.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly string _connectionString;
        private readonly Serilog.ILogger _logger;

        private PropertyRepository(string connectionString)
        {
            _connectionString = connectionString;

        }

        /// <summary>
        /// 靜態工廠方法
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static PropertyRepository CreateRepository(string connectionString)
        {
            // 返回此物件
            return new PropertyRepository(connectionString);
        }

        public List<PropertyModel> GetProperty(string tableName)
        {
            var tableInfo = new List<PropertyModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 查詢
                string query = @"
                    SELECT p.*
                    FROM [property] AS p
                    INNER JOIN [itemType] AS i ON p.itemTypeId = i.id
                    WHERE i.name = @value;
                ";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // 設定 query 參數
                    command.Parameters.Add("@value", SqlDbType.NVarChar).Value = tableName;


                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 映射到 Property 模型
                            var property = new PropertyModel
                            {
                                Id = reader.GetFieldValue<long>(reader.GetOrdinal("id")),
                                Name = reader["name"] as string,
                                Label = reader["label"] as string,
                                DataType = reader["dataType"] as string,
                                DataSource = reader["dataSource"] as string,
                                StoredLength = reader["storedLength"] as int?,
                                Scale = reader["scale"] as int?,
                                IsDelete = reader["isDelete"] != DBNull.Value && (bool)reader["isDelete"],
                                IsOnly = reader["isOnly"] != DBNull.Value && (bool)reader["isOnly"],
                                IsRequired = reader["isRequired"] != DBNull.Value && (bool)reader["isRequired"],
                                ColumnWidth = reader["columnWidth"] as int?,
                                ColumnIndex = reader["columnIndex"] as int?,
                                RowIndex = reader["rowIndex"] as int?,
                                SortIndex = int.Parse(reader["sortIndex"].ToString()),
                                DefaultValue = reader["defaultValue"] as string,
                                ItemTypeId = reader.GetFieldValue<long>(reader.GetOrdinal("itemTypeId")),
                                PropertyType = reader["propertyType"] as string,
                                CreatedBy = reader.GetFieldValue<long>(reader.GetOrdinal("createdBy")),
                                CreatedOn = reader.GetFieldValue<DateTime>(reader.GetOrdinal("createdOn")),
                                ModifiedBy = reader.GetFieldValue<long>(reader.GetOrdinal("modifiedBy")),
                                ModifiedOn = reader.GetFieldValue<DateTime>(reader.GetOrdinal("modifiedOn"))
                            };

                            tableInfo.Add(property);
                        }
                    }
                }
            }

            return tableInfo;
        }

        public List<PropertyModel> GetProperty(string tableName, SqlConnection connection, SqlTransaction? transaction)
        {
            var tableInfo = new List<PropertyModel>();

            // 查詢
            string query = @"
                SELECT p.*
                FROM [property] AS p
                INNER JOIN [itemType] AS i ON p.itemTypeId = i.id
                WHERE i.name = @value AND i.isDelete = 0 AND p.isDelete = 0;
            ";

            using (SqlCommand command = new SqlCommand())
            {
                // 設定連線
                command.Connection = connection;
                if (transaction != null)
                {
                    // 設定交易
                    command.Transaction = transaction;
                }

                // 查詢 property
                command.CommandText = query;
                command.Parameters.Add("@value", SqlDbType.NVarChar).Value = tableName;


                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 映射到 Property 模型
                        var property = new PropertyModel
                        {
                            Id = reader.GetFieldValue<long>(reader.GetOrdinal("id")),
                            Name = reader["name"] as string,
                            Label = reader["label"] as string,
                            DataType = reader["dataType"] as string,
                            DataSource = reader["dataSource"] as string,
                            StoredLength = reader["storedLength"] as int?,
                            Scale = reader["scale"] as int?,
                            IsDelete = reader["isDelete"] != DBNull.Value && (bool)reader["isDelete"],
                            IsOnly = reader["isOnly"] != DBNull.Value && (bool)reader["isOnly"],
                            IsRequired = reader["isRequired"] != DBNull.Value && (bool)reader["isRequired"],
                            ColumnWidth = reader["columnWidth"] as int?,
                            ColumnIndex = reader["columnIndex"] as int?,
                            RowIndex = reader["rowIndex"] as int?,
                            SortIndex = int.Parse(reader["sortIndex"].ToString()),
                            DefaultValue = reader["defaultValue"] as string,
                            ItemTypeId = reader.GetFieldValue<long>(reader.GetOrdinal("itemTypeId")),
                            PropertyType = reader["propertyType"] as string,
                            CreatedBy = reader.GetFieldValue<long>(reader.GetOrdinal("createdBy")),
                            CreatedOn = reader.GetFieldValue<DateTime>(reader.GetOrdinal("createdOn")),
                            ModifiedBy = reader.GetFieldValue<long>(reader.GetOrdinal("modifiedBy")),
                            ModifiedOn = reader.GetFieldValue<DateTime>(reader.GetOrdinal("modifiedOn"))
                        };

                        tableInfo.Add(property);
                    }
                }
            }


            return tableInfo;
        }


        public List<string> GetDataSource(string menuListName)
        {
            List<string> souceTableInfo = new List<string>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // 查詢
                string query = @"
                    SELECT " +
                    "droplist.value " +
                    "FROM [droplist] " +
                    "    INNER JOIN [menulist] ON droplist.menulistId = menulist.id \n" +
                    "    WHERE [menulist].name = @Value";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@Value", menuListName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // rows
                        while (reader.Read())
                        {
                            string sqlresult = string.Empty;

                            // columns
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                sqlresult = reader.GetValue(i).ToString();

                            }

                            souceTableInfo.Add(sqlresult);
                        }
                    }
                }
            }

            return souceTableInfo.Count > 0 ? souceTableInfo : null;
        }


        public async Task<List<PropertyInputValidItem>> GetPropertiesAsync(string tableName)
        {
            return await SqlHelper.ExecuteTransactionAsync(_connectionString, async (transaction, command) =>
            {
                string query = @"
                    SELECT 
                        p.[name],
                        p.[isRequired],
                        p.[defaultValue]
                    FROM [property] AS p
                    LEFT JOIN [itemtype] AS it ON it.id = p.itemTypeId
                    WHERE it.name = @Value_tableName";

                // 設定查詢和參數
                command.CommandText = query;
                command.Parameters.AddWithValue("@Value_tableName", tableName);

                var result = new List<PropertyInputValidItem>();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PropertyInputValidItem
                        {
                            Name = reader["name"].ToString(),
                            IsRequired = reader["isRequired"] != DBNull.Value && Convert.ToBoolean(reader["isRequired"]),
                            DefaultValue = reader["defaultValue"]?.ToString()
                        });
                    }
                }

                return result;
            });
        }


        public Dictionary<string, List<Dictionary<string, object>>> GetDataBaseSystemInfo()
        {
            var systemInfo = new Dictionary<string, List<Dictionary<string, object>>>();
            string query = @"
                SELECT 
                    c.TABLE_NAME, 
                    c.COLUMN_NAME, 
                    c.DATA_TYPE, 
                    c.CHARACTER_MAXIMUM_LENGTH, 
                    c.IS_NULLABLE, 
                    c.NUMERIC_PRECISION, 
                    c.NUMERIC_SCALE, 
                    ep.value AS Description,
                    fk_table.name AS Referenced_Table_Name, -- 關聯資料表名稱
                    fk_column.name AS Referenced_Column_Name, -- 關聯欄位名稱
                    uk.CONSTRAINT_NAME AS Unique_Constraint_Name -- 唯一鍵名稱
                FROM 
                    INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN 
                    sys.extended_properties ep 
                    ON ep.major_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) 
                    AND ep.minor_id = COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'ColumnId')
                    AND ep.name = 'MS_Description' -- 只選取描述類型的屬性
                LEFT JOIN 
                    sys.foreign_key_columns fk 
                    ON fk.parent_object_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) 
                    AND fk.parent_column_id = COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'ColumnId')
                LEFT JOIN 
                    sys.tables fk_table 
                    ON fk.referenced_object_id = fk_table.object_id
                LEFT JOIN 
                    sys.columns fk_column 
                    ON fk.referenced_object_id = fk_column.object_id 
                    AND fk.referenced_column_id = fk_column.column_id
                LEFT JOIN 
                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE uk 
                    ON c.TABLE_NAME = uk.TABLE_NAME 
                    AND c.COLUMN_NAME = uk.COLUMN_NAME 
                    AND uk.CONSTRAINT_NAME IN (
                        SELECT CONSTRAINT_NAME 
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                        WHERE CONSTRAINT_TYPE = 'UNIQUE'
                    )
                WHERE 
                    c.TABLE_CATALOG = DB_NAME() 
                    AND c.TABLE_NAME != 'sysdiagrams'
                ORDER BY 
                    c.TABLE_NAME, c.ORDINAL_POSITION;
            ";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader["TABLE_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();

                            if (!systemInfo.ContainsKey(tableName))
                            {
                                systemInfo[tableName] = new List<Dictionary<string, object>>();
                            }

                            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
                            keyValuePairs["COLUMN_NAME"] = reader["COLUMN_NAME"];
                            keyValuePairs["DATA_TYPE"] = reader["DATA_TYPE"];
                            keyValuePairs["IS_NULLABLE"] = reader["IS_NULLABLE"];
                            keyValuePairs["Unique_Constraint_Name"] = reader["Unique_Constraint_Name"];
                            keyValuePairs["CHARACTER_MAXIMUM_LENGTH"] = reader["CHARACTER_MAXIMUM_LENGTH"];
                            //reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")) ? "" : reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                            keyValuePairs["NUMERIC_PRECISION"] = reader["NUMERIC_PRECISION"];
                            keyValuePairs["NUMERIC_SCALE"] = reader["NUMERIC_SCALE"];
                            keyValuePairs["DESCRIPTION"] = reader["DESCRIPTION"];
                            keyValuePairs["Referenced_Table_Name"] = reader["Referenced_Table_Name"];
                            keyValuePairs["Referenced_Column_Name"] = reader["Referenced_Column_Name"];

                            systemInfo[tableName].Add(keyValuePairs);
                        }
                    }
                }
            }

            return systemInfo;
        }

        public long GetItemTypeId(string tableName)
        {
            long result = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT id FROM [dbo].[itemtype] WHERE name = @TableName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // 設定query參數
                    command.Parameters.AddWithValue("@TableName", tableName);

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = reader.GetInt64(0);
                        }
                        else
                        {
                            throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "No item found in [dbo].[itemtype] for table '{tableName}'");
                        }
                    }
                }
            }

            return result;
        }

        public async Task<bool> InitializeItemType(Dictionary<string, List<Dictionary<string, object>>> systemInfo, long user)
        {
            string sql = @"
                        INSERT INTO dbo.itemType (
                            name,
                            isDelete,
                            createdBy,
                            modifiedBy
                        ) 
                        VALUES (
                            @Value1,
                            @Value2,
                            @Value3,
                            @Value4
                        );";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // 打開連接
                await connection.OpenAsync();

                // 開始交易
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Transaction = transaction;  // 將 transaction 指定給 command

                            // 所有table name
                            foreach (var tableInfo in systemInfo)
                            {
                                string tableName = tableInfo.Key;

                                // 每次執行前清空參數
                                command.Parameters.Clear();

                                // 添加參數，避免 SQL 注入
                                command.Parameters.AddWithValue("@Value1", tableName);
                                command.Parameters.AddWithValue("@Value2", 0);
                                command.Parameters.AddWithValue("@Value3", user);
                                command.Parameters.AddWithValue("@Value4", user);

                                // 執行
                                await command.ExecuteNonQueryAsync();

                            }
                        }

                        // 提交交易
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return true;
        }

        public async Task<Dictionary<string, long>> InitializeItemType(SqlConnection connection, SqlTransaction transaction,
            Dictionary<string, List<Dictionary<string, object>>> systemInfo, long user)
        {

            var kvpOfNameAndId = new Dictionary<string, long>();

            string sql = @"
                        INSERT INTO dbo.itemType (
                            name,
                            isDelete,
                            createdBy,
                            modifiedBy
                        )  OUTPUT INSERTED.ID
                        VALUES (
                            @Value1,
                            @Value2,
                            @Value3,
                            @Value4
                        );";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Transaction = transaction;  // 將 transaction 指定給 command

                // 所有table name
                foreach (var tableInfo in systemInfo)
                {
                    string tableName = tableInfo.Key;

                    // 每次執行前清空參數
                    command.Parameters.Clear();

                    // 添加參數，避免 SQL 注入
                    command.Parameters.AddWithValue("@Value1", tableName);
                    command.Parameters.AddWithValue("@Value2", 0);
                    command.Parameters.AddWithValue("@Value3", user);
                    command.Parameters.AddWithValue("@Value4", user);

                    // 執行
                    var insertedId = await command.ExecuteScalarAsync();

                    kvpOfNameAndId.Add(tableName, (long)insertedId);
                }
            }

            return kvpOfNameAndId;
        }

        public async Task<bool> InitializeProperty(Dictionary<string, List<Dictionary<string, object>>> systemInfo)
        {


            string sql = @"
                        INSERT INTO dbo.property (
                                name,
                                label,
                                dataType,
                                dataSource,
                                storedLength,
                                scale,
                                isDelete,
                                isOnly,
                                isRequired,
	                            sortIndex,
	                            itemTypeId,
	                            propertyType,
	                            createdBy,
	                            modifiedBy
                        ) 
                        VALUES (
                            @Value_name,
                            @Value_label,
                            @Value_dataType,
                            @Value_dataSource,
                            @Value_storedLength,
                            @Value_scale,
                            @Value_isDelete,
                            @Value_isOnly,
                            @Value_isRequired,
                            @Value_sortIndex,
                            @Value_itemTypeId,
                            @Value_propertyType,
                            @Value_createdBy,
                            @Value_modifiedBy
                        );";


            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // 打開連接
                await connection.OpenAsync();

                // 開始交易
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);
                        // get system id
                        long sysId = await userRepository.GetSystemId(connection, transaction);

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Transaction = transaction;  // 將 transaction 指定給 command

                            int sortIndex = 0; // 每個table的每列資料的index

                            // 所有table
                            foreach (var tableInfo in systemInfo)
                            {
                                sortIndex = 0;
                                string tableName = tableInfo.Key;
                                //Debug.WriteLine($"================={tableName}============================");

                                // table 所有 column
                                for (int i = 0; i < tableInfo.Value.Count; i++)
                                {
                                    // column info
                                    Dictionary<string, object> colInfo = tableInfo.Value[i];
                                    sortIndex = sortIndex + 10;

                                    // 每次執行前清空參數
                                    command.Parameters.Clear();

                                    //Debug.WriteLine(colInfo["COLUMN_NAME"].ToString());
                                    Debug.WriteLine(tableName + $" -- colname: {colInfo["COLUMN_NAME"].ToString()}");
                                    var (propertyType, label) = ParseDescription(colInfo["DESCRIPTION"].ToString());
                                    //Debug.WriteLine($"{propertyType}, {label}");

                                    // 添加參數，避免 SQL 注入
                                    command.Parameters.AddWithValue("@Value_name", colInfo["COLUMN_NAME"]);
                                    command.Parameters.AddWithValue("@Value_label", label);
                                    command.Parameters.AddWithValue("@Value_dataType", "system");
                                    command.Parameters.AddWithValue("@Value_dataSource", colInfo["Referenced_Table_Name"]);
                                    command.Parameters.AddWithValue("@Value_storedLength", colInfo["CHARACTER_MAXIMUM_LENGTH"]);
                                    command.Parameters.AddWithValue("@Value_scale", colInfo["NUMERIC_SCALE"]);
                                    command.Parameters.AddWithValue("@Value_isDelete", "0");
                                    command.Parameters.AddWithValue("@Value_isOnly", string.IsNullOrEmpty(colInfo["Unique_Constraint_Name"].ToString()) ? "0" : "1");   // column must unique: isOnly = 1; otherwise: isOnly = 0
                                    command.Parameters.AddWithValue("@Value_isRequired", colInfo["IS_NULLABLE"].ToString().ToUpper() == "NO" ? "1" : "0");              // column not nullable: isRequired = 1; otherwise:isrequired = 0
                                    command.Parameters.AddWithValue("@Value_sortIndex", sortIndex);
                                    command.Parameters.AddWithValue("@Value_itemTypeId", GetItemTypeId(tableName));
                                    command.Parameters.AddWithValue("@Value_propertyType", propertyType);
                                    command.Parameters.AddWithValue("@Value_createdBy", sysId);
                                    command.Parameters.AddWithValue("@Value_modifiedBy", sysId);

                                    // 執行
                                    await command.ExecuteNonQueryAsync();

                                }
                            }
                        }

                        // 提交交易
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }

                }
            }

            return true;
        }

        public async Task<bool> InitializeProperty(SqlConnection connection, SqlTransaction transaction,
            Dictionary<string, List<Dictionary<string, object>>> systemInfo, Dictionary<string, long> itemtypeInfo, long user)
        {
            string sql = @"
                        INSERT INTO dbo.property (
                                name,
                                label,
                                dataType,
                                dataSource,
                                storedLength,
                                scale,
                                isDelete,
                                isOnly,
                                isRequired,
	                            sortIndex,
	                            itemTypeId,
	                            propertyType,
	                            createdBy,
	                            modifiedBy
                        ) 
                        VALUES (
                            @Value_name,
                            @Value_label,
                            @Value_dataType,
                            @Value_dataSource,
                            @Value_storedLength,
                            @Value_scale,
                            @Value_isDelete,
                            @Value_isOnly,
                            @Value_isRequired,
                            @Value_sortIndex,
                            @Value_itemTypeId,
                            @Value_propertyType,
                            @Value_createdBy,
                            @Value_modifiedBy
                        );";


            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Transaction = transaction;  // 將 transaction 指定給 command

                int sortIndex = 0; // 每個table的每列資料的index

                // 所有table
                foreach (var tableInfo in systemInfo)
                {
                    sortIndex = 0;
                    string tableName = tableInfo.Key;
                    //Debug.WriteLine($"================={tableName}============================");

                    // table 所有 column
                    for (int i = 0; i < tableInfo.Value.Count; i++)
                    {
                        // column info
                        Dictionary<string, object> colInfo = tableInfo.Value[i];
                        sortIndex = sortIndex + 10;

                        // 每次執行前清空參數
                        command.Parameters.Clear();

                        //Debug.WriteLine(colInfo["COLUMN_NAME"].ToString());
                        Debug.WriteLine(tableName + $" -- colname: {colInfo["COLUMN_NAME"].ToString()}");
                        var (propertyType, label) = ParseDescription(colInfo["DESCRIPTION"].ToString());
                        //Debug.WriteLine($"{propertyType}, {label}");

                        // 添加參數，避免 SQL 注入
                        command.Parameters.AddWithValue("@Value_name", colInfo["COLUMN_NAME"]);
                        command.Parameters.AddWithValue("@Value_label", label);
                        command.Parameters.AddWithValue("@Value_dataType", "system");
                        command.Parameters.AddWithValue("@Value_dataSource", colInfo["Referenced_Table_Name"]);
                        command.Parameters.AddWithValue("@Value_storedLength", colInfo["CHARACTER_MAXIMUM_LENGTH"]);
                        command.Parameters.AddWithValue("@Value_scale", colInfo["NUMERIC_SCALE"]);
                        command.Parameters.AddWithValue("@Value_isDelete", "0");
                        command.Parameters.AddWithValue("@Value_isOnly", string.IsNullOrEmpty(colInfo["Unique_Constraint_Name"].ToString()) ? "0" : "1");   // column must unique: isOnly = 1; otherwise: isOnly = 0
                        command.Parameters.AddWithValue("@Value_isRequired", colInfo["IS_NULLABLE"].ToString().ToUpper() == "NO" ? "1" : "0");              // column not nullable: isRequired = 1; otherwise:isrequired = 0
                        command.Parameters.AddWithValue("@Value_sortIndex", sortIndex);
                        command.Parameters.AddWithValue("@Value_itemTypeId", itemtypeInfo[tableName]);
                        command.Parameters.AddWithValue("@Value_propertyType", propertyType);
                        command.Parameters.AddWithValue("@Value_createdBy", user);
                        command.Parameters.AddWithValue("@Value_modifiedBy", user);

                        // 執行
                        await command.ExecuteNonQueryAsync();

                    }
                }
            }

            return true;
        }

        public async Task<bool> InitializeMenuListAndDropList(long user)
        {
            try
            {
                // 獲取指定json檔案路徑
                DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
                string filePath = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "MenuListAndDropList.json");

                // 讀取json
                var mappingItems = FileUtils.ReadJsonFile<List<Mapping_MenuListAndDropList>>(filePath);

                // UPSERT兩張資料表
                return
                await SqlHelper.ExecuteTransactionAsync(_connectionString, async (transaction, command) =>
                {
                    // 用來收集所有保留的 menulistId 和 droplist value
                    List<int> allMenuListIds = new List<int>();
                    HashSet<string> allDropListValues = new HashSet<string>();

                    foreach (var mappingItem in mappingItems)
                    {
                        StringBuilder queryBuilder = new("DECLARE @OutputTable TABLE (MenuListId INT);\n");
                        queryBuilder.Append("DECLARE @MenuListId INT;\n");

                        // 如果menulist已經存在同樣的name，則進行更新，否則插入新紀錄
                        queryBuilder.AppendLine(@"
                            MERGE INTO dbo.menulist AS target
                            USING (SELECT @Value1 AS name, @Value2 AS label, @Value3 AS description, 
                                          @Value4 AS type, @Value5 AS isItemlist, @Value6 AS isDelete,
                                          @Value7 AS createdBy,
                                          @Value8 AS modifiedBy) AS source
                            ON target.name = source.name 
                            WHEN MATCHED THEN
                                UPDATE SET
                                    target.label = source.label,
                                    target.description = source.description,
                                    target.type = source.type,
                                    target.isItemlist = source.isItemlist,
                                    target.isDelete = source.isDelete,
                                    target.createdBy = source.createdBy,
                                    target.modifiedBy = source.modifiedBy
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (name, label, description, type, isItemlist, isDelete, createdBy, modifiedBy)
                                VALUES (source.name, source.label, source.description, source.type, source.isItemlist, source.isDelete,
                                        source.createdBy, source.modifiedBy)
                            OUTPUT inserted.id INTO @OutputTable;
                            "
                        );

                        // 獲取插入或更新後的 menulistId
                        queryBuilder.AppendLine("SELECT @MenuListId = MenuListId FROM @OutputTable;");

                        if (mappingItem.DropList.Count != 0)
                        {
                            // droplist 臨時表格
                            queryBuilder.AppendLine(@"DECLARE @DropListItems TABLE (
                                label NVARCHAR(1000),
                                value NVARCHAR(50),
                                description NVARCHAR(200),
                                menulistId BIGINT,
                                type NVARCHAR(50),
                                isDelete BIT,
                                createdBy BIGINT,
                                modifiedBy BIGINT
                                );"
                            );

                            queryBuilder.AppendLine("INSERT INTO @DropListItems (label, value, description, menulistId, type, isDelete, createdBy, modifiedBy)");
                            queryBuilder.AppendLine("VALUES");

                            // 插入 0~多筆資料到臨時表格
                            for (int i = 0; i < mappingItem.DropList.Count; i++)
                            {
                                queryBuilder.Append($"(@Value{i}_1, @Value{i}_2, @Value{i}_3, @MenuListId, @Value{i}_4, @Value{i}_5, @Value{i}_6, @Value{i}_7)");

                                // 如果還沒到最後一筆就在結尾加上","
                                if (i < mappingItem.DropList.Count - 1)
                                {
                                    queryBuilder.AppendLine(",");
                                }
                            }

                            // 使用 menulistId 進行 droplist 插入或更新
                            queryBuilder.Append(@"
                                MERGE INTO dbo.droplist AS target
                                USING @DropListItems AS source
                                ON target.menulistId = source.menulistId AND target.value = source.value
                                WHEN MATCHED THEN
                                    UPDATE SET
                                        target.label = source.label,
                                        target.description = source.description,
                                        target.type = source.type,
                                        target.isDelete = source.isDelete,
                                        target.createdBy = source.createdBy,
                                        target.modifiedBy = source.modifiedBy
                                WHEN NOT MATCHED BY TARGET THEN
                                    INSERT (label, value, description, menulistId, type, isDelete, createdBy, modifiedBy)
                                    VALUES (source.label, source.value, source.description, source.menulistId, source.type, 
                                            source.isDelete, source.createdBy, source.modifiedBy);
                                ");
                        } // end of if exist droplist

                        // 返回當前的 MenuListId
                        queryBuilder.AppendLine(@"
                            SELECT @MenuListId
                            ");

                        // 設定command的查詢語句
                        command.CommandText = queryBuilder.ToString();

                        // 清除上回loop的參數
                        command.Parameters.Clear();

                        // 自訂序列化選項，避免中文轉成 ASCII
                        var options = new JsonSerializerOptions
                        {
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 允許非 ASCII 字符
                            WriteIndented = true // 格式化輸出
                        };

                        // 設置 menulist 的參數
                        command.Parameters.AddWithValue("@Value1", mappingItem.Name);
                        command.Parameters.AddWithValue("@Value2", JsonSerializer.Serialize(mappingItem.I18n, options)); // 將物件轉換為 JSON string
                        command.Parameters.AddWithValue("@Value3", "");
                        command.Parameters.AddWithValue("@Value4", mappingItem.Type);
                        command.Parameters.AddWithValue("@Value5", mappingItem.IsItemList);
                        command.Parameters.AddWithValue("@Value6", mappingItem.IsDelete);
                        command.Parameters.AddWithValue("@Value7", user);
                        command.Parameters.AddWithValue("@Value8", user);

                        // 設置 droplist 的參數
                        for (int i = 0; i < mappingItem.DropList.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@Value{i}_1", JsonSerializer.Serialize(mappingItem.DropList[i].I18n, options));
                            command.Parameters.AddWithValue($"@Value{i}_2", mappingItem.DropList[i].Value);
                            command.Parameters.AddWithValue($"@Value{i}_3", "");
                            command.Parameters.AddWithValue($"@Value{i}_4", mappingItem.DropList[i].Type);
                            command.Parameters.AddWithValue($"@Value{i}_5", mappingItem.DropList[i].IsDelete);
                            command.Parameters.AddWithValue($"@Value{i}_6", user);
                            command.Parameters.AddWithValue($"@Value{i}_7", user);
                        }

                        var sqlResult = await command.ExecuteScalarAsync();
                        Debug.Write($"MenuList name: {mappingItem.Name}, ");
                        Debug.WriteLine("MenuList id: " + sqlResult);
                        if (sqlResult != DBNull.Value)  // 檢查是否為 DBNull
                        {
                            allMenuListIds.Add(Convert.ToInt32(sqlResult));
                        }

                        // 收集 droplist 的 value
                        foreach (var dropItem in mappingItem.DropList)
                        {
                            allDropListValues.Add(dropItem.Value);
                        }
                    } // end of foreach()

                    // 刪除多餘的 droplist 資料並回傳 ID
                    StringBuilder deleteDropListQuery = new StringBuilder();
                    deleteDropListQuery.Append(@"
                        DECLARE @DeletedDropList TABLE (Id BIGINT);

                        DELETE FROM dbo.droplist
                        OUTPUT DELETED.Id INTO @DeletedDropList
                        WHERE menulistId NOT IN (" + string.Join(",", allMenuListIds) + @")
                        OR value NOT IN (" + string.Join(",", allDropListValues.Select(v => $"'{v}'")) + @");

                        SELECT Id FROM @DeletedDropList;
                    ");

                    command.CommandText = deleteDropListQuery.ToString();
                    var deletedDropListIdsList = new List<long>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.IsDBNull(0)) continue; // Skip if value is DBNull

                            deletedDropListIdsList.Add(reader.GetInt64(0));
                        }

                        Debug.WriteLine("==================deletedDropListIdsList==================");
                        Debug.WriteLine(string.Join(", ", deletedDropListIdsList));
                    }
                    // 刪除多餘的 menulist 資料並回傳 ID
                    StringBuilder deleteMenuListQuery = new StringBuilder();
                    deleteMenuListQuery.Append(@"
                        DECLARE @DeletedMenuList TABLE (Id INT);

                        DELETE FROM dbo.menulist
                        OUTPUT DELETED.Id INTO @DeletedMenuList
                        WHERE id NOT IN (" + string.Join(",", allMenuListIds) + @");

                        SELECT Id FROM @DeletedMenuList;
                    ");

                    command.CommandText = deleteMenuListQuery.ToString();
                    var deletedMenuListIdsList = new List<int>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.IsDBNull(0)) continue; // Skip if value is DBNull

                            deletedMenuListIdsList.Add(reader.GetInt32(0));
                        }

                        Debug.WriteLine("==================deletedMenuListIdsList==================");
                        Debug.WriteLine(string.Join(", ", deletedMenuListIdsList));
                    }

                    return true;
                });
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> InitializeMenuListAndDropList(SqlConnection connection, SqlTransaction transaction,
            List<Mapping_MenuListAndDropList> mappingItems, long user)
        {
            try
            {
                // UPSERT兩張資料表
                var command = new SqlCommand()
                {
                    Connection = connection,
                    Transaction = transaction,
                };

                // 用來收集所有保留的 menulistId 和 droplist value
                List<int> allMenuListIds = new List<int>();
                HashSet<string> allDropListValues = new HashSet<string>();


                // 遍歷所有資料來源
                foreach (var mappingItem in mappingItems)
                {
                    StringBuilder queryBuilder = new("DECLARE @OutputTable TABLE (MenuListId INT);\n");
                    queryBuilder.Append("DECLARE @MenuListId INT;\n");

                    // 如果menulist已經存在同樣的name，則進行更新，否則插入新紀錄
                    queryBuilder.AppendLine(@"
                        MERGE INTO dbo.menulist AS target
                        USING (SELECT @Value1 AS name, @Value2 AS label, @Value3 AS description, 
                                        @Value4 AS type, @Value5 AS isItemlist, @Value6 AS isDelete,
                                        @Value7 AS createdBy,
                                        @Value8 AS modifiedBy) AS source
                        ON target.name = source.name 
                        WHEN MATCHED THEN
                            UPDATE SET
                                target.label = source.label,
                                target.description = source.description,
                                target.type = source.type,
                                target.isItemlist = source.isItemlist,
                                target.isDelete = source.isDelete,
                                target.createdBy = source.createdBy,
                                target.modifiedBy = source.modifiedBy
                        WHEN NOT MATCHED BY TARGET THEN
                            INSERT (name, label, description, type, isItemlist, isDelete, createdBy, modifiedBy)
                            VALUES (source.name, source.label, source.description, source.type, source.isItemlist, source.isDelete,
                                    source.createdBy, source.modifiedBy)
                        OUTPUT inserted.id INTO @OutputTable;
                        "
                    );

                    // 獲取插入或更新後的 menulistId
                    queryBuilder.AppendLine("SELECT @MenuListId = MenuListId FROM @OutputTable;");

                    if (mappingItem.DropList.Count != 0)
                    {
                        // droplist 臨時表格
                        queryBuilder.AppendLine(@"DECLARE @DropListItems TABLE (
                            label NVARCHAR(1000),
                            value NVARCHAR(50),
                            description NVARCHAR(200),
                            menulistId BIGINT,
                            type NVARCHAR(50),
                            isDelete BIT,
                            createdBy BIGINT,
                            modifiedBy BIGINT
                            );"
                        );

                        queryBuilder.AppendLine("INSERT INTO @DropListItems (label, value, description, menulistId, type, isDelete, createdBy, modifiedBy)");
                        queryBuilder.AppendLine("VALUES");

                        // 插入 0~多筆資料到臨時表格
                        for (int i = 0; i < mappingItem.DropList.Count; i++)
                        {
                            queryBuilder.Append($"(@Value{i}_1, @Value{i}_2, @Value{i}_3, @MenuListId, @Value{i}_4, @Value{i}_5, @Value{i}_6, @Value{i}_7)");

                            // 如果還沒到最後一筆就在結尾加上","
                            if (i < mappingItem.DropList.Count - 1)
                            {
                                queryBuilder.AppendLine(",");
                            }
                        }

                        // 使用 menulistId 進行 droplist 插入或更新
                        queryBuilder.Append(@"
                            MERGE INTO dbo.droplist AS target
                            USING @DropListItems AS source
                            ON target.menulistId = source.menulistId AND target.value = source.value
                            WHEN MATCHED THEN
                                UPDATE SET
                                    target.label = source.label,
                                    target.description = source.description,
                                    target.type = source.type,
                                    target.isDelete = source.isDelete,
                                    target.createdBy = source.createdBy,
                                    target.modifiedBy = source.modifiedBy
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (label, value, description, menulistId, type, isDelete, createdBy, modifiedBy)
                                VALUES (source.label, source.value, source.description, source.menulistId, source.type, 
                                        source.isDelete, source.createdBy, source.modifiedBy);
                            ");
                    } // end of if exist droplist

                    // 返回當前的 MenuListId
                    queryBuilder.AppendLine(@"
                        SELECT @MenuListId
                        ");

                    // 設定command的查詢語句
                    command.CommandText = queryBuilder.ToString();

                    // 清除上回loop的參數
                    command.Parameters.Clear();

                    // 自訂序列化選項，避免中文轉成 ASCII
                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 允許非 ASCII 字符
                        WriteIndented = true // 格式化輸出
                    };

                    // 設置 menulist 的參數
                    command.Parameters.AddWithValue("@Value1", mappingItem.Name);
                    command.Parameters.AddWithValue("@Value2", JsonSerializer.Serialize(mappingItem.I18n, options)); // 將物件轉換為 JSON string
                    command.Parameters.AddWithValue("@Value3", "");
                    command.Parameters.AddWithValue("@Value4", mappingItem.Type);
                    command.Parameters.AddWithValue("@Value5", mappingItem.IsItemList);
                    command.Parameters.AddWithValue("@Value6", mappingItem.IsDelete);
                    command.Parameters.AddWithValue("@Value7", user);
                    command.Parameters.AddWithValue("@Value8", user);

                    // 設置 droplist 的參數
                    for (int i = 0; i < mappingItem.DropList.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@Value{i}_1", JsonSerializer.Serialize(mappingItem.DropList[i].I18n, options));
                        command.Parameters.AddWithValue($"@Value{i}_2", mappingItem.DropList[i].Value);
                        command.Parameters.AddWithValue($"@Value{i}_3", "");
                        command.Parameters.AddWithValue($"@Value{i}_4", mappingItem.DropList[i].Type);
                        command.Parameters.AddWithValue($"@Value{i}_5", mappingItem.DropList[i].IsDelete);
                        command.Parameters.AddWithValue($"@Value{i}_6", user);
                        command.Parameters.AddWithValue($"@Value{i}_7", user);
                    }

                    var sqlResult = await command.ExecuteScalarAsync();
                    Debug.Write($"MenuList name: {mappingItem.Name}, ");
                    Debug.WriteLine("MenuList id: " + sqlResult);
                    if (sqlResult != DBNull.Value)  // 檢查是否為 DBNull
                    {
                        allMenuListIds.Add(Convert.ToInt32(sqlResult));
                    }

                    // 收集 droplist 的 value
                    foreach (var dropItem in mappingItem.DropList)
                    {
                        allDropListValues.Add(dropItem.Value);
                    }
                } // end of foreach()

                // 刪除多餘的 droplist 資料並回傳 ID
                StringBuilder deleteDropListQuery = new StringBuilder();
                deleteDropListQuery.Append(@"
                    DECLARE @DeletedDropList TABLE (Id BIGINT);

                    DELETE FROM dbo.droplist
                    OUTPUT DELETED.Id INTO @DeletedDropList
                    WHERE menulistId NOT IN (" + string.Join(",", allMenuListIds) + @")
                    OR value NOT IN (" + string.Join(",", allDropListValues.Select(v => $"'{v}'")) + @");

                    SELECT Id FROM @DeletedDropList;
                ");

                command.CommandText = deleteDropListQuery.ToString();
                var deletedDropListIdsList = new List<long>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.IsDBNull(0)) continue; // Skip if value is DBNull

                        deletedDropListIdsList.Add(reader.GetInt64(0));
                    }

                    Debug.WriteLine("==================deletedDropListIdsList==================");
                    Debug.WriteLine(string.Join(", ", deletedDropListIdsList));
                }
                // 刪除多餘的 menulist 資料並回傳 ID
                StringBuilder deleteMenuListQuery = new StringBuilder();
                deleteMenuListQuery.Append(@"
                    DECLARE @DeletedMenuList TABLE (Id INT);

                    DELETE FROM dbo.menulist
                    OUTPUT DELETED.Id INTO @DeletedMenuList
                    WHERE id NOT IN (" + string.Join(",", allMenuListIds) + @");

                    SELECT Id FROM @DeletedMenuList;
                ");

                command.CommandText = deleteMenuListQuery.ToString();
                var deletedMenuListIdsList = new List<int>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.IsDBNull(0)) continue; // Skip if value is DBNull

                        deletedMenuListIdsList.Add(reader.GetInt32(0));
                    }

                    Debug.WriteLine("==================deletedMenuListIdsList==================");
                    Debug.WriteLine(string.Join(", ", deletedMenuListIdsList));
                }

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> InitializeMenuListAndDropList_useTempTable(SqlConnection connection, SqlTransaction transaction,
            List<Dictionary<string, object>> dropListSystemInfo,
            List<Mapping_MenuListAndDropList> mappingItems, long user)
        {
            try
            {
                // UPSERT兩張資料表
                var command = new SqlCommand()
                {
                    Connection = connection,
                    Transaction = transaction,
                };

                // 用來收集所有保留的 menulistId 和 droplist value
                List<int> allMenuListIds = new List<int>();
                HashSet<string> allDropListValues = new HashSet<string>();


                // 遍歷所有資料來源
                foreach (var mappingItem in mappingItems)
                {
                    StringBuilder queryBuilder = new("DECLARE @OutputTable TABLE (MenuListId INT);\n");
                    queryBuilder.Append("DECLARE @MenuListId INT;\n");

                    // 如果 menulist 已經存在同樣的 name，則進行更新，否則插入新紀錄
                    queryBuilder.AppendLine(@"
                        MERGE INTO dbo.menulist AS target
                        USING (SELECT @Value1 AS name, @Value2 AS label, @Value3 AS description, 
                                        @Value4 AS type, @Value5 AS isItemlist, @Value6 AS isDelete,
                                        @Value7 AS createdBy,
                                        @Value8 AS modifiedBy) AS source
                        ON target.name = source.name 
                        WHEN MATCHED THEN
                            UPDATE SET
                                target.label = source.label,
                                target.description = source.description,
                                target.type = source.type,
                                target.isItemlist = source.isItemlist,
                                target.isDelete = source.isDelete,
                                target.createdBy = source.createdBy,
                                target.modifiedBy = source.modifiedBy
                        WHEN NOT MATCHED BY TARGET THEN
                            INSERT (name, label, description, type, isItemlist, isDelete, createdBy, modifiedBy)
                            VALUES (source.name, source.label, source.description, source.type, source.isItemlist, source.isDelete,
                                    source.createdBy, source.modifiedBy)
                        OUTPUT inserted.id INTO @OutputTable;
                        "
                    );

                    // 獲取插入或更新後的 menulistId
                    queryBuilder.AppendLine("SELECT * FROM @OutputTable;");

                    // 設定 command 的查詢語句
                    command.CommandText = queryBuilder.ToString();

                    // 清除上回 loop 的參數
                    command.Parameters.Clear();

                    // 自訂序列化選項，避免中文轉成 ASCII
                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 允許非 ASCII 字符
                        WriteIndented = true // 格式化輸出
                    };

                    // 設置 menulist 的參數
                    command.Parameters.AddWithValue("@Value1", mappingItem.Name);
                    command.Parameters.AddWithValue("@Value2", JsonSerializer.Serialize(mappingItem.I18n, options)); // 將物件轉換為 JSON string
                    command.Parameters.AddWithValue("@Value3", "");
                    command.Parameters.AddWithValue("@Value4", mappingItem.Type);
                    command.Parameters.AddWithValue("@Value5", mappingItem.IsItemList);
                    command.Parameters.AddWithValue("@Value6", mappingItem.IsDelete);
                    command.Parameters.AddWithValue("@Value7", user);
                    command.Parameters.AddWithValue("@Value8", user);

                    var sqlResult = await command.ExecuteScalarAsync();
                    Debug.Write($"MenuList name: {mappingItem.Name}, ");
                    Debug.WriteLine("MenuList id: " + sqlResult);
                    if (sqlResult != DBNull.Value) // 檢查是否為 DBNull
                    {
                        allMenuListIds.Add(Convert.ToInt32(sqlResult));
                    }

                    // ===== droplist =====
                    // droplist 臨時表格
                    string tempTableName = $"##TempDropList_{Guid.NewGuid().ToString("N")}";
                    string createTableQuery = $@"
                        CREATE TABLE {tempTableName} (
                            label NVARCHAR(1000),
                            value NVARCHAR(50),
                            description NVARCHAR(200),
                            menulistId BIGINT,
                            type NVARCHAR(50),
                            isDelete BIT,
                            createdBy BIGINT,
                            modifiedBy BIGINT
                        )";

                    using (var createTablecommand = new SqlCommand(createTableQuery, connection, transaction))
                    {
                        await createTablecommand.ExecuteNonQueryAsync();
                    }

                    // 收集 droplist 的 value
                    foreach (var dropItem in mappingItem.DropList)
                    {
                        allDropListValues.Add(dropItem.Value);
                    }

                    // 如果 DropList 有資料，進行批量插入到臨時表
                    if (mappingItem.DropList.Count > 0)
                    {
                        // 建立 droplist 臨時表
                        var dropListTable = DynamicModel.CreateDataTable(dropListSystemInfo, API.Create);

                        dropListTable.Columns.Add("IsDelete", typeof(bool));
                        dropListTable.Columns.Add("createdBy", typeof(long));
                        dropListTable.Columns.Add("modifiedBy", typeof(long));

                        // 將資料加入 DataTable
                        foreach (var dropItem in mappingItem.DropList)
                        {
                            dropListTable.Rows.Add(
                                JsonSerializer.Serialize(dropItem.I18n, options),
                                dropItem.Value,
                                "",
                                Convert.ToInt64(sqlResult),
                                dropItem.Type,
                                dropItem.IsDelete,
                                user,
                                user
                            );
                        }

                        // 使用 SqlBulkCopy 將 DataTable 插入到資料庫臨時表
                        // SQLBulkCopy with Transaction
                        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = tempTableName; // 臨時表名稱
                            bulkCopy.ColumnMappings.Add("label", "label");
                            bulkCopy.ColumnMappings.Add("value", "value");
                            bulkCopy.ColumnMappings.Add("description", "description");
                            bulkCopy.ColumnMappings.Add("menulistId", "menulistId");
                            bulkCopy.ColumnMappings.Add("type", "type");
                            bulkCopy.ColumnMappings.Add("isDelete", "isDelete");
                            bulkCopy.ColumnMappings.Add("createdBy", "createdBy");
                            bulkCopy.ColumnMappings.Add("modifiedBy", "modifiedBy");

                            await bulkCopy.WriteToServerAsync(dropListTable);

                        }

                        // 使用 MERGE 語句進行 UPSERT
                        var mergeQuery = $@"
                            MERGE INTO dbo.droplist AS target
                            USING {tempTableName} AS source
                            ON target.menulistId = source.menulistId AND target.value = source.value
                            WHEN MATCHED THEN
                                UPDATE SET
                                    target.label = source.label,
                                    target.description = source.description,
                                    target.type = source.type,
                                    target.isDelete = source.isDelete,
                                    target.createdBy = source.createdBy,
                                    target.modifiedBy = source.modifiedBy
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (label, value, description, menulistId, type, isDelete, createdBy, modifiedBy)
                                VALUES (source.label, source.value, source.description, source.menulistId, source.type, 
                                        source.isDelete, source.createdBy, source.modifiedBy);
                        ";

                        using (var mergeCommand = new SqlCommand(mergeQuery, connection, transaction))
                        {
                            await mergeCommand.ExecuteNonQueryAsync();
                        }

                        // 清理全域臨時表
                        string dropTableQuery = $"DROP TABLE {tempTableName}";
                        using (var dropCommand = new SqlCommand(dropTableQuery, connection, transaction))
                        {
                            await dropCommand.ExecuteNonQueryAsync();
                        }



                    }
                }

                // 以下沒辦法個別 menulist 檢查，所以如果有重複的 droplist ，也就是同一個 droplist 出現在兩個 menulist 中，並不會算做多餘，
                // 改進方法: 將這兩個檢查放到上面的迴圈中，對 menulist 做個別檢查，這樣每個 menulist 都是獨立的個體。

                // 刪除多餘的 droplist 資料並回傳 ID
                StringBuilder deleteDropListQuery = new StringBuilder();
                deleteDropListQuery.Append(@"
                            DECLARE @DeletedDropList TABLE (Id BIGINT);

                            DELETE FROM dbo.droplist
                            OUTPUT DELETED.Id INTO @DeletedDropList
                            WHERE menulistId NOT IN (" + string.Join(",", allMenuListIds) + @")
                            OR value NOT IN (" + string.Join(",", allDropListValues.Select(v => $"'{v}'")) + @");

                            SELECT Id FROM @DeletedDropList;
                        ");

                command.CommandText = deleteDropListQuery.ToString();
                var deletedDropListIdsList = new List<long>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.IsDBNull(0)) continue; // Skip if value is DBNull

                        deletedDropListIdsList.Add(reader.GetInt64(0));
                    }

                    Debug.WriteLine("==================deletedDropListIdsList==================");
                    Debug.WriteLine(string.Join(", ", deletedDropListIdsList));
                }
                // 刪除多餘的 menulist 資料並回傳 ID
                StringBuilder deleteMenuListQuery = new StringBuilder();
                deleteMenuListQuery.Append(@"
                    DECLARE @DeletedMenuList TABLE (Id INT);

                    DELETE FROM dbo.menulist
                    OUTPUT DELETED.Id INTO @DeletedMenuList
                    WHERE id NOT IN (" + string.Join(",", allMenuListIds) + @");

                    SELECT Id FROM @DeletedMenuList;
                ");

                command.CommandText = deleteMenuListQuery.ToString();
                var deletedMenuListIdsList = new List<int>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.IsDBNull(0)) continue; // Skip if value is DBNull

                        deletedMenuListIdsList.Add(reader.GetInt32(0));
                    }

                    Debug.WriteLine("==================deletedMenuListIdsList==================");
                    Debug.WriteLine(string.Join(", ", deletedMenuListIdsList));
                }

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> UpdateDataSource(List<MappingItem_propertyAndMenuList> mappingItems)
        {
            // 構建 SQL 語句
            string query = $@"
                                UPDATE p
                                SET p.dataSource = @Value_name
                                FROM dbo.property p
                                INNER JOIN dbo.itemtype it ON it.name = @Value_itemTypeName
                                WHERE p.itemTypeId = it.id AND p.name = @Value_propertyName;";

            // 開始UPDATE
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 開始交易
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int rowsAffected = 0;
                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            foreach (var item in mappingItems)
                            {
                                command.Parameters.Clear();

                                // 添加條件參數
                                command.Parameters.AddWithValue("@Value_name", item.Name);
                                command.Parameters.AddWithValue("@Value_itemTypeName", item.ItemTypeName);
                                command.Parameters.AddWithValue("@Value_propertyName", item.PropertyName);

                                // 執行
                                int rowAffected = await command.ExecuteNonQueryAsync();
                                rowsAffected += rowAffected;
                            }
                        }

                        // 如果沒有匹配到記錄
                        if (rowsAffected == 0)
                        {
                            return false;
                        }

                        // 提交交易
                        await transaction.CommitAsync();

                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

        }


        public async Task<bool> CreateDataBase(
            string dataBaseName,
            string filePath,
            string logFilePath)
        {
            try
            {
                await SqlHelper.ExecuteTransactionAsync<bool>(_connectionString, async (transaction, command) =>
                {
                    // 獲取指定預存程序檔案路徑
                    DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
                    string filePath = System.IO.Path.Combine(directoryInfo.Parent != null ?
                        directoryInfo.Parent.FullName : string.Empty, "CreateDataBaseQuery/CreateDatabase.sql");

                    // 從檔案讀取 SQL 模板
                    string sqlTemplate = File.ReadAllText(filePath);

                    // 動態替換參數
                    string sql = sqlTemplate
                        .Replace("@DatabaseName", dataBaseName)
                        .Replace("@FileName", filePath)
                        .Replace("@LogFileName", logFilePath);

                    string[] sqlCommands = sqlTemplate.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string commandText in sqlCommands)
                    {
                        command.CommandText = commandText.Trim();
                        await command.ExecuteNonQueryAsync();
                    }


                    // 執行預存程序
                    await command.ExecuteNonQueryAsync();


                    return true;
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw;
            }
        }

        #region Helpers
        /// <summary>
        /// 解析資料庫欄位DESCRIPTION中的值
        /// </summary>
        /// <param name="description">JSON格式的string</param>
        /// <returns>propertyType, label</returns>
        private static (string propertyType, string label) ParseDescription(string description)
        {
            try
            {
                int jsonStartIndex = description.IndexOf('{');
                int jsonEndIndex = description.LastIndexOf('}') + 1;

                // Extract the valid JSON string
                string validJson = description.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex);

                // 解析 JSON 為 JsonDocument
                using (JsonDocument document = JsonDocument.Parse(validJson))
                {
                    string propertyType = document.RootElement.GetProperty("PropertyType").ToString();

                    string label = document.RootElement.GetProperty("Label").ToString();

                    return (propertyType, label);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(description);
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        #endregion

    }
}
