using Microsoft.Extensions.Logging;

namespace Concertable.Testing.Integration.Logging;

public sealed class XunitLoggerProvider : ILoggerProvider
{
    private readonly XunitOutputAccessor accessor;

    public XunitLoggerProvider(XunitOutputAccessor accessor) => this.accessor = accessor;

    public ILogger CreateLogger(string categoryName) => new XunitLogger(categoryName, accessor);

    public void Dispose() { }
}
