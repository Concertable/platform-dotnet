using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.Kernel.Serializers;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DateTime dateTime = DateTime.Parse(reader.GetString()!);
        return TimeOnly.FromDateTime(dateTime);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("HH:mm:ss"));
    }
}
