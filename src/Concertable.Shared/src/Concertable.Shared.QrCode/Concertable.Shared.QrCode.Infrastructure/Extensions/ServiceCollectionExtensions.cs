using Concertable.Shared.QrCode.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.QrCode.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddQrCode()
        {
            services.AddSingleton<QRCoder.QRCodeGenerator>();
            services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
            return services;
        }
    }
}
