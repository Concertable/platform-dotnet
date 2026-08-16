using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

public static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<SqlServerServerResource> AddSqlServerContainer(
        this IDistributedApplicationBuilder builder,
        string dataVolumeName = "concertable-sql-data")
    {
        return builder.AddSqlServer("sql").WithDataVolume(dataVolumeName);
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

    public static IResourceBuilder<ProjectResource> AddSecrets(
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
