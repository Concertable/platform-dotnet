using System.Text.Json;
using Concertable.Shared.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Concertable.Shared.Api.UnitTests;

public sealed class ControllerBuilderExtensionsTests
{
    [Fact]
    public void AddApplicationJson_ConfiguresStrictCamelCaseStringEnums()
    {
        var services = new ServiceCollection();
        services.AddControllers().AddApplicationJson();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        var json = JsonSerializer.Serialize(new EnumPayload(TestStatus.PendingReview), options);

        Assert.Equal("{\"status\":\"pendingReview\"}", json);
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EnumPayload>("{\"status\":1}", options));
    }

    [Fact]
    public void AddApplicationJson_AppliesAdditionalConfiguration()
    {
        var services = new ServiceCollection();
        services.AddControllers().AddApplicationJson(options => options.WriteIndented = true);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        Assert.True(options.WriteIndented);
    }

    private sealed record EnumPayload(TestStatus Status);

    private enum TestStatus
    {
        PendingReview
    }
}
