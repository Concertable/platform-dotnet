using Concertable.Shared.Imaging.Application;
using Microsoft.AspNetCore.Http;

namespace Concertable.Testing.Integration.Mocks;

public sealed class MockImageService : IImageService
{
    public Task<string> UploadAsync(IFormFile file) =>
        Task.FromResult($"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

    public Task DeleteAsync(string imageUrl) => Task.CompletedTask;

    public Task<string> ReplaceAsync(IFormFile newFile, string? oldImageUrl = null) =>
        Task.FromResult($"{Guid.NewGuid()}{Path.GetExtension(newFile.FileName)}");

    public Task<Stream> DownloadAsync(string imageUrl) => Task.FromResult(Stream.Null);
}
