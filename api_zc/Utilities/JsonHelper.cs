using System.Text.Json;

namespace Accura_MES.Utilities
{
    public class JsonHelper
    {

        /// <summary>
        /// 序列化方法
        /// </summary>
        /// <param name="json"></param>
        /// <remarks>
        /// 1.忽略大小寫
        /// 2.避免編碼特殊字元
        /// </remarks>
        /// <returns></returns>
        public static string Serialize(object json)
        {
            // 定義序列化選項
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 忽略大小寫
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 避免編碼特殊字元
            };

            return JsonSerializer.Serialize(json, options);
        }

        /// <summary>
        /// 反序列化方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <remarks>
        /// 1.忽略大小寫
        /// 2.避免編碼特殊字元
        /// </remarks>
        /// <returns></returns>
        public static T? Deserialize<T>(string json)
        {
            // 定義序列化選項
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 忽略大小寫
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 避免編碼特殊字元
            };

            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}
