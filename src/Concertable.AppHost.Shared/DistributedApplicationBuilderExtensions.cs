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
            string digest)
        {
            var sha256 = digest.StartsWith("sha256:", StringComparison.Ordinal)
                ? digest["sha256:".Length..]
                : digest;

            return builder.AddResource(new ServiceContainerResource(name))
                          .WithImage(image)
                          .WithImageSHA256(sha256);
        }

        /// <summary>Adds the Postgres container, on a data volume unique to this git worktree.</summary>
        public IResourceBuilder<PostgresServerResource> AddPostgresContainer(
            string dataVolumeName = "concertable-postgres-data") =>
            builder.AddPostgres("postgres").WithDataVolume(CheckoutVolume(dataVolumeName));

        /// <summary>Adds the SQL Server container, on a data volume unique to this git worktree. Services
        /// keep one while they still host the Auth container, which runs on SQL Server until its own
        /// cut-over.</summary>
        public IResourceBuilder<SqlServerServerResource> AddSqlServerContainer(
            string dataVolumeName = "concertable-sql-data") =>
            builder.AddSqlServer("sql").WithDataVolume(CheckoutVolume(dataVolumeName));

        public IResourceBuilder<AzureServiceBusResource> AddServiceBus() =>
            builder.AddAzureServiceBus("asb");

        public (IResourceBuilder<AzureStorageResource> storage, IResourceBuilder<AzureBlobStorageResource> blobs) AddAzureStorage()
        {
            var storage = builder.AddAzureStorage("storage")
                                 .RunAsEmulator(c => c.WithDataVolume("concertable-azurite-data"));
            var blobs = storage.AddBlobs("blobs");
            return (storage, blobs);
        }
    }

    extension(IResourceBuilder<PostgresServerResource> postgres)
    {
        public IResourceBuilder<PostgresServerResource> WithPostGis() =>
            postgres.WithImage(PostgisImage, PostgisTag);
    }

    extension(IResourceBuilder<AzureServiceBusResource> asb)
    {
        public AsbTopology Topology() => new(asb);
    }

    extension<T>(IResourceBuilder<T> resource)
        where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithOptionalEnvironment(string name, string? value) =>
            string.IsNullOrEmpty(value) ? resource : resource.WithEnvironment(name, value);

        public IResourceBuilder<T> AddSecrets(
            IDistributedApplicationBuilder builder,
            params string[] keys)
        {
            var configured = resource;
            foreach (var key in keys)
            {
                var value = builder.Configuration[key];
                if (!string.IsNullOrEmpty(value))
                    configured = configured.WithEnvironment(key.Replace(":", "__"), value);
            }
            return configured;
        }
    }

    internal const string PostgisImage = "postgis/postgis";
    internal const string PostgisTag = "17-3.5";

    /// <summary>Suffixes <paramref name="dataVolumeName"/> with a short hash of the AppHost assembly's own
    /// build output path, so every git worktree gets its own database volume automatically. Without this,
    /// two worktrees running the same service's AppHost at once share one Docker volume — a fresh
    /// worktree's migrations collide with whatever schema an older worktree already applied to it.
    /// Hashes <see cref="AppContext.BaseDirectory"/> rather than <see cref="Directory.GetCurrentDirectory"/>
    /// — the process working directory varies with how the AppHost is launched (a developer's `dotnet run`
    /// from inside the AppHost folder versus a script that `cd`s to the repo root first), which would give
    /// the same worktree two different volumes depending on invocation style. The build output path is
    /// fixed per checkout regardless of invocation.</summary>
    private static string CheckoutVolume(string dataVolumeName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory));
        return $"{dataVolumeName}-{Convert.ToHexStringLower(hash)[..8]}";
    }
}
