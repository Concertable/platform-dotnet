using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.DataAccess.IntegrationTests;

public sealed class ReferenceEntity : IEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public sealed class ReferenceDbContext : DbContextBase
{
    public const string Table = "References";

    public ReferenceDbContext(DbContextOptions<ReferenceDbContext> options)
        : base(options, Options.Create(new OutboxOptions())) { }

    public DbSet<ReferenceEntity> References => Set<ReferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReferenceEntity>(reference =>
        {
            reference.ToTable(Table, DataAccessFixture.Schema);
            reference.HasKey(entity => entity.Id);
            reference.Property(entity => entity.Code).IsRequired().HasMaxLength(64);
            reference.Property(entity => entity.Name).IsRequired().HasMaxLength(128);
            reference.HasIndex(entity => entity.Code).IsUnique();
        });
    }
}
