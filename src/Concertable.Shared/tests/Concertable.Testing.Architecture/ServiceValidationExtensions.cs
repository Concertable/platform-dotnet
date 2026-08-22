using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Testing.Architecture;

public static class ServiceValidationExtensions
{
    extension(IServiceCollection services)
    {
        public void AddInvalidLifetimeGraph()
        {
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<SingletonDependency>();
        }
    }

    private sealed class ScopedDependency;

    private sealed class SingletonDependency
    {
        public SingletonDependency(ScopedDependency dependency)
        {
        }
    }
}
