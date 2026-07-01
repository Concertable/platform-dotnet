using System.Text.Json.Serialization;

namespace Concertable.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Genre
{
    Rock = 1,
    Pop = 2,
    Jazz = 3,
    HipHop = 4,
    Electronic = 5,
    Indie = 6,
    DnB = 7,
    House = 8
}
