using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.UnitTests;

public sealed class TenantInterceptorTests
{
    private readonly Guid tenantId;
    private readonly TestTenantContext tenantContext;
    private readonly TenantInterceptor interceptor;

    public TenantInterceptorTests()
    {
        this.tenantId = Guid.CreateVersion7();
        this.tenantContext = new TestTenantContext(this.tenantId);
        this.interceptor = new TenantInterceptor(this.tenantContext);
    }

    [Fact]
    public async Task SavingChanges_AddedEntityWithoutTenant_StampsCurrentTenant()
    {
        await using var context = this.CreateContext();
        var entity = new TenantScopedEntity();
        context.Entities.Add(entity);

        await context.SaveChangesAsync();

        Assert.Equal(this.tenantId, entity.TenantId);
    }

    [Fact]
    public async Task SavingChanges_AddedEntityForDifferentTenant_Throws()
    {
        await using var context = this.CreateContext();
        context.Entities.Add(new TenantScopedEntity { TenantId = Guid.CreateVersion7() });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.StartsWith("Cross-tenant write blocked:", exception.Message);
    }

    [Theory]
    [InlineData(EntityState.Added)]
    [InlineData(EntityState.Modified)]
    [InlineData(EntityState.Deleted)]
    public async Task SavingChanges_UnresolvedTenant_Throws(EntityState state)
    {
        await using var context = this.CreateContext();
        var entity = new TenantScopedEntity { TenantId = this.tenantId };
        context.Attach(entity).State = state;
        this.tenantContext.TenantId = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Equal("Cannot persist a tenant-scoped entity without a current tenant.", exception.Message);
    }

    [Theory]
    [InlineData(EntityState.Modified)]
    [InlineData(EntityState.Deleted)]
    public async Task SavingChanges_ExistingEntityForDifferentTenant_Throws(EntityState state)
    {
        await using var context = this.CreateContext();
        var entity = new TenantScopedEntity { TenantId = Guid.CreateVersion7() };
        context.Attach(entity).State = state;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.StartsWith("Cross-tenant write blocked:", exception.Message);
    }

    private TenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(this.interceptor)
            .Options;
        return new TenantDbContext(options);
    }

    private sealed class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
    {
        public DbSet<TenantScopedEntity> Entities => Set<TenantScopedEntity>();
    }

    private sealed class TenantScopedEntity : ITenantScoped
    {
        public int Id { get; private set; }
        public Guid TenantId { get; set; }
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public TestTenantContext(Guid tenantId)
        {
            this.TenantId = tenantId;
        }

        public Guid? TenantId { get; set; }
    }
}
