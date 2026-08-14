using Microsoft.Extensions.Hosting;

namespace Concertable.Testing.Integration;

/// <summary>
/// Transitional pre-publish copy of <c>Concertable.Kernel.EnvironmentsExtensions</c> — fixtures read
/// <c>Environments.Integration</c> from source until the Kernel version is on the feed, then this is deleted and
/// they point at Kernel's.
/// </summary>
public static class EnvironmentsExtensions
{
    extension(Environments)
    {
        public static string Integration => "Integration";
        public static string E2E => "E2E";
    }
}
