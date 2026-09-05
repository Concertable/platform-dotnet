using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Frontend.Hosting;

public static class FrontendResourcesExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<NodeAppResource> AddSpaSurface(
            SpaSurface surface,
            IReadOnlyList<string[]> workspacePathCandidates,
            IResourceBuilder<IResourceWithServiceDiscovery> backend,
            IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
            builder.AddNpmApp(
                       surface.ResourceName,
                       FrontendWorkspacePathResolver.Resolve(builder.AppHostDirectory, workspacePathCandidates),
                       "dev")
                   .WithHttpsEndpoint(port: surface.HttpsPort, isProxied: false)
                   .WithReference(backend)
                   .WithReference(auth)
                   .WaitFor(backend);

        public IResourceBuilder<DevTunnelResource> AddMobileTunnel(
            string name,
            params IResourceBuilder<IResourceWithServiceDiscovery>[] services)
        {
            var tunnel = builder.AddDevTunnel(name).WithAnonymousAccess();
            foreach (var service in services)
                tunnel.WithReference(service, allowAnonymous: true);
            return tunnel;
        }

        public IResourceBuilder<NodeAppResource> AddMobileSurface(
            string resourceName,
            IReadOnlyList<string[]> workspacePathCandidates,
            IResourceBuilder<IResourceWithServiceDiscovery> backend,
            IResourceBuilder<DevTunnelResource> tunnel,
            params MobileServiceEndpoint[] endpoints)
        {
            var directory = FrontendWorkspacePathResolver.Resolve(
                builder.AppHostDirectory,
                workspacePathCandidates);
            var mobile = builder.AddNpmApp(resourceName, directory, "start:ci")
                .WithEnvironment("REACT_NATIVE_PACKAGER_HOSTNAME", builder.Configuration["MobileLanIp"] ?? "localhost")
                .WaitFor(backend)
                .WaitFor(tunnel);

            foreach (var endpoint in endpoints)
                mobile.WithMobileServiceEndpoint(tunnel, endpoint);

            mobile.WithCommand(
                name: "clear-metro-cache",
                displayName: "Clear Metro Cache",
                executeCommand: async context =>
                {
                    File.WriteAllText(Path.Combine(directory, ".metro-clear"), string.Empty);
                    var commands = context.ServiceProvider.GetRequiredService<ResourceCommandService>();
                    await commands.ExecuteCommandAsync(mobile.Resource, KnownResourceCommands.RestartCommand, context.CancellationToken);
                    return new ExecuteCommandResult { Success = true };
                },
                commandOptions: new CommandOptions { IconName = "ArrowCounterclockwise" });
            return mobile;
        }
    }

    extension(IResourceBuilder<NodeAppResource> mobile)
    {
        public IResourceBuilder<NodeAppResource> WithMobileServiceEndpoint(
            IResourceBuilder<DevTunnelResource> tunnel,
            MobileServiceEndpoint endpoint) =>
            mobile.WithReference(endpoint.Service, tunnel)
                  .WithEnvironment(context => SetServiceUrl(context, endpoint.Service.Resource.Name, endpoint.EnvironmentVariable));
    }

    private static void SetServiceUrl(EnvironmentCallbackContext context, string resourceName, string environmentVariable)
    {
        foreach (var endpointName in new[] { "https", "http" })
        {
            if (context.EnvironmentVariables.TryGetValue($"services__{resourceName}__{endpointName}__0", out var url)
                || context.EnvironmentVariables.TryGetValue(
                    $"services__{resourceName.Replace('-', '_')}__{endpointName}__0", out url))
            {
                context.EnvironmentVariables[environmentVariable] = url;
                return;
            }
        }
    }
}
