using System.Text.Json;
using Concertable.Contracts.Enums;
using Concertable.Shared.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Concertable.Shared.Api.UnitTests;

public sealed class GenreWireFormatTests
{
    private static JsonSerializerOptions ApplicationOptions()
    {
        var services = new ServiceCollection();
        services.AddControllers().AddApplicationJson();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Theory]
    [InlineData(Genre.Rock, "\"rock\"")]
    [InlineData(Genre.HipHop, "\"hipHop\"")]
    [InlineData(Genre.DnB, "\"dnB\"")]
    [InlineData(Genre.Electronic, "\"electronic\"")]
    [InlineData(Genre.House, "\"house\"")]
    public void SerializesCamelCase_UnderApplicationOptions(Genre genre, string expected)
        => Assert.Equal(expected, JsonSerializer.Serialize(genre, ApplicationOptions()));

    [Theory]
    [InlineData(Genre.HipHop, "\"hipHop\"")]
    [InlineData(Genre.DnB, "\"dnB\"")]
    public void SerializesCamelCase_ViaTypeAttribute_WithoutGlobalConverter(Genre genre, string expected)
        => Assert.Equal(expected, JsonSerializer.Serialize(genre));

    [Theory]
    [InlineData("\"hipHop\"", Genre.HipHop)]
    [InlineData("\"dnB\"", Genre.DnB)]
    public void DeserializesCamelCase(string json, Genre expected)
        => Assert.Equal(expected, JsonSerializer.Deserialize<Genre>(json, ApplicationOptions()));

    [Fact]
    public void RejectsNumericInput()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Genre>("4", ApplicationOptions()));
}
