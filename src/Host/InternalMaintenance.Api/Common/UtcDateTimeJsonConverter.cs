using System.Text.Json;
using System.Text.Json.Serialization;

namespace InternalMaintenance.Api.Common;

public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return default;
        }

        return DateTime.Parse(dateString, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcDateTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
