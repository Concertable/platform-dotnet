using System.Net;
using Xunit.Sdk;

namespace Concertable.Testing;

public static class HttpResponseAssertions
{
    public static async Task<HttpResponseMessage> ShouldBe(
        this HttpResponseMessage response,
        HttpStatusCode expected)
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
