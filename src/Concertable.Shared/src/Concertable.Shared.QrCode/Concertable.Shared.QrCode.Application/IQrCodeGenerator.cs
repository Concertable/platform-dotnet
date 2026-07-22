namespace Concertable.Shared.QrCode.Application;

public interface IQrCodeGenerator
{
    byte[] Generate(string content);
}
