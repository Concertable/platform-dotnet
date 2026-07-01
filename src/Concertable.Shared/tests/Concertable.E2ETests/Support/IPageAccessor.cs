using Microsoft.Playwright;

namespace Concertable.E2ETests.Support;

public interface IPageAccessor
{
    IPage Page { get; }
}
