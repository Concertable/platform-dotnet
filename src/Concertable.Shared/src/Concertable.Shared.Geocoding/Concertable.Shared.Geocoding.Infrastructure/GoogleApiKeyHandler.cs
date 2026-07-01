namespace Concertable.Shared.Geocoding.Infrastructure;

internal sealed class GoogleApiKeyHandler : DelegatingHandler
{
    private readonly string apiKey;

    public GoogleApiKeyHandler(string apiKey)
    {
        this.apiKey = apiKey;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri!;
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        request.RequestUri = new Uri($"{uri}{separator}key={Uri.EscapeDataString(apiKey)}");
        return base.SendAsync(request, ct);
    }
}
