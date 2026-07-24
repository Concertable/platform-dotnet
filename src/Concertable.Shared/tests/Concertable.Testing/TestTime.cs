namespace Concertable.Testing;

/// <summary>A fixed reference instant for deterministic tests — a frozen "now" that dated test data hangs off.</summary>
public static class TestTime
{
    public static readonly DateTimeOffset Now = new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero);
}
