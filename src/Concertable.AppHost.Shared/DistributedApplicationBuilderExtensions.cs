using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

public static class DistributedApplicationBuilderExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ServiceContainerResource> AddContainerImage(
            string name,
            string image,
            string digest) =>
            builder.AddResource(new ServiceContainerResource(name))
                   .WithImage(image, digest);
    }

    /// <summary>Suffixes <paramref name="dataVolumeName"/> with a short hash of the AppHost assembly's own
    /// build output path, so every git worktree gets its own SQL data volume automatically. Without this,
    /// two worktrees running the same service's AppHost at once share one Docker volume — a fresh
    /// worktree's migrations collide with whatever schema an older worktree already applied to it.
    /// Hashes <see cref="AppContext.BaseDirectory"/> rather than <see cref="Directory.GetCurrentDirectory"/>
    /// — the process working directory varies with how the AppHost is launched (a developer's `dotnet run`
    /// from inside the AppHost folder versus a script that `cd`s to the repo root first), which would give
    /// the same worktree two different volumes depending on invocation style. The build output path is
    /// fixed per checkout regardless of invocation.</summary>
    public static IResourceBuilder<SqlServerServerResource> AddSqlServerContainer(
        this IDistributedApplicationBuilder builder,
        string dataVolumeName = "concertable-sql-data")
    {
        var checkoutSuffix = CheckoutSuffix();
        return builder.AddSqlServer("sql").WithDataVolume($"{dataVolumeName}-{checkoutSuffix}");
    }

    private static string CheckoutSuffix()
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory));
        return Convert.ToHexStringLower(hash)[..8];
    }

    public static IResourceBuilder<AzureServiceBusResource> AddServiceBus(
        this IDistributedApplicationBuilder builder) =>
        builder.AddAzureServiceBus("asb");

    public static AsbTopology Topology(this IResourceBuilder<AzureServiceBusResource> asb) => new(asb);

    public static (IResourceBuilder<AzureStorageResource> storage, IResourceBuilder<AzureBlobStorageResource> blobs) AddAzureStorage(this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddAzureStorage("storage")
                             .RunAsEmulator(c => c.WithDataVolume("concertable-azurite-data"));
        var blobs = storage.AddBlobs("blobs");
        return (storage, blobs);
    }

    public static IResourceBuilder<T> WithOptionalEnvironment<T>(
        this IResourceBuilder<T> resource,
        string name,
        string? value)
        where T : IResourceWithEnvironment
    {
        if (!string.IsNullOrEmpty(value))
            resource = resource.WithEnvironment(name, value);
        return resource;
    }

    public static IResourceBuilder<T> AddSecrets<T>(
        this IResourceBuilder<T> resource,
        IDistributedApplicationBuilder builder,
        params string[] keys)
        where T : IResourceWithEnvironment
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
