using Azure.Storage.Blobs;
using Concertable.Shared.Blob.Application;
using Microsoft.Extensions.Options;

namespace Concertable.Shared.Blob.Infrastructure;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient blobServiceClient;
    private readonly string containerName;

    public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<BlobStorageSettings> options)
    {
        this.blobServiceClient = blobServiceClient;
        containerName = options.Value.ContainerName!;
    }

    public async Task UploadAsync(Stream stream, string blobName)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task DeleteAsync(string blobName)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<Stream> DownloadAsync(string blobName)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public async Task<bool> ExistsAsync(string blobName)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return (await blobClient.ExistsAsync()).Value;
    }
}
