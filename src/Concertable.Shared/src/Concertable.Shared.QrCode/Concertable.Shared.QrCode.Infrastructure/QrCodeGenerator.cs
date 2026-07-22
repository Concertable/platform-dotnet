using Concertable.Shared.QrCode.Application;
using QRCoder;

namespace Concertable.Shared.QrCode.Infrastructure;

internal sealed class QrCodeGenerator : IQrCodeGenerator
{
    private readonly QRCodeGenerator qrCodeGenerator;

    public QrCodeGenerator(QRCodeGenerator qrCodeGenerator) => this.qrCodeGenerator = qrCodeGenerator;

    public byte[] Generate(string content)
    {
        QRCodeData data = qrCodeGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(20);
    }
}
