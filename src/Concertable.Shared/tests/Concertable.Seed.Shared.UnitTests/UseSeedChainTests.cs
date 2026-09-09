using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Concertable.Seed.Shared.UnitTests;

public sealed class UseSeedChainTests
{
    private readonly List<string> calls;
    private readonly ServiceProvider provider;

    public UseSeedChainTests()
    {
        this.calls = [];

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(this.calls);
        services.AddDbContext<SeededDbContext>((sp, opt) =>
            opt.UseInMemoryDatabase(Guid.NewGuid().ToString()).UseSeedChain(sp));
        services.AddDbContext<UnseededDbContext>((sp, opt) =>
            opt.UseInMemoryDatabase(Guid.NewGuid().ToString()).UseSeedChain(sp));
        services.AddSeeder<SeededDbContext, FirstSeeder>();
        services.AddSeeder<SeededDbContext, SecondSeeder>();
        services.AddSeeder<UnseededDbContext, OtherContextSeeder>();

        this.provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task UseSeedChain_ContextCreated_RunsThatContextsSeedersInOrder()
    {
        using var scope = this.provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SeededDbContext>();

        await context.Database.EnsureCreatedAsync();

        Assert.Equal(["second", "first"], this.calls);
    }

    [Fact]
    public async Task UseSeedChain_ContextCreated_RunsNoOtherContextsSeeders()
    {
        using var scope = this.provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SeededDbContext>();

        await context.Database.EnsureCreatedAsync();

        Assert.Contains("first", this.calls);
        Assert.DoesNotContain("other-context", this.calls);
    }

    private sealed class SeededDbContext : DbContext
    {
        public SeededDbContext(DbContextOptions<SeededDbContext> options)
            : base(options)
        {
        }
    }

    private sealed class UnseededDbContext : DbContext
    {
        public UnseededDbContext(DbContextOptions<UnseededDbContext> options)
            : base(options)
        {
        }
    }

    private abstract class RecordingSeeder : ISeeder
    {
        private readonly List<string> calls;

        protected RecordingSeeder(List<string> calls)
        {
            this.calls = calls;
        }

        public abstract int Order { get; }
        protected abstract string Name { get; }

        public Task SeedAsync(CancellationToken ct = default)
        {
            this.calls.Add(this.Name);
            return Task.CompletedTask;
        }
    }

    private sealed class FirstSeeder : RecordingSeeder
    {
        public FirstSeeder(List<string> calls)
            : base(calls)
        {
        }

        public override int Order => 2;
        protected override string Name => "first";
    }

    private sealed class SecondSeeder : RecordingSeeder
    {
        public SecondSeeder(List<string> calls)
            : base(calls)
        {
        }

        public override int Order => 1;
        protected override string Name => "second";
    }

    private sealed class OtherContextSeeder : RecordingSeeder
    {
        public OtherContextSeeder(List<string> calls)
            : base(calls)
        {
        }

        public override int Order => 0;
        protected override string Name => "other-context";
    }
}
