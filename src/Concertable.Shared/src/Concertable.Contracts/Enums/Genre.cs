using System.Text.Json.Serialization;
using Concertable.Contracts.Serialization;

namespace Concertable.Contracts.Enums;

[JsonConverter(typeof(StrictCamelCaseEnumConverter<Genre>))]
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
