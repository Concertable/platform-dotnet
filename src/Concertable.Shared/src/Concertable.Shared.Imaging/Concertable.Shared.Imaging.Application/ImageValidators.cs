using FluentValidation;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace Concertable.Shared.Imaging.Application;

public class ImageValidator : AbstractValidator<IFormFile>
{
    private static readonly string[] AllowedMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/webp"
    ];

    private const long MaxFileSize = 5 * 1024 * 1024;

    public ImageValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedMimeTypes.Contains(ct))
            .WithMessage("Image must be a valid format (JPEG, PNG, GIF, BMP, WEBP).");

        RuleFor(x => x.Length)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("Image file exceeds the maximum size of 5MB.");

        RuleFor(x => x)
            .Custom((file, ctx) =>
            {
                Image image;
                try
                {
                    image = Image.Load(file.OpenReadStream());
                }
                catch
                {
                    ctx.AddFailure("Must be a valid image file.");
                    return;
                }

                using (image)
                    ValidateDimensions(image, ctx);
            });
    }

    protected virtual void ValidateDimensions(Image image, ValidationContext<IFormFile> ctx) { }
}

public sealed class BannerImageValidator : ImageValidator
{
    private const int MinWidth = 800;
    private const int MinHeight = 200;
    private const double MinRatio = 2.5;
    private const double MaxRatio = 4.0;

    protected override void ValidateDimensions(Image image, ValidationContext<IFormFile> ctx)
    {
        var ratio = (double)image.Width / image.Height;

        if (image.Width < MinWidth || image.Height < MinHeight)
            ctx.AddFailure($"Banner must be at least {MinWidth}x{MinHeight}px.");

        if (ratio < MinRatio || ratio > MaxRatio)
            ctx.AddFailure($"Banner aspect ratio must be between {MinRatio}:1 and {MaxRatio}:1 (landscape).");
    }
}

public sealed class AvatarImageValidator : ImageValidator
{
    private const int MinSize = 200;
    private const double MinRatio = 0.8;
    private const double MaxRatio = 1.25;

    protected override void ValidateDimensions(Image image, ValidationContext<IFormFile> ctx)
    {
        var ratio = (double)image.Width / image.Height;

        if (image.Width < MinSize || image.Height < MinSize)
            ctx.AddFailure($"Avatar must be at least {MinSize}x{MinSize}px.");

        if (ratio < MinRatio || ratio > MaxRatio)
            ctx.AddFailure($"Avatar must be roughly square (ratio between {MinRatio}:1 and {MaxRatio}:1).");
    }
}
