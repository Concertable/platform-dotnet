using Concertable.DataAccess.Application.Specifications;
using Concertable.DataAccess.Infrastructure.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDataAccessSpecifications()
        {
            services.AddScoped(typeof(IUpcomingSpecification<>), typeof(UpcomingSpecification<>));
            services.AddScoped(typeof(IDateRangeSpecification<>), typeof(DateRangeSpecification<>));
            return services;
        }
    }
}
