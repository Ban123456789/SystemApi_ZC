using Accura_MES.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Accura_MES.Utilities
{
    public class Utils
    {
        /// <summary>
        /// 避免注入攻擊
        /// </summary>
        /// <param name="input"></param>
        /// <returns>保留字母、數字和底線</returns>
        public static string RemoveSpecialCharacters(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9_]", ""); // 保留字母、數字和底線
        }

        #region 類型轉換工具方法

        /// <summary>
        /// 將 JsonElement 或其他類型轉換為 long
        /// </summary>
        /// <param name="value">要轉換的值</param>
        /// <param name="defaultValue">轉換失敗時的預設值，預設為 0</param>
        /// <returns>轉換後的 long 值</returns>
        public static long ConvertToLong(object? value, long defaultValue = 0)
        {
            if (value == null) return defaultValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt64();
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return long.TryParse(jsonElement.GetString(), out long result) ? result : defaultValue;
                }
            }

            if (long.TryParse(value.ToString(), out long parseResult))
            {
                return parseResult;
            }

            return defaultValue;
        }

        /// <summary>
        /// 將 JsonElement 或其他類型轉換為 int
        /// </summary>
        /// <param name="value">要轉換的值</param>
        /// <param name="defaultValue">轉換失敗時的預設值，預設為 0</param>
        /// <returns>轉換後的 int 值</returns>
        public static int ConvertToInt(object? value, int defaultValue = 0)
        {
            if (value == null) return defaultValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt32();
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return int.TryParse(jsonElement.GetString(), out int result) ? result : defaultValue;
                }
            }

            if (int.TryParse(value.ToString(), out int parseResult))
            {
                return parseResult;
            }

            return defaultValue;
        }

        /// <summary>
        /// 將 JsonElement 或其他類型轉換為 decimal
        /// </summary>
        /// <param name="value">要轉換的值</param>
        /// <param name="defaultValue">轉換失敗時的預設值，預設為 0</param>
        /// <returns>轉換後的 decimal 值</returns>
        public static decimal ConvertToDecimal(object? value, decimal defaultValue = 0)
        {
            if (value == null) return defaultValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetDecimal();
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return decimal.TryParse(jsonElement.GetString(), out decimal result) ? result : defaultValue;
                }
            }

            if (decimal.TryParse(value.ToString(), out decimal parseResult))
            {
                return parseResult;
            }

            return defaultValue;
        }

        /// <summary>
        /// 將 JsonElement 或其他類型轉換為 string
        /// </summary>
        /// <param name="value">要轉換的值</param>
        /// <param name="defaultValue">轉換失敗時的預設值，預設為空字串</param>
        /// <returns>轉換後的 string 值</returns>
        public static string ConvertToString(object? value, string defaultValue = "")
        {
            if (value == null) return defaultValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return jsonElement.GetString() ?? defaultValue;
                }
                else if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetRawText();
                }
                else if (jsonElement.ValueKind == JsonValueKind.True)
                {
                    return "true";
                }
                else if (jsonElement.ValueKind == JsonValueKind.False)
                {
                    return "false";
                }
            }

            return value.ToString() ?? defaultValue;
        }

        /// <summary>
        /// 將 JsonElement 或其他類型轉換為 bool
        /// </summary>
        /// <param name="value">要轉換的值</param>
        /// <param name="defaultValue">轉換失敗時的預設值，預設為 false</param>
        /// <returns>轉換後的 bool 值</returns>
        public static bool ConvertToBool(object? value, bool defaultValue = false)
        {
            if (value == null) return defaultValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True)
                {
                    return true;
                }
                else if (jsonElement.ValueKind == JsonValueKind.False)
                {
                    return false;
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return bool.TryParse(jsonElement.GetString(), out bool result) ? result : defaultValue;
                }
                else if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt32() != 0;
                }
            }

            if (bool.TryParse(value.ToString(), out bool parseResult))
            {
                return parseResult;
            }

            return defaultValue;
        }

        /// <summary>
        /// 將 JsonElement 或其他類型轉換為 DateTime
        /// </summary>
        /// <param name="value">要轉換的值</param>
        /// <param name="defaultValue">轉換失敗時的預設值，預設為 DateTime.MinValue</param>
        /// <returns>轉換後的 DateTime 值</returns>
        public static DateTime ConvertToDateTime(object? value, DateTime? defaultValue = null)
        {
            if (value == null) return defaultValue ?? DateTime.MinValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return DateTime.TryParse(jsonElement.GetString(), out DateTime result) ? result : (defaultValue ?? DateTime.MinValue);
                }
            }

            if (DateTime.TryParse(value.ToString(), out DateTime parseResult))
            {
                return parseResult;
            }

            return defaultValue ?? DateTime.MinValue;
        }

        #endregion

        /// <summary>
        /// 遞迴地移除 "password" 欄位
        /// </summary>
        /// <param name="dictionary"></param>
        public static void RemoveSensitiveData(Dictionary<string, object> dictionary)
        {
            var keysToRemove = new List<string> { "password" };

            foreach (var key in keysToRemove)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary.Remove(key);
                }
            }

            // 遞迴處理巢狀的 Dictionary
            foreach (var kvp in dictionary)
            {
                if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    var nestedDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                    RemoveSensitiveData(nestedDictionary);
                    dictionary[kvp.Key] = nestedDictionary;
                }
            }
        }

        /// <summary>
        /// 分割批次
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static IEnumerable<List<T>> SplitIntoBatches<T>(List<T> source, int batchSize)
        {
            for (int i = 0; i < source.Count; i += batchSize)
            {
                yield return source.GetRange(i, Math.Min(batchSize, source.Count - i));
            }
        }

        /// <summary>
        /// 比較 dict1 和 dict2 中的 KVP，將 dict2 中與 dict1 中任意一個字典的指定 KVP 相同的項目排除掉
        /// </summary>
        /// <param name="dict1"></param>
        /// <param name="dict2"></param>
        /// <param name="columns">指定 KVP 的 Key</param>
        /// <returns>從 dict2 中排除掉與 dict1 中相同的字典</returns>
        public static List<Dictionary<string, object>> RemoveMatchingDicts(
            List<Dictionary<string, object>> dict1,
            List<Dictionary<string, object>> dict2,
            List<string> columns)
        {
            return dict2.Where(d2 => !dict1.Any(d1 => DictionariesAreEqual(d1, d2, columns))).ToList();
        }

        /// <summary>
        /// 比較兩個字典，判斷指定 KVP 相不相同
        /// </summary>
        /// <param name="dict1"></param>
        /// <param name="dict2"></param>
        /// <param name="columns">指定 KVP 的 Key</param>
        /// <returns>true:指定 KVP 完全相同; false:otherwise</returns>
        public static bool DictionariesAreEqual(
            Dictionary<string, object> dict1,
            Dictionary<string, object> dict2,
            List<string> columns)
        {
            // 確保兩個字典中的指定 KVP 都相等
            return columns.All(column =>
                dict1.TryGetValue(column, out var value1) &&
                dict2.TryGetValue(column, out var value2) &&
                Equals(value1, value2));
        }


        
        #region WHERE clause process
        /// <summary>
        /// 避免注入攻擊
        /// </summary>
        /// <param name="operate"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string OperateCase(string operate, string value, ref int index)
        {
            if (string.IsNullOrWhiteSpace(operate)) return "";
            switch (operate.ToLower())
            {
                case "=":
                    return $"= @data{index++}";
                case "!=":
                    return $"!= @data{index++}";
                case ">":
                    return $"> @data{index++}";
                case "<":
                    return $"< @data{index++}";
                case ">=":
                    return $">= @data{index++}";
                case "<=":
                    return $"<= @data{index++}";
                case "like":
                    return $"LIKE '%' + @data{index++} + '%'";
                case "not like":
                    return $"NOT LIKE '%' + @data{index++} + '%'";
                case "null":
                    return $"IS NULL";
                case "not null":
                    return $"IS NOT NULL";
                case "in":
                    string itext = string.Empty;
                    string[] isplited = value.Split(',');
                    for (int i = 0; i < isplited.Length; i++)
                    {
                        if (i != 0)
                        {
                            itext += ",";
                        }
                        itext += $"@data{index++}";
                    }
                    return $"IN ({itext})";
                case "not in":
                    string text = string.Empty;
                    string[] splited = value.Split(',');
                    for (int i = 0; i < splited.Length; i++)
                    {
                        if (i != 0)
                        {
                            text += ",";
                        }
                        text += $"@data{index++}";
                    }
                    return $"NOT IN ({text})";
                case "between":
                    return $"BETWEEN @data{index++} AND @data{index++}";
                default:
                    throw new CustomErrorCodeException(SelfErrorCode.INVALID_PARAMETERS, $"{operate}");
            }
        }

        /// <summary>
        /// 計算參數數量
        /// </summary>
        /// <param name="operate"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static List<string> ValueCount(string operate, string Value)
        {
            List<string> values = new List<string>();
            if (string.IsNullOrWhiteSpace(operate)) return values;
            if (string.IsNullOrWhiteSpace(Value))
            {
                Value = "";
            }
            string[] splited = Value.Split(',');
            switch (operate.ToLower())
            {
                case "=":
                    values.Add(Value);
                    return values;
                case "!=":
                    values.Add(Value);
                    return values;
                case ">":
                    values.Add(Value);
                    return values;
                case "<":
                    values.Add(Value);
                    return values;
                case ">=":
                    values.Add(Value);
                    return values;
                case "<=":
                    values.Add(Value);
                    return values;
                case "like":
                    values.Add(Value);
                    return values;
                case "not like":
                    values.Add(Value);
                    return values;
                case "null":
                    return values;
                case "not null":
                    return values;
                case "empty":
                    return values;
                case "in":
                    for (int i = 0; i < splited.Length; i++)
                    {
                        values.Add(splited[i]);
                    }
                    return values;
                case "not in":
                    for (int i = 0; i < splited.Length; i++)
                    {
                        values.Add(splited[i]);
                    }
                    return values;
                case "between":
                    for (int i = 0; i < splited.Length; i++)
                    {
                        values.Add(splited[i]);
                    }
                    return values;
                default:
                    throw new CustomErrorCodeException(SelfErrorCode.INVALID_PARAMETERS, $"{operate}");
            }
        }

        /// <summary>
        /// 避免注入攻擊
        /// </summary>
        /// <param name="operate"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object OperateCase(string operate, object value, ref int index)
        {
            if (string.IsNullOrWhiteSpace(operate)) return "";
            switch (operate.ToLower())
            {
                case "=":
                    return $"= @data{index++}";
                case "!=":
                    return $"!= @data{index++}";
                case ">":
                    return $"> @data{index++}";
                case "<":
                    return $"< @data{index++}";
                case ">=":
                    return $">= @data{index++}";
                case "<=":
                    return $"<= @data{index++}";
                case "like":
                    return $"LIKE '%' + @data{index++} + '%'";
                case "not like":
                    return $"NOT LIKE '%' + @data{index++} + '%'";
                case "null":
                    return $"IS NULL";
                case "not null":
                    return $"IS NOT NULL";
                case "in":
                    string itext = string.Empty;
                    string[] isplited = value?.ToString()?.Split(',') ?? throw new ArgumentNullException("輸入之值不予許為 null");
                    for (int i = 0; i < isplited.Length; i++)
                    {
                        if (i != 0)
                        {
                            itext += ",";
                        }
                        itext += $"@data{index++}";
                    }
                    return $"IN ({itext})";
                case "not in":
                    string text = string.Empty;
                    string[] splited = value?.ToString()?.Split(',') ?? throw new ArgumentNullException("輸入之值不予許為 null");
                    for (int i = 0; i < splited.Length; i++)
                    {
                        if (i != 0)
                        {
                            text += ",";
                        }
                        text += $"@data{index++}";
                    }
                    return $"NOT IN ({text})";
                case "between":
                    return $"BETWEEN @data{index++} AND @data{index++}";
                default:
                    throw new CustomErrorCodeException(SelfErrorCode.INVALID_PARAMETERS, $"{operate}");
            }
        }

        /// <summary>
        /// 計算參數數量
        /// </summary>
        /// <param name="operate"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static List<object> ValueCount(string operate, object Value)
        {
            List<object> values = new List<object>();
            if (string.IsNullOrWhiteSpace(operate)) return values;
            if (string.IsNullOrWhiteSpace(Value?.ToString()))
            {
                // 若輸入為空值則轉換成空字串
                Value = "";
            }
            string[] splited = Value.ToString().Split(',');
            switch (operate.ToLower())
            {
                case "=":
                    values.Add(Value);
                    return values;
                case "!=":
                    values.Add(Value);
                    return values;
                case ">":
                    values.Add(Value);
                    return values;
                case "<":
                    values.Add(Value);
                    return values;
                case ">=":
                    values.Add(Value);
                    return values;
                case "<=":
                    values.Add(Value);
                    return values;
                case "like":
                    values.Add(Value);
                    return values;
                case "not like":
                    values.Add(Value);
                    return values;
                case "null":
                    return values;
                case "not null":
                    return values;
                case "in":
                    for (int i = 0; i < splited.Length; i++)
                    {
                        values.Add(splited[i]);
                    }
                    return values;
                case "not in":
                    for (int i = 0; i < splited.Length; i++)
                    {
                        values.Add(splited[i]);
                    }
                    return values;
                case "between":
                    for (int i = 0; i < splited.Length; i++)
                    {
                        values.Add(splited[i]);
                    }
                    return values;
                default:
                    throw new CustomErrorCodeException(SelfErrorCode.INVALID_PARAMETERS, $"{operate}");
            }
        }

        /// <summary>
        /// 建立 WHERE 條件子句
        /// </summary>
        /// <param name="AndQuery"></param>
        /// <param name="OrQuery"></param>
        /// <param name="OrderQuery"></param>
        /// <returns></returns>
        public static string QueryText(List<QueryObject> AndQuery, List<QueryObject> OrQuery, List<QueryObject> OrderQuery)
        {
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
                        andConditions.Add($"([{RemoveSpecialCharacters(AndQueryItem.Field)}] IS NULL OR [{RemoveSpecialCharacters(AndQueryItem.Field)}] = '')");
                    }

                    // 一般運算元
                    else
                    {
                        andConditions.Add($"[{RemoveSpecialCharacters(AndQueryItem.Field)}] {OperateCase(AndQueryItem.Operate, AndQueryItem.Value, ref index)}");
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
                        orConditions.Add($"([{RemoveSpecialCharacters(OrQueryItem.Field)}] IS NULL OR [{RemoveSpecialCharacters(OrQueryItem.Field)}] = '')");
                    }

                    // 一般運算元
                    else
                    {
                        orConditions.Add($"[{RemoveSpecialCharacters(OrQueryItem.Field)}] {OperateCase(OrQueryItem.Operate, OrQueryItem.Value, ref index)}");
                    }
                }

                QueryStr = string.Join(" OR ", orConditions);
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
                            QueryStr += $" ORDER BY {RemoveSpecialCharacters(OrderQueryItem.Field)} {sortOrder}";
                        else
                            QueryStr += $" , {RemoveSpecialCharacters(OrderQueryItem.Field)} {sortOrder}";
                    }

                    orderIndex++;
                }
            }
            return QueryStr;
        }
        #endregion


    }

    /// <summary>
    /// 資料庫型別與 CSharp 型別解析
    /// </summary>
    public class TypeConverter
    {
        private static readonly Dictionary<string, Type> SqlTypeToCSharpType = new Dictionary<string, Type>
        {
            { "bigint", typeof(long) },
            { "nvarchar", typeof(string) },
            { "int", typeof(int) },
            { "datetime", typeof(DateTime) },
            { "bit", typeof(bool) },
            { "decimal", typeof(decimal) },
            { "float", typeof(float) },
            { "varchar", typeof(string) },
            { "char", typeof(char) }
        };

        /// <summary>
        /// image
        /// i18n
        /// int
        /// decimal
        /// list
        /// text
        /// time
        /// itemlist
        /// date
        /// item
        /// shortdate
        /// ID
        /// string
        /// boolean
        /// </summary>
        private static readonly Dictionary<string, Type> PropertyTypeToCSharpType = new Dictionary<string, Type>
        {
            { "image", typeof(string) },
            { "i18n", typeof(string) },
            { "int", typeof(int) },
            { "decimal", typeof(decimal) },
            { "list", typeof(string) },
            { "text", typeof(string) },
            { "time", typeof(decimal) },    // 前端會解釋為時分秒，所以資料庫基本上都是數字
            { "itemlist", typeof(string) },
            { "date", typeof(DateTime) },
            { "item", typeof(long) },
            { "shortdate", typeof(DateTime) },
            { "id", typeof(long) },
            { "string", typeof(string) },
            { "boolean", typeof(bool) }
        };

        /// <summary>
        /// 將 sql 的型別解析為 csharp 的型別
        /// </summary>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Type GetCSharpType(string sqlType)
        {
            if (SqlTypeToCSharpType.TryGetValue(sqlType.ToLower(), out var csharpType))
            {
                return csharpType;
            }

            throw new ArgumentException($"未知的 SQL 類型: [{sqlType}]");
        }

        /// <summary>
        /// 將 [property].propertyType 的字串解析為 csharp 的型別
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Type GetCSharpType2(string propertyType)
        {
            if (PropertyTypeToCSharpType.TryGetValue(propertyType.ToLower(), out var csharpType))
            {
                return csharpType;
            }

            throw new ArgumentException($"未知的 propertyType 類型: [{propertyType}]");
        }
    }
}
