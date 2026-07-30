using Microsoft.Extensions.Logging;

namespace Concertable.Shared.Api;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An unhandled exception occurred.")]
    internal static partial void UnhandledException(this ILogger logger, Exception exception);
}
