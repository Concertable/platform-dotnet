using Concertable.Seed.Shared;
using Concertable.Shared.Blob.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Blob.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharedBlob(IConfiguration configuration)
        {
            services.Configure<BlobStorageSettings>(configuration.GetSection(BlobStorageSettings.SectionName));

            var useRealBlob = configuration.GetSection("ExternalServices").GetValue<bool>("UseRealBlob");
            if (useRealBlob)
                services.AddScoped<IBlobStorageService, BlobStorageService>();
            else
                services.AddScoped<IBlobStorageService, FakeBlobStorageService>();

            return services;
        }

        public IServiceCollection AddBlobDevSeeder()
        {
            services.AddScoped<IDevSeeder, BlobDevSeeder>();
            return services;
        }
    }
}
