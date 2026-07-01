using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.Testing;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string url, T body)
    {
        return await client.PostAsJsonAsync(url, body, JsonOptions);
    }

    public static async Task<HttpResponseMessage> PostAsync(this HttpClient client, string url)
    {
        return await client.PostAsJsonAsync<object?>(url, null, JsonOptions);
    }

    public static async Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, string url, T body)
    {
        return await client.PutAsJsonAsync(url, body, JsonOptions);
    }

    public static async Task<HttpResponseMessage> DeleteAsync(this HttpClient client, string url)
    {
        return await client.DeleteAsync(url);
    }

    public static async Task<T?> ReadAsync<T>(this HttpContent content)
    {
        return await content.ReadFromJsonAsync<T>(JsonOptions);
    }
}
