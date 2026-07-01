using Microsoft.Extensions.Logging;

namespace Concertable.Kernel;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred executing background work item.")]
    internal static partial void BackgroundWorkItemError(this ILogger logger, Exception ex);
}
