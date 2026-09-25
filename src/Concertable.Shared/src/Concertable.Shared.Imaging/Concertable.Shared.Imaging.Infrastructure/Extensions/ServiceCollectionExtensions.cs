using Concertable.Shared.Imaging.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Imaging.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharedImaging()
        {
            services.AddScoped<IImageService, ImageService>();
            return services;
        }
    }
}
