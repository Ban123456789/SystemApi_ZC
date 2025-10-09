using Accura_MES.Interfaces.Repositories;
using Accura_MES.Repositories;
using Accura_MES.Service;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace Accura_MES.Models
{
    public class DynamicModel : DataTable
    {
        /// <summary>
        /// 防止外部直接創建 DynamicModel 物件
        /// </summary>
        private DynamicModel()
        {
        }

        /// <summary>
        /// 根據輸入的欄位資訊創建 DataTable 並設定好欄位
        /// </summary>
        /// <param name="tableInfo">List of (columnName, columnInfo)</param>
        /// <returns></returns>
        public static DynamicModel CreateDataTable(List<Dictionary<string, object>> tableInfo, API apiType)
        {
            var dynamicModel = new DynamicModel();

            // 遍歷所有欄位資訊
            foreach (var columnInfo in tableInfo)
            {
                // 確保欄位資訊完整
                if (!columnInfo.ContainsKey("COLUMN_NAME") || !columnInfo.ContainsKey("DATA_TYPE"))
                {
                    throw new ArgumentException("缺少必要的欄位資訊：COLUMN_NAME 或 DATA_TYPE。");
                }

                // 獲取欄位名稱與資料型別
                var columnName = columnInfo["COLUMN_NAME"].ToString();
                var dataType = columnInfo["DATA_TYPE"].ToString();

                if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(dataType))
                {
                    throw new ArgumentException("欄位名稱或資料型別不能為空。");
                }

                // 略過不允許的欄位
                var filter = InputValidationService.GetInputFilterByEnum(apiType);
                if (filter.Contains(columnName))
                {
                    continue;
                }

                // 添加欄位到臨時表
                try
                {
                    dynamicModel.Columns.Add(columnName, Utilities.TypeConverter.GetCSharpType(dataType));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"無法新增欄位 '{columnName}'，資料型別 '{dataType}' 無效。", ex);
                }
            }


            return dynamicModel;
        }

        /// <summary>
        /// 根據輸入的欄位資訊創建 DataTable 並設定好欄位
        /// </summary>
        /// <param name="columnInfos">COLUMN_NAME, DATA_TYPE</param>
        /// <returns></returns>
        public static DynamicModel CreateDataTable(Dictionary<string, string> columnInfos, API apiType)
        {
            var dynamicModel = new DynamicModel();

            // 遍歷所有欄位資訊
            foreach (var columnInfo in columnInfos)
            {
                if (string.IsNullOrEmpty(columnInfo.Value))
                {
                    throw new ArgumentException("資料型別不能為空。");
                }

                // 略過不允許的欄位
                var filter = InputValidationService.GetInputFilterByEnum(apiType);
                if (filter.Contains(columnInfo.Key))
                {
                    continue;
                }

                // 添加欄位到臨時表
                try
                {
                    dynamicModel.Columns.Add(columnInfo.Key, Utilities.TypeConverter.GetCSharpType(columnInfo.Value));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"無法新增欄位 '{columnInfo.Key}'，資料型別 '{columnInfo.Value}' 無效。", ex);
                }
            }


            return dynamicModel;
        }


        /// <summary>
        /// 根據輸入的欄位資訊創建 DataTable 並設定好欄位
        /// </summary>
        /// <param name="tableInfo">資料表資料</param>
        /// <param name="apiType">用來過濾特殊欄位</param>
        /// <param name="tableName">資料表名</param>
        /// <param name="exceptionColumns">要排除的欄位</param>
        /// <returns></returns>
        public static DynamicModel CreateDataTable(List<PropertyModel> tableInfo, API apiType,
            string? tableName = null, HashSet<string>? exceptionColumns = null)
        {
            var dynamicModel = new DynamicModel();

            dynamicModel.TableName = tableName;

            // 遍歷所有欄位資訊
            foreach (var columnInfo in tableInfo)
            {
                if (string.IsNullOrEmpty(columnInfo.PropertyType))
                {
                    throw new ArgumentException("資料型別不能為空。");
                }

                // 略過不允許的欄位
                var filter = InputValidationService.GetInputFilterByEnum(apiType);
                if (filter.Contains(columnInfo.Name))
                {
                    continue;
                }
                // 略過指定要排除的欄位
                if (exceptionColumns != null && exceptionColumns.Contains(columnInfo.Name))
                {
                    continue;
                }

                // 添加欄位到臨時表
                try
                {
                    dynamicModel.Columns.Add(columnInfo.Name, Utilities.TypeConverter.GetCSharpType2(columnInfo.PropertyType));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"無法新增欄位 '{columnInfo.Name}'，" +
                        $"資料型別 '{Utilities.TypeConverter.GetCSharpType2(columnInfo.PropertyType)}' 無效。", ex);
                }
            }


            return dynamicModel;
        }

        /// <summary>
        /// 根據輸入的資料表名稱創建 DataTable 並設定好欄位
        /// </summary>
        /// <param name="tableName">資料表名稱</param>
        /// <param name="apiType">用來過濾特定欄位</param>
        /// <param name="exceptionColumns">要排除的欄位</param>
        /// <remarks>
        /// 從 [property] 資料表中取得欄位資訊
        /// </remarks>
        /// <returns></returns>
        public static DynamicModel CreateDataTable(string connectionString, SqlConnection connection, SqlTransaction? transaction,
            string tableName, API apiType, HashSet<string>? exceptionColumns = null)
        {
            IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(connectionString);

            List<PropertyModel> tableInfo = propertyRepository.GetProperty(
                    tableName,
                    connection,
                    transaction);

            return CreateDataTable(tableInfo, apiType, tableName, exceptionColumns);
        }

        /// <summary>
        /// 將 DataTable 轉換為字串格式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // 加入表格名稱（如果有設定 TableName）
            if (!string.IsNullOrEmpty(this.TableName))
            {
                sb.AppendLine($"Table Name: {this.TableName}");
            }

            // 加入欄位名稱
            sb.AppendLine("Columns:");
            foreach (DataColumn column in this.Columns)
            {
                sb.Append($"{column.ColumnName}\t");
            }
            sb.AppendLine();

            // 加入每一行的資料
            sb.AppendLine("Rows:");
            foreach (DataRow row in this.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    sb.Append($"{item}\t");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

    }
}
