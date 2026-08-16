using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.Contracts.Serialization;

public sealed class StrictCamelCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public StrictCamelCaseEnumConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
