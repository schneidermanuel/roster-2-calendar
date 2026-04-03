using System.Globalization;
using System.Text.Json;

namespace RosterSync.Core.Internals;

public class RosterDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()!;
        return DateTime.SpecifyKind(
            DateTime.ParseExact(value, Format, CultureInfo.InvariantCulture),
            DateTimeKind.Utc
        );
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}