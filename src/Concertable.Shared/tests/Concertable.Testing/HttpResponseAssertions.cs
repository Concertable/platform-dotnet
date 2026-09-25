using System.Net;
using Xunit.Sdk;

namespace Concertable.Testing;

public static class HttpResponseAssertions
{
    extension(HttpResponseMessage response)
    {
        public async Task<HttpResponseMessage> ShouldBe(HttpStatusCode expected)
        {
            if (response.StatusCode == expected)
                return response;

            var body = await response.Content.ReadAsStringAsync();
            throw new XunitException(
                $"Expected {(int)expected} {expected}, got {(int)response.StatusCode} {response.StatusCode}.\n" +
                $"Request: {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}\n" +
                $"Body:\n{body}");
        }
    }
}
