namespace Concertable.Seed.Shared;

public interface IDbSeeder
{
    int Order { get; }
    Task MigrateAsync(CancellationToken ct = default) => Task.CompletedTask;
    Task SeedAsync(CancellationToken ct = default);
}

/// <summary>
/// Runs in dev AND E2E environments. Seeds realistic data against real external APIs (real Stripe accounts etc.).
/// Use this when an E2E test is missing data.
/// </summary>
public interface IDevSeeder : IDbSeeder { }

/// <summary>
/// Runs in integration tests ONLY — never in dev or E2E.
/// Uses fake/stub IDs (e.g. "acct_test_venue1") that only work with E2EStripeAccountClient.
/// Do NOT reach for this to fix E2E failures.
/// </summary>
public interface ITestSeeder : IDbSeeder { }
