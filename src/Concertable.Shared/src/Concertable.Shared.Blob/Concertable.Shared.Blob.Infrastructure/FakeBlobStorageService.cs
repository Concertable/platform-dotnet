using Concertable.Shared.Blob.Application;

namespace Concertable.Shared.Blob.Infrastructure;

public sealed class FakeBlobStorageService : IBlobStorageService
{
    public Task UploadAsync(Stream content, string blobName) => Task.CompletedTask;
    public Task DeleteAsync(string blobName) => Task.CompletedTask;
    public Task<Stream> DownloadAsync(string blobName) => Task.FromResult(Stream.Null);
    public Task<bool> ExistsAsync(string blobName) => Task.FromResult(false);
}
