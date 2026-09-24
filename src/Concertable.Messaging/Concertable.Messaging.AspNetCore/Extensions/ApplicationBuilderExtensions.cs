using Microsoft.AspNetCore.Builder;

namespace Concertable.Messaging.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Inserts <see cref="RequestQuiescenceMiddleware"/> so in-flight requests are tracked and drained by
        /// <see cref="Contracts.IHostQuiescence"/>. Place it early — before the endpoints whose handlers reach
        /// the database — and after <c>AddHttpRequestQuiescence()</c> has registered the participant.
        /// </summary>
        public IApplicationBuilder UseHostQuiescence() =>
            app.UseMiddleware<RequestQuiescenceMiddleware>();
    }
}
