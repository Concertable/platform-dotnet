using Microsoft.Extensions.Logging.Abstractions;

namespace Concertable.Seed.Shared.UnitTests;

public sealed class SeedChainTests
{
    private readonly List<string> calls;

    public SeedChainTests()
    {
        this.calls = [];
    }

    #region SeedAsync

    [Fact]
    public async Task SeedAsync_SeedersOutOfOrder_RunsThemInOrderAscending()
    {
        ISeeder[] seeders =
        [
            new RecordingSeeder("concert", 7, this.calls),
            new RecordingSeeder("venue", 2, this.calls),
            new RecordingSeeder("user", 0, this.calls),
        ];

        await SeedChain.SeedAsync(seeders, NullLogger.Instance);

        Assert.Equal(["seed:user", "seed:venue", "seed:concert"], this.calls);
    }

    [Fact]
    public async Task SeedAsync_SeedersShareAnOrder_RunsThemInRegistrationOrder()
    {
        ISeeder[] seeders =
        [
            new RecordingSeeder("admin", 1, this.calls),
            new RecordingSeeder("artist", 1, this.calls),
            new RecordingSeeder("tenant", 1, this.calls),
        ];

        await SeedChain.SeedAsync(seeders, NullLogger.Instance);

        Assert.Equal(["seed:admin", "seed:artist", "seed:tenant"], this.calls);
    }

    [Fact]
    public async Task SeedAsync_SeederThrows_RethrowsAndStopsTheChain()
    {
        var failure = new InvalidOperationException("Cannot insert explicit value for identity column");
        ISeeder[] seeders =
        [
            new RecordingSeeder("venue", 2, this.calls),
            new RecordingSeeder("deal", 3, this.calls) { Failure = failure },
            new RecordingSeeder("booking", 6, this.calls),
        ];

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SeedChain.SeedAsync(seeders, NullLogger.Instance));

        Assert.Same(failure, thrown);
        Assert.Equal(["seed:venue", "seed:deal"], this.calls);
    }

    [Fact]
    public async Task SeedAsync_NoSeeders_CompletesWithoutRunningAnything()
    {
        await SeedChain.SeedAsync([], NullLogger.Instance);

        Assert.Empty(this.calls);
    }

    [Fact]
    public async Task SeedAsync_TokenSupplied_PassesItToEverySeeder()
    {
        using var cts = new CancellationTokenSource();
        var seeder = new RecordingSeeder("venue", 2, this.calls);

        await SeedChain.SeedAsync([seeder], NullLogger.Instance, cts.Token);

        Assert.Equal(cts.Token, seeder.ObservedToken);
    }

    #endregion

    #region MigrateAsync

    [Fact]
    public async Task MigrateAsync_SeedersOutOfOrder_MigratesThemInOrderAscending()
    {
        ISeeder[] seeders =
        [
            new RecordingSeeder("concert", 7, this.calls),
            new RecordingSeeder("user", 0, this.calls),
        ];

        await SeedChain.MigrateAsync(seeders, NullLogger.Instance);

        Assert.Equal(["migrate:user", "migrate:concert"], this.calls);
    }

    #endregion

    private sealed class RecordingSeeder : ISeeder
    {
        private readonly List<string> calls;

        public RecordingSeeder(string name, int order, List<string> calls)
        {
            this.Name = name;
            this.Order = order;
            this.calls = calls;
        }

        public string Name { get; }
        public int Order { get; }
        public Exception? Failure { get; init; }
        public CancellationToken ObservedToken { get; private set; }

        public Task MigrateAsync(CancellationToken ct = default)
        {
            this.calls.Add($"migrate:{this.Name}");
            return Task.CompletedTask;
        }

        public Task SeedAsync(CancellationToken ct = default)
        {
            this.ObservedToken = ct;
            this.calls.Add($"seed:{this.Name}");
            return this.Failure is null ? Task.CompletedTask : Task.FromException(this.Failure);
        }
    }
}
