using System.Text.Json.Serialization;

namespace Concertable.Shared;

public class AutocompleteDto : IHasName
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    [JsonPropertyName("$type")]
    public required string Type { get; init; }
}
