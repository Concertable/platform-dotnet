namespace Concertable.Shared.Blob.Application;

public interface IBlobStorageService
{
    Task UploadAsync(Stream content, string blobName);
    Task DeleteAsync(string blobName);
    Task<Stream> DownloadAsync(string blobName);
    Task<bool> ExistsAsync(string blobName);
}
