using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.UnitTests;

public sealed class AuditInterceptorTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid userId;
    private readonly TestTimeProvider timeProvider;
    private readonly AuditInterceptor interceptor;

    public AuditInterceptorTests()
    {
        this.userId = Guid.CreateVersion7();
        this.timeProvider = new TestTimeProvider(CreatedAt);
        this.interceptor = new AuditInterceptor(new TestCurrentUser(this.userId), this.timeProvider);
    }

    [Fact]
    public async Task SavingChanges_AddedEntity_StampsCreationAudit()
    {
        await using var context = this.CreateContext();
        var entity = new AuditableEntity { Name = "Created" };
        context.Entities.Add(entity);

        await context.SaveChangesAsync();

        Assert.Equal(CreatedAt, entity.CreatedAt);
        Assert.Equal(this.userId.ToString(), entity.CreatedBy);
        Assert.Null(entity.LastModifiedAt);
        Assert.Null(entity.LastModifiedBy);
    }

    [Fact]
    public async Task SavingChanges_ModifiedEntity_StampsModificationAudit()
    {
        await using var context = this.CreateContext();
        var entity = new AuditableEntity { Name = "Created" };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();
        var modifiedAt = CreatedAt.AddMinutes(5);
        this.timeProvider.SetUtcNow(modifiedAt);
        entity.Name = "Modified";

        await context.SaveChangesAsync();

        Assert.Equal(CreatedAt, entity.CreatedAt);
        Assert.Equal(this.userId.ToString(), entity.CreatedBy);
        Assert.Equal(modifiedAt, entity.LastModifiedAt);
        Assert.Equal(this.userId.ToString(), entity.LastModifiedBy);
    }

    private AuditDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(this.interceptor)
            .Options;
        return new AuditDbContext(options);
    }

    private sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
    {
        public DbSet<AuditableEntity> Entities => Set<AuditableEntity>();
    }

    private sealed class AuditableEntity : IAuditable
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTimeOffset? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid id)
        {
            this.Id = id;
        }

        public Guid? Id { get; }
        public string? Email => null;
        public bool IsAuthenticated => true;
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => this.utcNow;

        public void SetUtcNow(DateTimeOffset value) => this.utcNow = value;
    }
}
