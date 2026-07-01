using Concertable.Shared.Blob.Application;
using Concertable.Shared.Imaging.Application;
using Microsoft.AspNetCore.Http;

namespace Concertable.Shared.Imaging.Infrastructure;

internal sealed class ImageService : IImageService
{
    private readonly IBlobStorageService blobStorageService;

    public ImageService(IBlobStorageService blobStorageService)
    {
        this.blobStorageService = blobStorageService;
    }

    public async Task<string> ReplaceAsync(IFormFile newFile, string? oldImageUrl = null)
    {
        if (!string.IsNullOrEmpty(oldImageUrl))
        {
            await blobStorageService.DeleteAsync(oldImageUrl);
        }

        var extension = Path.GetExtension(newFile.FileName);
        var imageName = $"{Guid.NewGuid()}{extension}";

        using var stream = newFile.OpenReadStream();
        await blobStorageService.UploadAsync(stream, imageName);

        return imageName;
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var imageName = $"{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();
        await blobStorageService.UploadAsync(stream, imageName);

        return imageName;
    }

    public async Task DeleteAsync(string imageUrl)
    {
        if (!string.IsNullOrEmpty(imageUrl))
            await blobStorageService.DeleteAsync(imageUrl);
    }

    public Task<Stream> DownloadAsync(string imageUrl) =>
        blobStorageService.DownloadAsync(imageUrl);
}
