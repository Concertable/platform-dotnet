using Concertable.Shared.Email.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Email.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var useRealEmail = configuration.GetSection("ExternalServices").GetValue<bool>("UseRealEmail");
        if (useRealEmail)
            services.AddScoped<IEmailSender, EmailSender>();
        else
        {
            services.AddHttpClient();
            services.AddScoped<IEmailSender, FakeEmailSender>();
        }

        return services;
    }
}
