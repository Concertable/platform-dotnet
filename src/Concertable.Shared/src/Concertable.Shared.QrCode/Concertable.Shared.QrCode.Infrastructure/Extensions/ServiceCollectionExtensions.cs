using Concertable.Shared.QrCode.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.QrCode.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQrCode(this IServiceCollection services)
    {
        services.AddSingleton<QRCoder.QRCodeGenerator>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        return services;
    }
}
