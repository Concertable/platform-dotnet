using Concertable.Seed.Shared.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Seed.Shared.UnitTests;

public sealed class SeedingIdentityRewriterTests
{
    private readonly Dictionary<string, string> identityTables;

    public SeedingIdentityRewriterTests()
    {
        using var context = new SeedModelContext();
        this.identityTables = SeedingIdentityRewriter.BuildTableMap(context.Model);
    }

    #region Rewrite

    [Fact]
    public void Rewrite_InsertSupplyingTheIdentityColumn_WrapsTheCommandInAnIdentityInsertWindow()
    {
        const string sql = "INSERT INTO [Widgets] ([Id], [Name])\nVALUES (@p0, @p1);";

        var rewritten = SeedingIdentityRewriter.Rewrite(sql, this.identityTables);

        Assert.Equal(
            "SET IDENTITY_INSERT [Widgets] ON;\n" + sql + "\nSET IDENTITY_INSERT [Widgets] OFF;\n",
            rewritten);
    }

    [Fact]
    public void Rewrite_MergeSupplyingTheIdentityColumnOfATphTable_WrapsTheCommandInAnIdentityInsertWindow()
    {
        const string sql = """
            MERGE [Animals] USING (
            VALUES (@p0, @p1, @p2, 0),
            (@p3, @p4, @p5, 1)) AS i ([Id], [Name], [Discriminator], _Position) ON 1=0
            WHEN NOT MATCHED THEN
            INSERT ([Id], [Name], [Discriminator])
            VALUES (i.[Id], i.[Name], i.[Discriminator]);
            """;

        var rewritten = SeedingIdentityRewriter.Rewrite(sql, this.identityTables);

        Assert.NotNull(rewritten);
        Assert.StartsWith("SET IDENTITY_INSERT [Animals] ON;\n", rewritten);
        Assert.EndsWith("\nSET IDENTITY_INSERT [Animals] OFF;\n", rewritten);
    }

    [Fact]
    public void Rewrite_SchemaQualifiedIdentityTable_WrapsUsingTheQualifiedName()
    {
        const string sql = "INSERT INTO [catalog].[Gadgets] ([Id], [Code])\nVALUES (@p0, @p1);";

        var rewritten = SeedingIdentityRewriter.Rewrite(sql, this.identityTables);

        Assert.Equal(
            "SET IDENTITY_INSERT [catalog].[Gadgets] ON;\n" + sql + "\nSET IDENTITY_INSERT [catalog].[Gadgets] OFF;\n",
            rewritten);
    }

    [Fact]
    public void Rewrite_SecondMappedTableLettingTheDatabaseGenerateItsIdentity_WrapsOnlyTheTableSupplyingOne()
    {
        const string sql = """
            INSERT INTO [Widgets] ([Name])
            VALUES (@p0);
            INSERT INTO [Animals] ([Id], [Name], [Discriminator])
            VALUES (@p1, @p2, @p3);
            """;

        var rewritten = SeedingIdentityRewriter.Rewrite(sql, this.identityTables);

        Assert.NotNull(rewritten);
        Assert.StartsWith("SET IDENTITY_INSERT [Animals] ON;\n", rewritten);
        Assert.DoesNotContain("[Widgets] ON", rewritten);
    }

    [Fact]
    public void Rewrite_TwoStatementsAgainstTheSameIdentityTable_WrapsItOnce()
    {
        const string sql = """
            INSERT INTO [Animals] ([Id], [Name], [Discriminator])
            VALUES (@p0, @p1, @p2);
            INSERT INTO [Animals] ([Id], [Name], [Discriminator])
            VALUES (@p3, @p4, @p5);
            """;

        var rewritten = SeedingIdentityRewriter.Rewrite(sql, this.identityTables);

        Assert.NotNull(rewritten);
        Assert.Equal(1, Occurrences(rewritten, "SET IDENTITY_INSERT [Animals] ON;"));
        Assert.Equal(1, Occurrences(rewritten, "SET IDENTITY_INSERT [Animals] OFF;"));
    }

