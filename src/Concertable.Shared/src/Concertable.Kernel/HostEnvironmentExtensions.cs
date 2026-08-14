using Microsoft.Extensions.Hosting;

namespace Concertable.Kernel;

/// <summary>
/// Custom-environment predicates on <see cref="IHostEnvironment"/>, mirroring the framework's
/// <see cref="HostEnvironmentEnvExtensions.IsDevelopment"/> so a host reads <c>env.IsIntegration()</c> next to
/// <c>env.IsDevelopment()</c>.
/// </summary>
public static class HostEnvironmentExtensions
{
    extension(IHostEnvironment environment)
    {
        public bool IsIntegration() => environment.IsEnvironment(Environments.Integration);
        public bool IsE2E() => environment.IsEnvironment(Environments.E2E);
    }
}
