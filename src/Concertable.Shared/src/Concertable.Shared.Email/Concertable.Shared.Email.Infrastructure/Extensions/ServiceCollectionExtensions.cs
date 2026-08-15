using Concertable.Messaging.Contracts;
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
        {
            services.AddScoped<SmtpEmailTransport>();
            services.AddScoped<IEmailTransport>(sp => sp.GetRequiredService<SmtpEmailTransport>());
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailTransport>());
        }
        else
        {
            services.AddHttpClient();
            services.AddScoped<FakeEmailTransport>();
            services.AddScoped<IEmailTransport>(sp => sp.GetRequiredService<FakeEmailTransport>());
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<FakeEmailTransport>());
        }

        services.AddScoped<IIntegrationCommandHandler<SendEmailCommand>, SendEmailCommandHandler>();
        services.AddScoped<IIntegrationCommandHandler<SendVerificationEmailCommand>, SendVerificationEmailCommandHandler>();

        services.AddSingleton<IEmailRenderer, EmailRenderer>();

        return services;
    }

    public static IServiceCollection UseOutboxEmailSender(this IServiceCollection services)
    {
        services.AddScoped<IEmailSender, OutboxEmailSender>();
        return services;
    }
}