    [Fact]
    public void Rewrite_MergeCarryingAnOutputClauseAndDeclaredTableVariable_WrapsTheTable()
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @inserted0 TABLE ([Id] int, [_Position] int);
            MERGE [Animals] USING (
            VALUES (@p0, @p1, @p2, 0)) AS i ([Id], [Name], [Discriminator], _Position) ON 1=0
            WHEN NOT MATCHED THEN
            INSERT ([Id], [Name], [Discriminator])
            VALUES (i.[Id], i.[Name], i.[Discriminator])
            OUTPUT INSERTED.[Id], i._Position INTO @inserted0;
            SELECT [Id] FROM @inserted0 ORDER BY [_Position];
            """;

        var rewritten = SeedingIdentityRewriter.Rewrite(sql, this.identityTables);

        Assert.NotNull(rewritten);
        Assert.Equal(1, Occurrences(rewritten, "SET IDENTITY_INSERT [Animals] ON;"));
        Assert.DoesNotContain("[]", rewritten);
    }

    [Fact]
    public void Rewrite_AnInsertAndAMergeForDifferentIdentityTables_ThrowsNamingBothInOrder()
    {
        const string sql = """
            INSERT INTO [Widgets] ([Id], [Name])
            VALUES (@p0, @p1);
            MERGE [Animals] USING (
            VALUES (@p2, @p3, @p4, 0)) AS i ([Id], [Name], [Discriminator], _Position) ON 1=0
            WHEN NOT MATCHED THEN
            INSERT ([Id], [Name], [Discriminator])
            VALUES (i.[Id], i.[Name], i.[Discriminator]);
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => SeedingIdentityRewriter.Rewrite(sql, this.identityTables));

        Assert.Contains("[Animals] and [Widgets]", exception.Message);
    }

    [Fact]
    public void Rewrite_InsertLettingTheDatabaseGenerateTheIdentity_LeavesTheCommandAlone()
    {
        const string sql = "INSERT INTO [Widgets] ([Name])\nVALUES (@p0);";

        Assert.Null(SeedingIdentityRewriter.Rewrite(sql, this.identityTables));
    }

    [Fact]
    public void Rewrite_InsertIntoATableWithoutAnIdentityColumn_LeavesTheCommandAlone()
    {
        const string sql = "INSERT INTO [Tokens] ([Id], [Value])\nVALUES (@p0, @p1);";

        Assert.Null(SeedingIdentityRewriter.Rewrite(sql, this.identityTables));
    }

    [Fact]
    public void Rewrite_CommandThatInsertsNothing_LeavesTheCommandAlone()
    {
        const string sql = "UPDATE [Widgets] SET [Name] = @p0 WHERE [Id] = @p1;";

        Assert.Null(SeedingIdentityRewriter.Rewrite(sql, this.identityTables));
    }

    [Fact]
    public void Rewrite_TwoIdentityTablesInOneCommand_ThrowsNamingBoth()
    {
        const string sql = """
            INSERT INTO [Widgets] ([Id], [Name])
            VALUES (@p0, @p1);
            INSERT INTO [Animals] ([Id], [Name], [Discriminator])
            VALUES (@p2, @p3, @p4);
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => SeedingIdentityRewriter.Rewrite(sql, this.identityTables));

        Assert.Contains("[Animals]", exception.Message);
        Assert.Contains("[Widgets]", exception.Message);
    }

    #endregion

    #region BuildTableMap

    [Fact]
    public void BuildTableMap_TphHierarchy_MapsTheRootTableOnce()
    {
        Assert.Equal(
            ["[Animals]", "[Widgets]", "[catalog].[Gadgets]"],
            this.identityTables.Keys.OrderBy(key => key, StringComparer.Ordinal));

        Assert.Equal("Id", this.identityTables["[Animals]"]);
    }

    [Fact]
    public void BuildTableMap_KeyThatIsNotAnIdentityColumn_IsExcluded()
    {
        Assert.DoesNotContain("[Tokens]", this.identityTables.Keys);
    }

    #endregion

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        for (var at = haystack.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private sealed class SeedModelContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseSqlServer("Server=model-only;Database=model-only;");

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Widget>().ToTable("Widgets");
            model.Entity<Token>().ToTable("Tokens");
            model.Entity<Gadget>().ToTable("Gadgets", "catalog");

            model.Entity<Animal>()
                .ToTable("Animals")
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Animal>("Animal")
                .HasValue<Dog>("Dog");
        }
    }

    private sealed class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private sealed class Token
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = null!;
    }

    private sealed class Gadget
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
    }

    private class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private sealed class Dog : Animal
    {
        public string Breed { get; set; } = null!;
    }
}
