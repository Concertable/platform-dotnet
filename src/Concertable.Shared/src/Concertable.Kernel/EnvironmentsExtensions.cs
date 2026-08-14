using Microsoft.Extensions.Hosting;

namespace Concertable.Kernel;

/// <summary>
/// Concertable's custom environment names, hung onto the framework's <see cref="Environments"/> so they read
/// alongside the built-ins — <c>Environments.Integration</c> next to <c>Environments.Development</c>.
/// </summary>
public static class EnvironmentsExtensions
{
    extension(Environments)
    {
        public static string Integration => "Integration";
        public static string E2E => "E2E";
    }
}
