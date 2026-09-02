using System.Text.Json;
using Concertable.Kernel;
using Concertable.Shared.Api.Extensions;
using Concertable.Shared.Api.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Concertable.Shared.Api.UnitTests;

public sealed class ActionLinkWireFormatTests
{
    private readonly JsonSerializerOptions options;

    public ActionLinkWireFormatTests()
    {
        var services = new ServiceCollection();
        services.AddControllers().AddApplicationJson();
        using var provider = services.BuildServiceProvider();
        this.options = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Fact]
    public void Post_SerializesHrefAsBareStringAndMethodAsVerb()
    {
        Assert.Equal(
            """{"href":"/api/application/42/accept","method":"POST"}""",
            JsonSerializer.Serialize(ActionLink.Post("/api/application/42/accept"), this.options));
    }

    [Fact]
    public void Get_SerializesHrefAsBareStringAndMethodAsVerb()
    {
        Assert.Equal(
            """{"href":"/api/concert/7/contract/pdf","method":"GET"}""",
            JsonSerializer.Serialize(ActionLink.Get("/api/concert/7/contract/pdf"), this.options));
    }

    [Fact]
    public void Deserialize_SerializedActionLink_RoundTrips()
    {
        var original = ActionLink.Post("/api/application/42/accept");

        var restored = JsonSerializer.Deserialize<ActionLink>(
            JsonSerializer.Serialize(original, this.options),
            this.options);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Deserialize_ResponseCarryingAnActionLink_RoundTrips()
    {
        var original = new ActionsEnvelope(ActionLink.Post("/api/application/42/accept"), null);

        var restored = JsonSerializer.Deserialize<ActionsEnvelope>(
            JsonSerializer.Serialize(original, this.options),
            this.options);

        Assert.Equal(original, restored);
    }

    [Theory]
    [InlineData("https://example.com/api/application/42/accept")]
    [InlineData("/\\example.com/api/application/42/accept")]
    public void Post_HrefNotRootRelative_ThrowsDomainException(string href)
    {
        Assert.Throws<DomainException>(() => ActionLink.Post(href));
    }

    [Fact]
    public void Get_HrefNotRootRelative_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ActionLink.Get("https://example.com/api/concert/7"));
    }

    [Theory]
    [InlineData("""{"method":"POST"}""")]
    [InlineData("""{"href":"/api/application/42/accept"}""")]
    [InlineData("""{}""")]
    public void Deserialize_MemberMissing_Throws(string json)
    {
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ActionLink>(json, this.options));
    }

    [Theory]
    [InlineData("""{"href":"https://example.com/x","method":"POST"}""")]
    [InlineData("""{"href":"/api//example.com/x","method":"POST"}""")]
    public void Deserialize_HrefThatWouldLeaveTheOrigin_Throws(string json)
    {
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ActionLink>(json, this.options));
    }

    [Fact]
    public void Equality_IsByHrefAndMethod()
    {
        Assert.Equal(ActionLink.Post("/api/venue/3"), ActionLink.Post("/api/venue/3"));
        Assert.NotEqual(ActionLink.Post("/api/venue/3"), ActionLink.Get("/api/venue/3"));
    }

    private sealed record ActionsEnvelope(ActionLink? Accept, ActionLink? Cancel);
}
