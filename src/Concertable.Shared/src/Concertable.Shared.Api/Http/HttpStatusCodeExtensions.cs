using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Concertable.Shared.Api.Http;

internal static class HttpStatusCodeExtensions
{
    internal static string ToReasonPhrase(this HttpStatusCode statusCode) =>
        ReasonPhrases.GetReasonPhrase((int)statusCode);
}
