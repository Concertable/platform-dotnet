using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Frontend.Hosting;

public static class AppHostExtensions
{
    public static IResourceBuilder<NodeAppResource> AddCustomerSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend,
        IResourceBuilder<IResourceWithServiceDiscovery> customerWeb,
        IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
        AddSpaSurface(builder, backend, auth, LocalSpaSurfaces.Customer)
            .WithReference(customerWeb)
            .WaitFor(customerWeb);

    public static IResourceBuilder<NodeAppResource> AddVenueSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend,
        IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
        AddSpaSurface(builder, backend, auth, LocalSpaSurfaces.Venue, "b2b");

    public static IResourceBuilder<NodeAppResource> AddArtistSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend,
        IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
        AddSpaSurface(builder, backend, auth, LocalSpaSurfaces.Artist, "b2b");

    public static IResourceBuilder<NodeAppResource> AddBusinessSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend,
        IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
        AddSpaSurface(builder, backend, auth, LocalSpaSurfaces.Business, "b2b");

    public static IResourceBuilder<NodeAppResource> AddAdminSpa(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend,
        IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
        AddSpaSurface(builder, backend, auth, LocalSpaSurfaces.Admin);

    private static IResourceBuilder<NodeAppResource> AddSpaSurface(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend,
        IResourceBuilder<IResourceWithServiceDiscovery> auth,
        LocalSpaSurface surface,
        params string[] tierSegments) =>
        builder.AddNpmApp(surface.ResourceName, RepoPath(builder, ["app", "web", .. tierSegments, surface.ResourceName]), "dev")
               .WithHttpsEndpoint(port: surface.HttpsPort, isProxied: false)
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

    public static void AddMobile<TAuth>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> api,
        IResourceBuilder<TAuth> auth,
        IResourceBuilder<IResourceWithServiceDiscovery> searchWeb,
        IResourceBuilder<IResourceWithServiceDiscovery> customerWeb,
        IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
        where TAuth : class, IResourceWithServiceDiscovery, IResourceWithEnvironment
    {
        if (!builder.Configuration.GetValue<bool>("RunMobile"))
            return;

        var tunnel = builder.AddDevTunnel("concertable-dev").WithAnonymousAccess();
        var lanIp = builder.Configuration["MobileLanIp"] ?? "localhost";

        tunnel.WithReference(auth, allowAnonymous: true);
        tunnel.WithReference(api, allowAnonymous: true);
        tunnel.WithReference(searchWeb, allowAnonymous: true);
        tunnel.WithReference(customerWeb, allowAnonymous: true);
        tunnel.WithReference(paymentWeb, allowAnonymous: true);
        auth.WithEnvironment(ctx =>
        {
            if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                ctx.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
        });

        AddMobileSurface(builder, api, auth, tunnel, lanIp, "customer", searchWeb, customerWeb, paymentWeb);
        AddMobileSurface(builder, api, auth, tunnel, lanIp, "b2b", searchWeb, customerWeb, paymentWeb);
    }

    public static void AddMobileB2B<TAuth>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> api,
        IResourceBuilder<TAuth> auth,
        IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
        where TAuth : class, IResourceWithServiceDiscovery, IResourceWithEnvironment
    {
        if (!builder.Configuration.GetValue<bool>("RunMobile"))
            return;

        var tunnel = builder.AddDevTunnel("b2b-dev").WithAnonymousAccess();
        var lanIp = builder.Configuration["MobileLanIp"] ?? "localhost";

        tunnel.WithReference(auth, allowAnonymous: true);
        tunnel.WithReference(api, allowAnonymous: true);
        tunnel.WithReference(paymentWeb, allowAnonymous: true);
        auth.WithEnvironment(ctx =>
        {
            if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                ctx.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
        });

        AddMobileSurface(builder, api, auth, tunnel, lanIp, "b2b", paymentWeb: paymentWeb);
    }

    public static void AddMobileCustomer<TAuth>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> customerWeb,
        IResourceBuilder<TAuth> auth,
        IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
        where TAuth : class, IResourceWithServiceDiscovery, IResourceWithEnvironment
    {
        if (!builder.Configuration.GetValue<bool>("RunMobile"))
            return;

        var tunnel = builder.AddDevTunnel("customer-dev").WithAnonymousAccess();
        var lanIp = builder.Configuration["MobileLanIp"] ?? "localhost";

        tunnel.WithReference(auth, allowAnonymous: true);
        tunnel.WithReference(customerWeb, allowAnonymous: true);
        tunnel.WithReference(paymentWeb, allowAnonymous: true);
        auth.WithEnvironment(ctx =>
        {
            if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                ctx.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
        });

        AddMobileSurface(builder, customerWeb, auth, tunnel, lanIp, "customer", customerWeb: customerWeb, paymentWeb: paymentWeb);
    }

    private static IResourceBuilder<NodeAppResource> AddMobileSurface(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> api,
        IResourceBuilder<IResourceWithServiceDiscovery> auth,
        IResourceBuilder<DevTunnelResource> tunnel,
        string lanIp,
        string surface,
        IResourceBuilder<IResourceWithServiceDiscovery>? searchWeb = null,
        IResourceBuilder<IResourceWithServiceDiscovery>? customerWeb = null,
        IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null)
    {
        var mobile = builder.AddNpmApp($"mobile-{surface}", RepoPath(builder, "app", "mobile", surface), "start:ci")
               .WithEnvironment("REACT_NATIVE_PACKAGER_HOSTNAME", lanIp)
               .WithReference(api, tunnel)
               .WithReference(auth, tunnel)
               .WaitFor(api)
               .WaitFor(tunnel)
               .WithEnvironment(ctx =>
               {
                   SetServiceUrl(ctx, api.Resource.Name, "EXPO_PUBLIC_API_URL");
                   if (ctx.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                       ctx.EnvironmentVariables["EXPO_PUBLIC_AUTH_AUTHORITY"] = authUrl;
               });

        WithServiceUrl(mobile, tunnel, searchWeb, "EXPO_PUBLIC_SEARCH_API_URL");
        WithServiceUrl(mobile, tunnel, customerWeb, "EXPO_PUBLIC_CUSTOMER_API_URL");
        WithServiceUrl(mobile, tunnel, paymentWeb, "EXPO_PUBLIC_PAYMENT_API_URL");

        WithClearMetroCacheCommand(builder, mobile, surface);

        return mobile;
    }

    private static void WithServiceUrl(
        IResourceBuilder<NodeAppResource> mobile,
        IResourceBuilder<DevTunnelResource> tunnel,
        IResourceBuilder<IResourceWithServiceDiscovery>? service,
        string envName)
    {
        if (service is null)
            return;

        mobile.WithReference(service, tunnel)
              .WithEnvironment(ctx => SetServiceUrl(ctx, service.Resource.Name, envName));
    }

    private static void SetServiceUrl(EnvironmentCallbackContext ctx, string resourceName, string envName)
    {
        if (ctx.EnvironmentVariables.TryGetValue($"services__{resourceName}__https__0", out var url)
            || ctx.EnvironmentVariables.TryGetValue($"services__{resourceName.Replace('-', '_')}__https__0", out url))
            ctx.EnvironmentVariables[envName] = url;
    }

    private static void WithClearMetroCacheCommand(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<NodeAppResource> mobile,
        string surface)
    {
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
    }
}
