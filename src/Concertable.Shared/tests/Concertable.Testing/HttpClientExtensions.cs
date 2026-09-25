using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.Testing;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    extension(HttpClient client)
    {
        public async Task<HttpResponseMessage> PostAsync<T>(string url, T body)
        {
            return await client.PostAsJsonAsync(url, body, JsonOptions);
        }

        public async Task<HttpResponseMessage> PostAsync(string url)
        {
            return await client.PostAsJsonAsync<object?>(url, null, JsonOptions);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string url, T body)
        {
            return await client.PutAsJsonAsync(url, body, JsonOptions);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            return await client.DeleteAsync(url);
        }
    }

    extension(HttpContent content)
    {
        public async Task<T?> ReadAsync<T>()
        {
            return await content.ReadFromJsonAsync<T>(JsonOptions);
        }
    }
}
