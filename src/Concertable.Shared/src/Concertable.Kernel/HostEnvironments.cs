namespace Concertable.Kernel;

/// <summary>
/// The custom host environment names shared by production hosts and their integration fixtures — the one
/// owner for these strings, so a `Program`'s `IsEnvironment(...)` and a fixture's `UseEnvironment(...)` can't
/// drift apart. Named <c>HostEnvironments</c>, not bare <c>Environments</c>, to avoid a clash with the
/// framework's <c>Microsoft.Extensions.Hosting.Environments</c> (Development/Staging/Production).
/// </summary>
public static class HostEnvironments
{
    public const string Integration = "Integration";
    public const string E2E = "E2E";
}
