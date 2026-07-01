using Microsoft.Extensions.Logging;

namespace Concertable.Seed.Shared;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Beginning DB initialization ({SeederCount} seeders)")]
    internal static partial void BeginDbInitialization(this ILogger logger, int seederCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Migrating {SeederName}")]
    internal static partial void MigratingSeeder(this ILogger logger, string seederName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding {SeederName} (order {Order})")]
    internal static partial void SeedingSeeder(this ILogger logger, string seederName, int order);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Seeder {SeederName} completed in {ElapsedMs}ms")]
    internal static partial void SeederCompleted(this ILogger logger, string seederName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Seeder {SeederName} failed after {ElapsedMs}ms")]
    internal static partial void SeederFailed(this ILogger logger, string seederName, long elapsedMs, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "DB initialization complete in {ElapsedMs}ms")]
    internal static partial void DbInitializationComplete(this ILogger logger, long elapsedMs);
}
