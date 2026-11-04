using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.DataAccess.IntegrationTests;

public sealed class ReferenceEntity : IEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Sha256Digest? Fingerprint { get; set; }
    public List<ReferenceNoteEntity> Notes { get; set; } = [];
}

public sealed class ReferenceNoteEntity : IEntity
{
    public int Id { get; set; }
    public int ReferenceId { get; set; }
    public string Text { get; set; } = null!;
}

public sealed class ReferenceDbContext : DbContextBase
{
    public const string Table = "References";
    public const string NotesTable = "ReferenceNotes";

    public ReferenceDbContext(DbContextOptions<ReferenceDbContext> options)
        : base(options, Options.Create(new OutboxOptions())) { }

    public DbSet<ReferenceEntity> References => Set<ReferenceEntity>();
    public DbSet<ReferenceNoteEntity> ReferenceNotes => Set<ReferenceNoteEntity>();

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
            reference.HasMany(entity => entity.Notes).WithOne().HasForeignKey(note => note.ReferenceId);
        });

        modelBuilder.Entity<ReferenceNoteEntity>(note =>
        {
            note.ToTable(NotesTable, DataAccessFixture.Schema);
            note.HasKey(entity => entity.Id);
            note.Property(entity => entity.Text).IsRequired().HasMaxLength(128);
        });
    }
}
