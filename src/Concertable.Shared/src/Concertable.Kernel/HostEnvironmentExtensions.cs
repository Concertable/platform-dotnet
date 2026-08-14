using Microsoft.Extensions.Hosting;

namespace Concertable.Kernel;

/// <summary>
/// Predicates for Concertable's custom host environments on <see cref="IHostEnvironment"/>, mirroring the
/// framework's <see cref="HostEnvironmentEnvExtensions.IsDevelopment"/> / <c>IsProduction</c> so a host reads
/// <c>env.IsIntegration()</c> instead of <c>IsEnvironment("Integration")</c> with a bare string.
/// </summary>
public static class HostEnvironmentExtensions
{
    /// <summary>Whether the host is running under the integration-test environment.</summary>
    public static bool IsIntegration(this IHostEnvironment environment) =>
        environment.IsEnvironment(HostEnvironments.Integration);

    /// <summary>Whether the host is running under the E2E environment.</summary>
    public static bool IsE2E(this IHostEnvironment environment) =>
        environment.IsEnvironment(HostEnvironments.E2E);
}
