using Microsoft.AspNetCore.Http;

namespace Concertable.Shared.Imaging.Application;

public interface IImageService
{
    Task<string> UploadAsync(IFormFile file);
    Task DeleteAsync(string imageUrl);
    Task<string> ReplaceAsync(IFormFile newFile, string? oldImageUrl = null);
    Task<Stream> DownloadAsync(string imageUrl);
}
