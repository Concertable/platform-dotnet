namespace Concertable.Shared.Blob.Infrastructure;

public sealed class BlobStorageSettings
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
}
