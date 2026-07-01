using Concertable.Shared.Imaging.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Imaging.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedImaging(this IServiceCollection services)
    {
        services.AddScoped<IImageService, ImageService>();
        return services;
    }
}
