using Microsoft.AspNetCore.Builder;

namespace Concertable.Messaging.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Inserts <see cref="GateMiddleware"/>. Place it before the endpoints whose handlers reach the database,
        /// and after <c>AddGate()</c> has registered it.
        /// </summary>
        public IApplicationBuilder UseGate() =>
            app.UseMiddleware<GateMiddleware>();
    }
}
