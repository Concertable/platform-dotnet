namespace Concertable.Testing.Integration;

/// <summary>
/// The custom host environment names a fixture boots under. <c>HostEnvironments</c> rather than bare
/// <c>Environments</c> because the production hosts reference the same value, and the Web SDK auto-imports
/// the framework's <c>Microsoft.Extensions.Hosting.Environments</c> — which a bare name would collide with.
/// </summary>
public static class HostEnvironments
{
    public const string Integration = "Integration";
    public const string E2E = "E2E";
}
