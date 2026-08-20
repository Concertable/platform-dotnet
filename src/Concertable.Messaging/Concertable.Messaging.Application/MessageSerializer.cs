using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.Messaging.Application;

public sealed class MessageSerializer
{
    private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    public BinaryData Serialize<T>(T payload) =>
        new(JsonSerializer.SerializeToUtf8Bytes(payload, options));

    public object Deserialize(BinaryData body, Type targetType) =>
        JsonSerializer.Deserialize(body.ToStream(), targetType, options)
            ?? throw new InvalidOperationException($"Deserialization returned null for {targetType.FullName}.");
}
