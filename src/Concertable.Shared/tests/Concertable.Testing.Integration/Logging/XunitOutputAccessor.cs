using Xunit.Abstractions;

namespace Concertable.Testing.Integration.Logging;

public sealed class XunitOutputAccessor
{
    public ITestOutputHelper? Output { get; set; }
}
