using System.Reflection;
using Concertable.Shared.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Api.Extensions;

public static class ControllerBuilderExtensions
{
    public static IMvcBuilder AddInternalControllers(this IMvcBuilder builder, Assembly assembly)
        => builder
            .AddApplicationPart(assembly)
            .ConfigureApplicationPartManager(apm =>
            {
                if (!apm.FeatureProviders.OfType<InternalControllerFeatureProvider>().Any())
                    apm.FeatureProviders.Add(new InternalControllerFeatureProvider());
            });
}
