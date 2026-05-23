using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.DevTunnels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<SqlServerServerResource> AddSqlServerContainer(
        this IDistributedApplicationBuilder builder,
        string dataVolumeName = "concertable-sql-data")
    {
        return builder.AddSqlServer("sql").WithDataVolume(dataVolumeName);
    }

    public static IResourceBuilder<AzureServiceBusResource> AddServiceBus(
        this IDistributedApplicationBuilder builder,
        bool b2b = false,
        bool customer = false,
        bool search = false,
        bool payment = false)
    {
        var asb = builder.AddAzureServiceBus("asb");

        // Published by B2B, consumed by search + customer
        if (b2b || search || customer)
        {
            var concertChanged = asb.AddServiceBusTopic("event-concertchangedevent");
            if (search) concertChanged.AddServiceBusSubscription("search-concert-changed", "concertable-search");
            if (customer) concertChanged.AddServiceBusSubscription("customer-concert-changed", "concertable-customer");
        }

        // Published by customer, consumed by b2b
        if (customer || b2b)
        {
            var reviewSubmitted = asb.AddServiceBusTopic("event-reviewsubmittedevent");
            if (b2b) reviewSubmitted.AddServiceBusSubscription("b2b-review-submitted", "concertable-b2b");
        }

        // Published by B2B, consumed by search + customer
        if (b2b || search || customer)
        {
            var artistChanged = asb.AddServiceBusTopic("event-artistchangedevent");
            if (search) artistChanged.AddServiceBusSubscription("search-artist-changed", "concertable-search");
            if (customer) artistChanged.AddServiceBusSubscription("customer-artist-changed", "concertable-customer");

            var venueChanged = asb.AddServiceBusTopic("event-venuechangedevent");
            if (search) venueChanged.AddServiceBusSubscription("search-venue-changed", "concertable-search");
            if (customer) venueChanged.AddServiceBusSubscription("customer-venue-changed", "concertable-customer");
        }

        // Published by customer (after review), consumed by search + customer (own projections)
        if (customer || search)
        {
            var artistRating = asb.AddServiceBusTopic("event-artistratingupdatedevent");
            if (search) artistRating.AddServiceBusSubscription("search-artist-rating-updated", "concertable-search");
            if (customer) artistRating.AddServiceBusSubscription("customer-artist-rating-updated", "concertable-customer");

            var venueRating = asb.AddServiceBusTopic("event-venueratingupdatedevent");
            if (search) venueRating.AddServiceBusSubscription("search-venue-rating-updated", "concertable-search");
            if (customer) venueRating.AddServiceBusSubscription("customer-venue-rating-updated", "concertable-customer");

            var concertRating = asb.AddServiceBusTopic("event-concertratingupdatedevent");
            if (search) concertRating.AddServiceBusSubscription("search-concert-rating-updated", "concertable-search");
            if (customer) concertRating.AddServiceBusSubscription("customer-concert-rating-updated", "concertable-customer");
        }

        // Published by auth, consumed by b2b + customer + payment
        var credentialRegistered = asb.AddServiceBusTopic("event-credentialregisteredevent");
        if (b2b) credentialRegistered.AddServiceBusSubscription("b2b-credential-registered", "concertable-b2b");
        if (customer) credentialRegistered.AddServiceBusSubscription("customer-credential-registered", "concertable-customer");
        if (payment) credentialRegistered.AddServiceBusSubscription("payment-credential-registered", "concertable-payment");

        // Published by payment, consumed by b2b + customer + payment
        var paymentSucceeded = asb.AddServiceBusTopic("event-paymentsucceededevent");
        if (b2b) paymentSucceeded.AddServiceBusSubscription("b2b-payment-succeeded", "concertable-b2b");
        if (customer) paymentSucceeded.AddServiceBusSubscription("customer-payment-succeeded", "concertable-customer");
        if (payment) paymentSucceeded.AddServiceBusSubscription("payment-payment-succeeded", "concertable-payment");

        var paymentFailed = asb.AddServiceBusTopic("event-paymentfailedevent");
        if (b2b) paymentFailed.AddServiceBusSubscription("b2b-payment-failed", "concertable-b2b");
        if (customer) paymentFailed.AddServiceBusSubscription("customer-payment-failed", "concertable-customer");
        if (payment) paymentFailed.AddServiceBusSubscription("payment-payment-failed", "concertable-payment");

        return asb.RunAsEmulator();
    }

    public static (IResourceBuilder<AzureStorageResource> storage, IResourceBuilder<AzureBlobStorageResource> blobs) AddAzureStorage(this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddAzureStorage("storage")
                             .RunAsEmulator(c => c.WithDataVolume("concertable-azurite-data"));
        var blobs = storage.AddBlobs("blobs");
        return (storage, blobs);
    }

    public static IResourceBuilder<ProjectResource> AddAuth<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> authDb,
        IResourceBuilder<SqlServerDatabaseResource> b2bDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        var auth = builder.AddProject<TProject>("auth")
                          .WithReference(authDb)
                          .WaitFor(authDb)
                          .WithReference(b2bDb)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .WithEnvironment(ctx =>
                          {
                              if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                                  ctx.EnvironmentVariables["Auth__Authority"] = authUrl;
                          })
                          .AddSecrets(builder, "ServiceAuth:B2BClientSecret", "ServiceAuth:CustomerClientSecret", "ServiceAuth:AuthClientSecret");

        var lanIp = builder.Configuration["MobileLanIp"];
        if (!string.IsNullOrEmpty(lanIp))
        {
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
        }

        return auth;
    }

    public static IResourceBuilder<ProjectResource> AddApi<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> sql,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<AzureStorageResource> storage,
        IResourceBuilder<AzureBlobStorageResource> blobs,
        IResourceBuilder<AzureServiceBusResource> asb,
        IResourceBuilder<ProjectResource> paymentWeb)
        where TProject : IProjectMetadata, new()
    {
        var b2bSecret = builder.Configuration["ServiceAuth:B2BClientSecret"];
        return builder.AddProject<TProject>("api")
                      .WithReference(sql)
                      .WaitFor(sql)
                      .WithReference(auth)
                      .WaitFor(auth)
                      .WithReference(blobs)
                      .WaitFor(storage)
                      .WithReference(asb)
                      .WaitFor(asb)
                      .WithReference(paymentWeb)
                      .WaitFor(paymentWeb)
                      .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                      .WithEnvironment("ServiceAuth__ClientId", "concertable-b2b")
                      .WithEnvironment("ServiceAuth__ClientSecret", b2bSecret ?? "")
                      .AddSecrets(builder, "Stripe:SecretKey");
    }

    public static IResourceBuilder<AzureFunctionsProjectResource> AddWorkers<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> sql)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddAzureFunctionsProject<TProject>("workers")
                      .WithReference(sql)
                      .WaitFor(sql);
    }

    public static IResourceBuilder<ProjectResource> AddCustomerWeb<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<SqlServerDatabaseResource> customerDb,
        IResourceBuilder<AzureServiceBusResource> asb,
        IResourceBuilder<ProjectResource> paymentWeb)
        where TProject : IProjectMetadata, new()
    {
        var customerSecret = builder.Configuration["ServiceAuth:CustomerClientSecret"];
        return builder.AddProject<TProject>("customer-web")
                      .WithReference(auth)
                      .WaitFor(auth)
                      .WithReference(customerDb)
                      .WaitFor(customerDb)
                      .WithReference(asb)
                      .WaitFor(asb)
                      .WithReference(paymentWeb)
                      .WaitFor(paymentWeb)
                      .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                      .WithEnvironment("ServiceAuth__ClientId", "concertable-customer")
                      .WithEnvironment("ServiceAuth__ClientSecret", customerSecret ?? "");
    }

    public static IResourceBuilder<ProjectResource> AddSearchWeb<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<SqlServerDatabaseResource> searchDb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>("search-web")
                      .WithReference(auth)
                      .WaitFor(auth)
                      .WithReference(searchDb)
                      .WaitFor(searchDb)
                      .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));
    }

    public static IResourceBuilder<ProjectResource> AddSearchWorkers<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> searchDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>("search-workers")
                      .WithReference(searchDb)
                      .WaitFor(searchDb)
                      .WithReference(asb)
                      .WaitFor(asb);
    }

    public static IResourceBuilder<ProjectResource> AddPaymentWeb<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<SqlServerDatabaseResource> paymentDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>("payment-web")
                      .WithReference(paymentDb)
                      .WaitFor(paymentDb)
                      .WithReference(auth)
                      .WaitFor(auth)
                      .WithReference(asb)
                      .WaitFor(asb)
                      .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                      .AddSecrets(builder, "Stripe:SecretKey", "Stripe:WebhookSecret", "ExternalServices:UseRealStripe");
    }

    public static IResourceBuilder<ProjectResource> AddPaymentWorkers<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> paymentDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>("payment-workers")
                      .WithReference(paymentDb)
                      .WaitFor(paymentDb)
                      .WithReference(asb)
                      .WaitFor(asb)
                      .AddSecrets(builder, "Stripe:SecretKey", "ExternalServices:UseRealStripe");
    }

    public static IResourceBuilder<NodeAppResource> AddCustomerSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> backend,
        IResourceBuilder<ProjectResource> auth) =>
        AddSpaSurface(builder, backend, auth, "customer", 5174);

    public static IResourceBuilder<NodeAppResource> AddVenueSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> backend,
        IResourceBuilder<ProjectResource> auth) =>
        AddSpaSurface(builder, backend, auth, "venue", 5175);

    public static IResourceBuilder<NodeAppResource> AddArtistSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> backend,
        IResourceBuilder<ProjectResource> auth) =>
        AddSpaSurface(builder, backend, auth, "artist", 5176);

    public static IResourceBuilder<NodeAppResource> AddBusinessSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> backend,
        IResourceBuilder<ProjectResource> auth) =>
        AddSpaSurface(builder, backend, auth, "business", 5177);

    private static IResourceBuilder<NodeAppResource> AddSpaSurface(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> backend,
        IResourceBuilder<ProjectResource> auth,
        string surface,
        int port) =>
        builder.AddNpmApp(surface, RepoPath(builder, "app", "web", surface), "dev")
               .WithHttpsEndpoint(port: port, isProxied: false)
               .WithReference(backend)
               .WithReference(auth)
               .WaitFor(backend);

    private static string RepoPath(IDistributedApplicationBuilder builder, params string[] segments)
    {
        var root = AncestorsAndSelf(builder.AppHostDirectory)
            .FirstOrDefault(d => Directory.Exists(Path.Combine(d, "app")))
            ?? throw new InvalidOperationException(
                $"Could not locate repo root (no 'app' directory found walking up from '{builder.AppHostDirectory}'.");

        return Path.Combine([root, .. segments]);
    }

    private static IEnumerable<string> AncestorsAndSelf(string path)
    {
        for (var dir = new DirectoryInfo(path); dir is not null; dir = dir.Parent)
            yield return dir.FullName;
    }

    public static void AddMobile(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api,
        IResourceBuilder<ProjectResource> auth)
    {
        if (!builder.Configuration.GetValue<bool>("RunMobile"))
            return;

        var tunnel = builder.AddDevTunnel("concertable-dev").WithAnonymousAccess();
        var lanIp = builder.Configuration["MobileLanIp"] ?? "localhost";

        tunnel.WithReference(auth, allowAnonymous: true);
        tunnel.WithReference(api, allowAnonymous: true);
        auth.WithEnvironment(ctx =>
        {
            if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                ctx.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
        });

        AddMobileSurface(builder, api, auth, tunnel, lanIp, "customer");
        AddMobileSurface(builder, api, auth, tunnel, lanIp, "business");
    }

    public static void AddMobileB2B(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api,
        IResourceBuilder<ProjectResource> auth)
    {
        if (!builder.Configuration.GetValue<bool>("RunMobile"))
            return;

        var tunnel = builder.AddDevTunnel("b2b-dev").WithAnonymousAccess();
        var lanIp = builder.Configuration["MobileLanIp"] ?? "localhost";

        tunnel.WithReference(auth, allowAnonymous: true);
        tunnel.WithReference(api, allowAnonymous: true);
        auth.WithEnvironment(ctx =>
        {
            if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                ctx.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
        });

        AddMobileSurface(builder, api, auth, tunnel, lanIp, "business");
    }

    public static void AddMobileCustomer(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> customerWeb,
        IResourceBuilder<ProjectResource> auth)
    {
        if (!builder.Configuration.GetValue<bool>("RunMobile"))
            return;

        var tunnel = builder.AddDevTunnel("customer-dev").WithAnonymousAccess();
        var lanIp = builder.Configuration["MobileLanIp"] ?? "localhost";

        tunnel.WithReference(auth, allowAnonymous: true);
        tunnel.WithReference(customerWeb, allowAnonymous: true);
        auth.WithEnvironment(ctx =>
        {
            if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                ctx.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
        });

        AddMobileSurface(builder, customerWeb, auth, tunnel, lanIp, "customer");
    }

    private static IResourceBuilder<NodeAppResource> AddMobileSurface(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<DevTunnelResource> tunnel,
        string lanIp,
        string surface)
    {
        var apiEnvKey = $"services__{api.Resource.Name.Replace('-', '_')}__https__0";
        var mobile = builder.AddNpmApp($"mobile-{surface}", RepoPath(builder, "app", "mobile", surface), "start:ci")
               .WithEnvironment("REACT_NATIVE_PACKAGER_HOSTNAME", lanIp)
               .WithReference(api, tunnel)
               .WithReference(auth, tunnel)
               .WaitFor(api)
               .WaitFor(tunnel)
               .WithEnvironment(ctx =>
               {
                   if (ctx.EnvironmentVariables.TryGetValue(apiEnvKey, out var apiUrl))
                       ctx.EnvironmentVariables["EXPO_PUBLIC_API_URL"] = apiUrl;
                   if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                       ctx.EnvironmentVariables["EXPO_PUBLIC_AUTH_AUTHORITY"] = authUrl;
               });

        mobile.WithCommand(
            name: "clear-metro-cache",
            displayName: "Clear Metro Cache",
            executeCommand: async ctx =>
            {
                var mobileDir = RepoPath(builder, "app", "mobile", surface);
                File.WriteAllText(Path.Combine(mobileDir, ".metro-clear"), "");

                var commands = ctx.ServiceProvider.GetRequiredService<ResourceCommandService>();
                await commands.ExecuteCommandAsync(mobile.Resource, KnownResourceCommands.RestartCommand, ctx.CancellationToken);
                return new ExecuteCommandResult { Success = true };
            },
            commandOptions: new CommandOptions { IconName = "ArrowCounterclockwise" });

        return mobile;
    }

    public static void AddStripeCli(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> api)
    {
        var secretKey = builder.Configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            return;

        if (builder.ExecutionContext.IsRunMode)
        {
            builder.AddExecutable(
                name: "stripe-cli",
                command: "stripe",
                workingDirectory: ".",
                "listen", "--api-key", secretKey,
                "--forward-to", "https://localhost:7086/api/webhook",
                "--skip-verify");
            return;
        }

        var webhookSecret = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stripeCli = builder.AddContainer("stripe-cli", "stripe/stripe-cli")
               .WithVolume("stripe-cli-config", "/root/.config/stripe")
               .WithArgs("listen", "--api-key", secretKey, "--forward-to",
                   ReferenceExpression.Create($"{api.GetEndpoint("http")}/api/webhook"));

        builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            var logs = evt.Services.GetRequiredService<ResourceLoggerService>();
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var line in logs.WatchLinesAsync(stripeCli.Resource, ct))
                    {
                        var match = Regex.Match(line.Content, @"whsec_\w+");
                        if (match.Success)
                        {
                            webhookSecret.TrySetResult(match.Value);
                            return;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    webhookSecret.TrySetCanceled(ct);
                }
            }, ct);
            return Task.CompletedTask;
        });

        api.WithEnvironment(async ctx =>
        {
            ctx.EnvironmentVariables["Stripe__WebhookSecret"] =
                await webhookSecret.Task.WaitAsync(TimeSpan.FromSeconds(60));
        });
    }

    private static async IAsyncEnumerable<LogLine> WatchLinesAsync(
        this ResourceLoggerService logs,
        IResource resource,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var batch in logs.WatchAsync(resource).WithCancellation(ct))
            foreach (var line in batch)
                yield return line;
    }

    private static IResourceBuilder<ProjectResource> AddSecrets(
        this IResourceBuilder<ProjectResource> resource,
        IDistributedApplicationBuilder builder,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = builder.Configuration[key];
            if (!string.IsNullOrEmpty(value))
                resource = resource.WithEnvironment(key.Replace(":", "__"), value);
        }
        return resource;
    }
}
