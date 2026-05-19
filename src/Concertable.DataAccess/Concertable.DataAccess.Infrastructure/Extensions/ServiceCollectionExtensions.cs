using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.DataAccess.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReadDbContext>(opt =>
            opt.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOpt => sqlOpt.UseNetTopologySuite()));
        services.AddScoped<IReadDbContext, ReadDbContext>();

        services.AddScoped(typeof(IUpcomingSpecification<>), typeof(UpcomingSpecification<>));
        services.AddScoped(typeof(IDateRangeSpecification<>), typeof(DateRangeSpecification<>));
        return services;
    }
}
