using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Concertable.Kernel.Auth;

internal sealed class ClientCredentialsTokenService : ITokenService
{
    private readonly ITokenApi api;
    private readonly IOptions<TokenServiceOptions> options;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> cache = new();

    public ClientCredentialsTokenService(ITokenApi api, IOptions<TokenServiceOptions> options)
    {
        this.api = api;
        this.options = options;
    }

    public async Task<string> GetTokenAsync(string scope, CancellationToken ct = default)
    {
        if (TryGetCached(scope, out var token))
            return token;

        await gate.WaitAsync(ct);
        try
        {
            if (TryGetCached(scope, out token))
                return token;

            var opts = options.Value;
            var response = await api.GetTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = opts.ClientId,
                ["client_secret"] = opts.ClientSecret,
                ["scope"] = scope
            }, ct);

            token = response.AccessToken;
            cache[scope] = (token, DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 30));
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetCached(string scope, out string token)
    {
        if (cache.TryGetValue(scope, out var entry) && DateTimeOffset.UtcNow < entry.Expiry)
        {
            token = entry.Token;
            return true;
        }
        token = null!;
        return false;
    }
}
