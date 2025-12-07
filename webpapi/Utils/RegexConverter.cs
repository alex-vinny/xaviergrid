using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicMongoAPI.Utils
{
    public class RegexConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Return the raw string value - no transformation needed during deserialization
            return reader.GetString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // Write string value as-is without escaping
            writer.WriteStringValue(value);
        }
    }
}