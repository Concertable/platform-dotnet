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
    private static JsonSerializerOptions ApplicationOptions()
    {
        var services = new ServiceCollection();
        services.AddControllers().AddApplicationJson();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Fact]
    public void Post_SerializesHrefAsBareStringAndMethodAsVerb()
    {
        Assert.Equal(
            """{"href":"/api/application/42/accept","method":"POST"}""",
            JsonSerializer.Serialize(
                ActionLink.Post("/api/application/42/accept"),
                ApplicationOptions()));
    }

    [Fact]
    public void Get_SerializesHrefAsBareStringAndMethodAsVerb()
    {
        Assert.Equal(
            """{"href":"/api/concert/7/contract/pdf","method":"GET"}""",
            JsonSerializer.Serialize(
                ActionLink.Get("/api/concert/7/contract/pdf"),
                ApplicationOptions()));
    }

    [Fact]
    public void Factory_RejectsAnHrefThatIsNotRootRelative()
    {
        Assert.Throws<DomainException>(() => ActionLink.Post("https://example.com/api/application/42/accept"));
    }

    [Fact]
    public void Equality_IsByHrefAndMethod()
    {
        Assert.Equal(ActionLink.Post("/api/venue/3"), ActionLink.Post("/api/venue/3"));
        Assert.NotEqual(ActionLink.Post("/api/venue/3"), ActionLink.Get("/api/venue/3"));
    }
}
