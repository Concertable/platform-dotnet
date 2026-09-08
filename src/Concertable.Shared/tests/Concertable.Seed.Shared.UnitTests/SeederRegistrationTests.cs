using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Seed.Shared.UnitTests;

public sealed class SeederRegistrationTests
{
    private readonly FakeServiceCollection services;

    public SeederRegistrationTests()
    {
        this.services = [];
    }

    #region AddSeeder

    [Fact]
    public void AddSeeder_OrdinarySeeder_RegistersItKeyedOnTheContextType()
    {
        this.services.AddSeeder<StubDbContext, StubSeeder>();

        var descriptor = Assert.Single(this.services);
        Assert.Equal(typeof(ISeeder), descriptor.ServiceType);
        Assert.Equal(typeof(StubDbContext), descriptor.ServiceKey);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddSeeder_StandInSeeder_ThrowsAndRegistersNothing()
    {
        var thrown = Assert.Throws<InvalidOperationException>(
            () => this.services.AddSeeder<StubDbContext, StubStandInSeeder>());

        Assert.Contains(nameof(IStandInSeeder), thrown.Message);
        Assert.Empty(this.services);
    }

    #endregion

    #region AddStandInSeeder

    [Fact]
    public void AddStandInSeeder_StandInSeeder_RegistersItKeyedOnTheContextType()
    {
        this.services.AddStandInSeeder<StubDbContext, StubStandInSeeder>();

        var descriptor = Assert.Single(this.services);
        Assert.Equal(typeof(ISeeder), descriptor.ServiceType);
        Assert.Equal(typeof(StubDbContext), descriptor.ServiceKey);
    }

    #endregion

    private sealed class FakeServiceCollection : List<ServiceDescriptor>, IServiceCollection { }

    private sealed class StubDbContext : DbContext { }

    private sealed class StubSeeder : ISeeder
    {
        public int Order => 0;
        public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubStandInSeeder : IStandInSeeder
    {
        public int Order => 0;
        public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
