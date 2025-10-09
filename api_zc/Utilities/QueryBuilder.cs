using System.Text;

namespace Accura_MES.Utilities
{
    public class QueryBuilder
    {
        /// <summary>
        /// Query : 建立一筆指定資料表的資料並回傳inserted id
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns">要插入的欄位</param>
        /// <returns>Query</returns>
        public static string BuildInsertQuery(string tableName, List<string> columns)
        {
            var queryBuilder = new StringBuilder();

            queryBuilder.AppendLine($"INSERT INTO [{tableName}]");

            queryBuilder.AppendLine($"({string.Join(", ", columns)})");

            queryBuilder.AppendLine("OUTPUT INSERTED.id");

            queryBuilder.AppendLine("VALUES");

            queryBuilder.AppendLine("(");

            // 所有欄位
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                // 每個欄位建立對應的參數名稱，例如 @id、@companyId 等
                queryBuilder.Append($"@{columns[colIndex]}");

                // 若不是最後一個欄位，則添加逗號
                if (colIndex < columns.Count - 1)
                {
                    queryBuilder.AppendLine(",");
                }
            }
            queryBuilder.AppendLine(")");


            return queryBuilder.ToString();
        }


        /// <summary>
        /// Query : 建立多筆指定資料表的資料並回傳inserted id
        /// </summary>
        /// <param name="tableName">資料表名</param>
        /// <param name="rowIndex">使用者輸入幾筆資料</param>
        /// <param name="columns">這張表的所有欄位名稱</param>
        /// <param name="parametersList">用來參數注入的參數名</param>
        /// <returns>query</returns>
        public static string BuildMultiInsertQuery(string tableName, List<string> columns, int rowIndex, out List<List<string>> parametersList)
        {
            var queryBuilder = new StringBuilder();
            parametersList = new List<List<string>>();

            queryBuilder.AppendLine($"INSERT INTO [{tableName}]");

            queryBuilder.AppendLine($"({string.Join(", ", columns)})");

            queryBuilder.AppendLine("OUTPUT INSERTED.id");

            queryBuilder.AppendLine("VALUES");


            // 輸入有幾筆資料，就建立幾筆VALUE
            for (int i = 0; i < rowIndex; i++)
            {
                var parameters = new List<string>();
                queryBuilder.AppendLine("(");

                // 所有欄位
                for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                {
                    // 每個欄位建立對應的參數名稱，例如 @id_0、@companyId_0 等
                    queryBuilder.Append($"@{columns[colIndex]}_{i}");
                    parameters.Add($"@{columns[colIndex]}_{i}");    // 記錄參數名

                    // 若不是最後一個欄位，則添加逗號
                    if (colIndex < columns.Count - 1)
                    {
                        queryBuilder.AppendLine(",");
                    }
                }
                queryBuilder.AppendLine(")");

                // 若不是最後一筆資料，則添加逗號
                if (i < rowIndex - 1)
                {
                    queryBuilder.AppendLine(",");
                }

                parametersList.Add(parameters);
            }

            return queryBuilder.ToString();
        }


        /// <summary>
        /// 產生複製資料的 SQL 語句。
        /// 回傳 INSERT INTO 的 SQL 語句，並使用 OUTPUT 取得自動生成的 ID。
        /// </summary>
        /// <remarks>
        /// helper of GenericRepository.DuplicateInactiveRecord()
        /// </remarks>
        public static string BuildCloneSqlWithOutput(
            string tableName,
            List<string> allColumns,
            string identityColumn = "id", // 回傳用主鍵
            List<string>? primaryKeys = null,
            Dictionary<string, object>? oldKeyValues = null,
            Dictionary<string, object>? newKeyValues = null,
            bool isEdit = false  // 是否為編輯動作產出的資料
            )
        {
            // 預設主鍵為 ID 欄位
            primaryKeys ??= new List<string> { identityColumn };

            // 過濾要插入的欄位（排除未指定新值的主鍵與"id"）
            var insertColumns = allColumns.Where(col =>
                (!primaryKeys.Contains(col, StringComparer.OrdinalIgnoreCase) ||     // 主鍵欄位
                (newKeyValues != null && newKeyValues.ContainsKey(col))) &&          // 指定新值的主鍵欄位
                !col.Equals("id", StringComparison.OrdinalIgnoreCase)       // ID 欄位
            ).ToList();

            // SELECT 部分
            var selectColumns = insertColumns.Select(col =>
            {
                if (newKeyValues != null && newKeyValues.ContainsKey(col))  // 指定要替換主鍵值
                    return $"'{newKeyValues[col]}' AS {col}";
                else if (col.Equals("cloneSourceId", StringComparison.OrdinalIgnoreCase))   // 將來源的 id 記錄到新資料的 cloneSourceId 欄位
                    return $"{identityColumn} AS cloneSourceId";
                else
                    return col;
            });

            if (isEdit)
            {
                // 編輯動作的話
                // 將 versionFlag、outBoundBy、cloneBy 欄位的值改為參數
                selectColumns = selectColumns.Select(col =>
                    col == "versionFlag" ? "@versionFlag" :
                    col == "outBoundBy" ? "@outBoundBy" :
                    col == "cloneBy" ? "@cloneBy" :
                    col
                ).ToList();
            }

            string insertColList = string.Join(", ", insertColumns);
            string selectColList = string.Join(", ", selectColumns);

            string whereClause = "";
            if (oldKeyValues != null && oldKeyValues.Any())
            {
                var conditions = oldKeyValues.Select(kv =>
                    $"{kv.Key} = '{kv.Value}'"
                );
                whereClause = "WHERE " + string.Join(" AND ", conditions);
            }

            // 用 OUTPUT 拿到自動生成的 ID（或其他主鍵）
            string sql = $@"
                DECLARE @Output TABLE ({identityColumn} BIGINT);

                INSERT INTO {tableName} ({insertColList})
                OUTPUT INSERTED.{identityColumn} INTO @Output
                SELECT {selectColList}
                FROM {tableName}
                {whereClause};

                SELECT * FROM @Output;
                ";

            return sql;
        }

    }
}
