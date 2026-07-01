using System.Text.Json;
using Concertable.Shared.Geocoding.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Concertable.Shared.Geocoding.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedGeocoding(this IServiceCollection services)
    {
        services.AddRefitClient<IGoogleGeocodingApi>(new RefitSettings(
            new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })))
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/geocode"))
            .AddHttpMessageHandler(sp => new GoogleApiKeyHandler(sp.GetRequiredService<IConfiguration>()["GoogleApiKey"]!));

        services.AddScoped<IGeocodingClient, GoogleGeocodingClient>();

        return services;
    }
}
