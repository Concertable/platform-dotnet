using Concertable.Kernel.Settings;
using Microsoft.Extensions.Options;

namespace Concertable.Kernel.Services;

public sealed class UriService : IUriService
{
    private readonly UrlSettings urlSettings;

    public UriService(IOptions<UrlSettings> urlSettings)
    {
        this.urlSettings = urlSettings.Value;
    }

    public Uri GetUri(string path, IDictionary<string, string>? query = null)
    {
        var builder = new UriBuilder(urlSettings.Frontend) { Path = path };

        if (query?.Count > 0)
            builder.Query = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        return builder.Uri;
    }
}
