using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;

namespace Concertable.E2ETests;

internal static class DistributedApplicationBuilderExtensions
{
    internal const string B2BServiceAuthSecret = "concertable-e2e-b2b-service-secret";
    internal const string CustomerServiceAuthSecret = "concertable-e2e-customer-service-secret";
    private const string AuthServiceAuthSecret = "concertable-e2e-auth-service-secret";

    internal static void PinPaymentWeb(
        this IDistributedApplicationTestingBuilder builder,
        string paymentBaseUrl,
        string authBaseUrl,
        StripeE2ERun stripeRun)
    {
        var paymentWeb = builder.Resources
            .OfType<ProjectResource>()
            .Single(r => r.Name == AppHostConstants.ResourceNames.PaymentWeb);

        var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];

        paymentWeb.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "E2E";
            context.EnvironmentVariables["ASPNETCORE_URLS"] = paymentBaseUrl;
            context.EnvironmentVariables["Auth__Authority"] = authBaseUrl;
            AddStripeRunConfiguration(context, stripeRun);
            if (!string.IsNullOrEmpty(stripeSecretKey))
                context.EnvironmentVariables["Stripe__SecretKey"] = stripeSecretKey;
        }));
    }

    internal static void PinAuthService(
        this IDistributedApplicationTestingBuilder builder,
        string authBaseUrl)
    {
        var auth = builder.Resources
            .OfType<ProjectResource>()
            .Single(r => r.Name == AuthConstants.Resource);

        auth.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "E2E";
            context.EnvironmentVariables["ASPNETCORE_URLS"] = authBaseUrl;
            context.EnvironmentVariables["Auth__Authority"] = authBaseUrl;
            context.EnvironmentVariables["ServiceAuth__B2BClientSecret"] = B2BServiceAuthSecret;
            context.EnvironmentVariables["ServiceAuth__CustomerClientSecret"] = CustomerServiceAuthSecret;
            context.EnvironmentVariables["ServiceAuth__AuthClientSecret"] = AuthServiceAuthSecret;
        }));
    }

    internal static void PinPaymentWorkers(
        this IDistributedApplicationTestingBuilder builder,
        StripeE2ERun stripeRun)
    {
        var paymentWorkers = builder.Resources
            .OfType<ProjectResource>()
            .Single(r => r.Name == AppHostConstants.ResourceNames.PaymentWorkers);

        paymentWorkers.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "E2E";
            AddStripeRunConfiguration(context, stripeRun);
        }));
    }

    private static void AddStripeRunConfiguration(
        EnvironmentCallbackContext context,
        StripeE2ERun stripeRun)
    {
        foreach (var (key, value) in stripeRun.GetConfiguration())
            context.EnvironmentVariables[key.Replace(":", "__")] = value;
    }

    internal static void PinStripeCli(
        this IDistributedApplicationTestingBuilder builder,
        string paymentBaseUrl)
    {
        var stripeCli = builder.Resources
            .SingleOrDefault(r => r.Name == AppHostConstants.ResourceNames.StripeCli);

        if (stripeCli is null) return;

        var apiKey = builder.Configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
        var forwardTo = $"{paymentBaseUrl}/api/Webhook";

        foreach (var annotation in stripeCli.Annotations.OfType<CommandLineArgsCallbackAnnotation>().ToList())
            stripeCli.Annotations.Remove(annotation);

        stripeCli.Annotations.Add(new CommandLineArgsCallbackAnnotation(ctx =>
        {
            ctx.Args.Add("listen");
            ctx.Args.Add("--skip-verify");
            ctx.Args.Add("--api-key");
            ctx.Args.Add(apiKey);
            ctx.Args.Add("--forward-to");
            ctx.Args.Add(forwardTo);
            return Task.CompletedTask;
        }));
    }

    internal static void AddEphemeralSql(
        this IDistributedApplicationTestingBuilder builder)
    {
        var sql = builder.Resources
            .OfType<SqlServerServerResource>()
            .Single();

        var volume = sql.Annotations
            .OfType<ContainerMountAnnotation>()
            .FirstOrDefault();

        if (volume is not null)
            sql.Annotations.Remove(volume);
    }
}
