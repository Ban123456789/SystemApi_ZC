using System.Data;
using System.Text;

namespace Accura_MES.Utilities
{
    /// <summary>
    /// 通用的 SQL 查詢語句生成器
    /// </summary>
    internal static class GenericSqlQueryGenerator
    {

        /// <summary>
        /// 根據 DataTable 生成 CREATE TABLE 語句
        /// </summary>
        /// <param name="dataTable">包含欄位名稱、型別(支援 C# 型別)</param>
        /// <param name="tableName">要建立的資料表名稱</param>
        /// <returns>SQL query</returns>
        public static string GenerateCreateTableStatement(DataTable dataTable, string tableName)
        {
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine($"CREATE TABLE [{tableName}] (");

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                DataColumn column = dataTable.Columns[i];
                string columnName = column.ColumnName;
                string sqlType = GetSqlTypeFromType(column.DataType);
                sqlBuilder.Append($"    [{columnName}] {sqlType} NULL");

                if (i < dataTable.Columns.Count - 1)
                    sqlBuilder.AppendLine(",");
                else
                    sqlBuilder.AppendLine();
            }

            sqlBuilder.AppendLine(");");
            return sqlBuilder.ToString();

            static string GetSqlTypeFromType(Type type)
            {
                if (type == typeof(string)) return "NVARCHAR(MAX)";
                if (type == typeof(int)) return "INT";
                if (type == typeof(long)) return "BIGINT";
                if (type == typeof(decimal)) return "DECIMAL(18,5)";
                if (type == typeof(float)) return "FLOAT";
                if (type == typeof(double)) return "FLOAT";
                if (type == typeof(DateTime)) return "DATETIME";
                if (type == typeof(bool)) return "BIT";
                if (type == typeof(byte[])) return "VARBINARY(MAX)";
                // 視情況補上其他型別
                return type.ToString();
            }
        }
    }
}