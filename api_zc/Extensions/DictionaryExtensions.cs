using System.Diagnostics;
using System.Text.Json;

namespace Accura_MES.Extensions
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// 從 Dictionary&lt;string, object?&gt; 讀取值，
        /// 嘗試轉換成指定型別 T
        /// <para></para>
        /// 轉換失敗時，拋出 CustomErrorCodeException
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">字典的鍵</param>
        /// <param name="errorCode">若讀取值發生例外，指定擲出的例外內容</param>
        /// <param name="errorMessage">若讀取值發生例外，指定擲出的例外內容</param>
        /// <returns>讀取成功並轉換型別後的值</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        /// <remarks>
        /// Obsolete, Superseded by<see cref="GetValueThenTryParseOrThrow{T}(Dictionary{string, object?}, string, SelfErrorCode?, string)"/>
        /// </remarks>
        [Obsolete("use GetValueThenTryParseOrThrow() instead")]
        public static T? GetValueOrThrow<T>(
            this Dictionary<string, object?> dictionary,
            string key,
            SelfErrorCode? errorCode = null,
            string errorMessage = "")
        {
            #region 預設輸入

            // 1.error code
            if (errorCode is null)
            {
                errorCode = SelfErrorCode.INTERNAL_SERVER_WITH_MSG;
            }

            // 2.訊息
            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = $"無法解析輸入的字典，請確認格式是否正確";
            }

            #endregion

            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (!dictionary.TryGetValue(key, out var value) || value is null)
                throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key})");

            if (value is DBNull)
                return default;

            try
            {
                // 處理 JsonElement
                if (value is JsonElement jsonElement)
                {
                    if (typeof(T) == typeof(DateTime))
                    {
                        // 嘗試解析成字串，然後轉換成 DateTime
                        if (jsonElement.ValueKind == JsonValueKind.String &&
                            DateTime.TryParse(jsonElement.GetString(), out var dateValue))
                        {
                            return (T)(object)dateValue;
                        }
                        throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key}, Invalid Date Format: {jsonElement.GetString()})");
                    }

                    // 其餘的型別
                    return jsonElement.Deserialize<T>() ??
                        throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key}, Value is null after deserialization)");
                }

                // 直接轉型，如果失敗則拋出錯誤
                return (T)value!;
            }
            catch (CustomErrorCodeException cusEx)
            {
                throw new CustomErrorCodeException(
                    errorCode, $"message:[{errorMessage}] stack trace:[{cusEx}]", $"Key: {key}, Value: {value}");
            }
            catch (JsonException ex)  // 捕捉 JSON 解析錯誤
            {
                throw new CustomErrorCodeException(
                    errorCode, $"message:[{errorMessage}] stack trace:[{ex}]", $"Key: {key}, Value: {value}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 嘗試將 input 轉換為指定的類型 T，如果成功則返回轉換後的值，否則一律返回 defaultValue。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key">字典的鍵</param>
        /// <param name="defaultValue">若失敗返回的預設值</param>
        /// <returns>讀取成功並轉換型別後的值; 失敗則返回指定預設值 <paramref name="defaultValue"/></returns>
        public static T? GetValueThenTryParseOrDefault<T>(
            this Dictionary<string, object?> dictionary,
            string key,
            T defaultValue)
        {
            #region 預設輸入

            SelfErrorCode? errorCode = SelfErrorCode.INTERNAL_SERVER_WITH_MSG;
            string errorMessage = $"無法解析輸入的字典，請確認格式是否正確";

            #endregion


            try
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                if (!dictionary.TryGetValue(key, out object? value))
                    throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key})");

                // 嘗試轉型
                return ParseValue<T>(key, errorCode, errorMessage, value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"嘗試取得並轉換字典值發生錯誤，已將數值設為預設值 Key:[{key}] Exception:[{ex}] DefaultValue:[{defaultValue}]");
                return defaultValue;
            }
        }

        /// <summary>
        /// 嘗試將 input 轉換為指定的類型 T，如果成功則返回轉換後的值，否則擲出例外
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key">字典的鍵</param>
        /// <param name="errorCode">若讀取值發生例外，指定擲出的例外內容</param>
        /// <param name="errorMessage">若讀取值發生例外，指定擲出的例外內容</param>
        /// <returns>讀取成功並轉換型別後的值</returns>
        /// <remarks>
        /// 如果值是 DBNull.Value 或 null，則返回 default(T)。
        /// </remarks>
        /// <exception cref="CustomErrorCodeException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static T? GetValueThenTryParseOrThrow<T>(
            this Dictionary<string, object?> dictionary,
            string key,
            SelfErrorCode? errorCode = null,
            string errorMessage = "")
        {
            #region 預設輸入

            // 1.error code
            if (errorCode is null)
                errorCode = SelfErrorCode.INTERNAL_SERVER_WITH_MSG;

            // 2.訊息
            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = $"無法解析輸入的字典，請確認格式是否正確";

            #endregion

            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "字典參數為 null");

            if (!dictionary.TryGetValue(key, out object? value))
                throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key})");

            // 特殊處理 DBNull.Value 或 null
            if (value == DBNull.Value || value == null)
                return default;

            // 嘗試轉型
            return ParseValue<T>(key, errorCode, errorMessage, value);
        }

        /// <summary>
        /// 嘗試將 input 轉換為指定的類型 T，如果成功則返回轉換後的值，否則擲出例外
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        private static T? ParseValue<T>(string key, SelfErrorCode errorCode, string errorMessage, object value)
        {

            // 處理 Nullable 型別
            var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            // 處理 JsonElement
            if (value is JsonElement jsonElement)
            {
                // 特殊處理 DateTime 類型
                if (underlyingType == typeof(DateTime))
                {
                    // 嘗試解析成字串，然後轉換成 DateTime
                    if (jsonElement.ValueKind == JsonValueKind.String &&
                        DateTime.TryParse(jsonElement.GetString(), out var dateValue))
                    {
                        return (T)(object)dateValue;
                    }
                    throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key}, Invalid Date Format: {jsonElement.GetString()})");
                }

                if (underlyingType == typeof(List<DateTime>))
                {
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        var dateList = new List<DateTime>();
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String &&
                                DateTime.TryParse(item.GetString(), out var dateValue))
                            {
                                dateList.Add(dateValue);
                            }
                            else
                            {
                                throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key}, Invalid Date Format: {item.GetString()})");
                            }
                        }

                        return (T)(object)dateList;
                    }
                    throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key}, Invalid Date Format: {jsonElement.GetString()})");
                }

                // 特殊處理 bool 類型（數字轉 bool）
                if (underlyingType == typeof(bool))
                {
                    if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out int intValue))
                    {
                        return (T)(object)(intValue != 0);
                    }
                    if (jsonElement.ValueKind == JsonValueKind.String && int.TryParse(jsonElement.GetString(), out intValue))
                    {
                        return (T)(object)(intValue != 0);
                    }
                }

                // 使用 RawText 轉型，泛用處理其他所有型別（包含 class, list 等）
                var raw = jsonElement.GetRawText();
                return JsonSerializer.Deserialize<T>(raw)
                    ?? throw new CustomErrorCodeException(errorCode, $"{errorMessage} (Key: {key}, Value is null after deserialization)");
            }


            // 特殊處理 char
            if (underlyingType == typeof(char))
            {
                return (T?)(object)Convert.ToString(value)![0];
            }

            // 一般轉型
            return (T?)Convert.ChangeType(value, underlyingType);
        }
    }

}
