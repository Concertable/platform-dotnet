namespace Concertable.Seed.Shared;

public interface ISeeder
{
    int Order { get; }
    Task MigrateAsync(CancellationToken ct = default) => Task.CompletedTask;
    Task SeedAsync(CancellationToken ct = default);
}

/// <summary>
/// Seeds rows whose only production write path is a handler reacting to another service's event, so it may
/// run ONLY where that producer is absent — an integration fixture. Never in dev or E2E, where the real
/// producer writes them and a direct insert would collide with it.
/// </summary>
public interface IStandInSeeder : ISeeder { }

/// <summary>
/// Superseded by <see cref="ISeeder"/>. Retained until every consumer's platform pin has moved: assemblies
/// already compiled against it name this type in their interface maps, so removing it before then is a
/// type-load failure at boot that the build does not catch.
/// </summary>
public interface IDbSeeder : ISeeder { }

/// <summary>Runs in dev AND E2E environments, against real external APIs. Superseded by <see cref="ISeeder"/>.</summary>
public interface IDevSeeder : IDbSeeder { }

/// <summary>Runs in integration tests ONLY. Superseded by <see cref="IStandInSeeder"/>.</summary>
public interface ITestSeeder : IDbSeeder { }
