using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Concertable.Testing.Integration.Logging;

internal sealed class XunitLogger : ILogger
{
    private readonly string category;
    private readonly XunitOutputAccessor accessor;

    public XunitLogger(string category, XunitOutputAccessor accessor)
    {
        this.category = category;
        this.accessor = accessor;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var output = accessor.Output;
        if (output is null) return;

        var line = $"[{logLevel.ToShortString()}] {category}: {formatter(state, exception)}";
        TryWrite(output, line);
        if (exception is not null)
            TryWrite(output, exception.ToString());
    }

    private static void TryWrite(ITestOutputHelper output, string message)
    {
        try { output.WriteLine(message); }
        catch (InvalidOperationException) { }
    }
}

internal static class LogLevelExtensions
{
    public static string ToShortString(this LogLevel level) => level switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => level.ToString()
    };
}
