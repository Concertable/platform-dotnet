namespace Concertable.Kernel.Auth;

public interface ITokenService
{
    Task<string> GetTokenAsync(string scope, CancellationToken ct = default);
}
