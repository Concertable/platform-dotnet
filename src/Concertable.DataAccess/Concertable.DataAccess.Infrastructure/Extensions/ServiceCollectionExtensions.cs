using Concertable.DataAccess.Application.Specifications;
using Concertable.DataAccess.Infrastructure.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccessSpecifications(this IServiceCollection services)
    {
        services.AddScoped(typeof(IUpcomingSpecification<>), typeof(UpcomingSpecification<>));
        services.AddScoped(typeof(IDateRangeSpecification<>), typeof(DateRangeSpecification<>));
        return services;
    }
}
