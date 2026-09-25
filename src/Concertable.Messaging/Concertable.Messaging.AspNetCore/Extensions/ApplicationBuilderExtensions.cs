using Microsoft.AspNetCore.Builder;

namespace Concertable.Messaging.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Inserts <see cref="PausableRequestsMiddleware"/> so requests are held while the host is paused and
        /// in-flight ones are waited for. Place it before the endpoints whose handlers reach the database, and
        /// after <c>AddPausableRequests()</c> has registered it.
        /// </summary>
        public IApplicationBuilder UsePausableRequests() =>
            app.UseMiddleware<PausableRequestsMiddleware>();
    }
}
