using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Concertable.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddSharedDefaults();
        builder.AddAzureAppConfiguration();

        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }));

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    private const string SharedDefaultsPrefix = "Concertable.ServiceDefaults.SharedDefaults.";

    private static IHostApplicationBuilder AddSharedDefaults(this IHostApplicationBuilder builder)
    {
        var shared = new ConfigurationBuilder();
        shared.AddJsonStream(OpenSharedDefault("appsettings.json"));

        var envStream = TryOpenSharedDefault($"appsettings.{builder.Environment.EnvironmentName}.json");
        if (envStream is not null)
            shared.AddJsonStream(envStream);

        // Chain a pre-built sub-config at index 0 (lowest precedence); don't insert stream sources direct —
        // ConfigurationManager re-reads one-shot manifest streams to EOF on every Sources mutation.
        builder.Configuration.Sources.Insert(0, new ChainedConfigurationSource
        {
            Configuration = shared.Build(),
            ShouldDisposeConfiguration = false
        });

        return builder;
    }

    private static Stream OpenSharedDefault(string fileName) =>
        TryOpenSharedDefault(fileName)
            ?? throw new InvalidOperationException(
                $"Embedded shared-defaults resource '{SharedDefaultsPrefix}{fileName}' was not found.");

    private static Stream? TryOpenSharedDefault(string fileName) =>
        typeof(Extensions).Assembly.GetManifestResourceStream(SharedDefaultsPrefix + fileName);

    /// <summary>
    /// Swaps Azure App Configuration in as the cloud config source — the non-secret tree (by environment
    /// label) plus Key Vault references resolved by managed identity. No-op locally: the endpoint is set
    /// only by the deployed app, so <c>aspire run</c> keeps <see cref="AddSharedDefaults"/> + appsettings.
    /// </summary>
    private static IHostApplicationBuilder AddAzureAppConfiguration(this IHostApplicationBuilder builder)
    {
        var endpoint = builder.Configuration.GetConnectionString("appconfig");
        if (string.IsNullOrWhiteSpace(endpoint))
            return builder;

        var credential = new DefaultAzureCredential();
        builder.Configuration.AddAzureAppConfiguration(options =>
            options.Connect(new Uri(endpoint), credential)
                   .Select(KeyFilter.Any, LabelFilter.Null)                       // unlabeled defaults, then
                   .Select(KeyFilter.Any, builder.Environment.EnvironmentName)    // this environment's overrides
                   .ConfigureKeyVault(kv => kv.SetCredential(credential)));        // resolve Key Vault references

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Azure.Messaging.ServiceBus");
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("E2E"))
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
