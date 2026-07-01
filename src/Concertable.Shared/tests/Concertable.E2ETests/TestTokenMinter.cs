using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Concertable.E2ETests;

public sealed class TestTokenMinter : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly string authBaseUrl;

    public TestTokenMinter(IConfiguration configuration)
    {
        authBaseUrl = configuration["Endpoints:Auth"]
            ?? throw new InvalidOperationException("Endpoints:Auth is not configured.");

        httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    public async Task<string> MintAsync(string email, string password)
    {
        var response = await httpClient.PostAsync($"{authBaseUrl}/connect/token",
            new FormUrlEncodedContent([
                new("grant_type", "password"),
                new("client_id", "concertable-test"),
                new("username", email),
                new("password", password),
                new("scope", "concertable.b2b.api concertable.customer.api concertable.search.api"),
            ]));
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public void Dispose() => httpClient.Dispose();
}
