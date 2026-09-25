using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Concertable.Shared.Api.Http;

internal static class HttpStatusCodeExtensions
{
    extension(HttpStatusCode statusCode)
    {
        internal string ToReasonPhrase() =>
            ReasonPhrases.GetReasonPhrase((int)statusCode);
    }
}
