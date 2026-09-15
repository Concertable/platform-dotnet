namespace Concertable.Testing.Integration.UnitTests;

public sealed class DatabaseProviderSelectorTests
{
    private const DatabaseProvider Undeclared = (DatabaseProvider)99;

    #region Resolve

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_NothingConfigured_KeepsTheDeclaredDefault(string? configured) =>
        Assert.Equal(
            DatabaseProvider.SqlServer,
            DatabaseProviderSelector.Resolve(DatabaseProvider.SqlServer, configured));

    [Fact]
    public void Resolve_NothingConfigured_KeepsADeclaredPostgres() =>
        Assert.Equal(
            DatabaseProvider.Postgres,
            DatabaseProviderSelector.Resolve(DatabaseProvider.Postgres, configured: null));

    [Fact]
    public void Resolve_Configured_OverridesTheDeclaredDefault() =>
        Assert.Equal(
            DatabaseProvider.Postgres,
            DatabaseProviderSelector.Resolve(DatabaseProvider.SqlServer, "Postgres"));

    [Fact]
    public void Resolve_ConfiguredBackToTheDefault_OverridesADeclaredPostgres() =>
        Assert.Equal(
            DatabaseProvider.SqlServer,
            DatabaseProviderSelector.Resolve(DatabaseProvider.Postgres, "SqlServer"));

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("SqlServer,Postgres")]
    [InlineData("Oracle")]
    public void Resolve_ConfiguredWithAnythingButAName_Rejects(string configured) =>
        Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderSelector.Resolve(DatabaseProvider.SqlServer, configured));

    [Fact]
    public void Resolve_UndeclaredDefault_RejectsWithNothingConfigured() =>
        Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderSelector.Resolve(Undeclared, configured: null));

    [Fact]
    public void Resolve_UndeclaredDefault_RejectsThroughAValidOverride() =>
        Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderSelector.Resolve(Undeclared, "Postgres"));

    [Fact]
    public void Resolve_UndeclaredDefault_NamesTheValueAndEveryAllowedName()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderSelector.Resolve(Undeclared, configured: null));

        Assert.Contains("99", error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseProvider.SqlServer), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseProvider.Postgres), error.Message, StringComparison.Ordinal);
    }

    #endregion

    #region Parse

    [Theory]
    [InlineData("Postgres", DatabaseProvider.Postgres)]
    [InlineData("postgres", DatabaseProvider.Postgres)]
    [InlineData("SQLSERVER", DatabaseProvider.SqlServer)]
    [InlineData("  Postgres  ", DatabaseProvider.Postgres)]
    public void Parse_KnownName_IgnoresCaseAndSurroundingSpace(string value, DatabaseProvider expected) =>
        Assert.Equal(expected, DatabaseProviderSelector.Parse(value));

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("99")]
    public void Parse_TheNumberBehindAName_Rejects(string value) =>
        Assert.Throws<InvalidOperationException>(() => DatabaseProviderSelector.Parse(value));

    [Theory]
    [InlineData("SqlServer,Postgres")]
    [InlineData("SqlServer, Postgres")]
    [InlineData("0,1")]
    public void Parse_SeveralNamesAtOnce_Rejects(string value) =>
        Assert.Throws<InvalidOperationException>(() => DatabaseProviderSelector.Parse(value));

    [Theory]
    [InlineData("Oracle")]
    [InlineData("Sql Server")]
    [InlineData("PostgreSQL")]
    public void Parse_UnknownName_Rejects(string value) =>
        Assert.Throws<InvalidOperationException>(() => DatabaseProviderSelector.Parse(value));

    [Fact]
    public void Parse_UnknownName_NamesTheVariableAndEveryAllowedValue()
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseProviderSelector.Parse("Oracle"));

        Assert.Contains(DatabaseProviderSelector.ProviderVariable, error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseProvider.SqlServer), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseProvider.Postgres), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_UnknownName_QuotesTheOffendingValue()
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseProviderSelector.Parse("Oracle"));

        Assert.Contains("'Oracle'", error.Message, StringComparison.Ordinal);
    }

    #endregion
}
