using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Concertable.Shared.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Api.Extensions;

public static class ControllerBuilderExtensions
{
    public static IMvcBuilder AddApplicationJson(
        this IMvcBuilder builder,
        Action<JsonSerializerOptions>? configure = null)
        => builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
            configure?.Invoke(options.JsonSerializerOptions);
        });

    public static IMvcBuilder AddInternalControllers(this IMvcBuilder builder, Assembly assembly)
        => builder
            .AddApplicationPart(assembly)
            .ConfigureApplicationPartManager(apm =>
            {
                if (!apm.FeatureProviders.OfType<InternalControllerFeatureProvider>().Any())
                    apm.FeatureProviders.Add(new InternalControllerFeatureProvider());
            });
}
