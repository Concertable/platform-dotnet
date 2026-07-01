using System.Text.Json.Serialization;
using Refit;

namespace Concertable.Kernel.Auth;

/// <summary>
/// Wire contract for the OAuth2 <c>/connect/token</c> client-credentials endpoint. The authority is
/// the base address, configured once per host in <c>AddClientCredentials</c>; scope/credentials ride
/// in the form body per call. This client carries no bearer of its own — it is what mints them.
/// </summary>
internal interface ITokenApi
{
    [Post("/connect/token")]
    Task<TokenResponse> GetTokenAsync(
        [Body(BodySerializationMethod.UrlEncoded)] IDictionary<string, string> form,
        CancellationToken ct = default);
}

/// <summary>The subset of the OAuth2 token response this service reads.</summary>
internal sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
