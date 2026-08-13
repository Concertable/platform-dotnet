namespace Concertable.Testing.Integration;

/// <summary>
/// The environment names an integration or E2E host runs under. Use these instead of raw
/// <c>"Testing"</c> / <c>"E2E"</c> literals so a fixture and the <c>Program</c> it boots agree.
/// </summary>
public static class TestEnvironments
{
    public const string Testing = "Testing";
    public const string E2E = "E2E";
}
