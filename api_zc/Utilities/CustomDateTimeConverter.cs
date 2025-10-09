using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Accura_MES.Utilities
{
    /// <summary>
    /// Provides custom JSON serialization and deserialization for <see cref="DateTime"/> values using a specific date
    /// and time format.
    /// </summary>
    /// <remarks>This converter serializes <see cref="DateTime"/> values to the format "yyyy-MM-dd HH:mm:ss"
    /// and deserializes strings in the same format back to <see cref="DateTime"/> objects. If the input string does not
    /// match the custom format during deserialization, it falls back to the default <see
    /// cref="DateTime.Parse(string)"/> behavior.</remarks>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private const string CustomFormat = "yyyy-MM-dd HH:mm:ss";
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (DateTime.TryParseExact(value, CustomFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            return DateTime.Parse(value!); // fallback to default
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(CustomFormat));
        }
    }
}
