using Concertable.Seed.Shared;
using Concertable.Shared.Blob.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Blob.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedBlob(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BlobStorageSettings>(configuration.GetSection("BlobStorage"));

        var useRealBlob = configuration.GetSection("ExternalServices").GetValue<bool>("UseRealBlob");
        if (useRealBlob)
            services.AddScoped<IBlobStorageService, BlobStorageService>();
        else
            services.AddScoped<IBlobStorageService, FakeBlobStorageService>();

        return services;
    }

    public static IServiceCollection AddBlobDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, BlobDevSeeder>();
        return services;
    }
}
