using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Concertable.AppHost.Shared.UnitTests;

public sealed class SqlServerContainerResourceTests
{
    #region AddSqlServerContainer

    [Fact]
    public void AddSqlServerContainer_DefaultVolumeName_SuffixesTheVolumePerCheckout()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sql = builder.AddSqlServerContainer();

        Assert.StartsWith("concertable-sql-data-", VolumeName(sql), StringComparison.Ordinal);
        Assert.NotEqual("concertable-sql-data-", VolumeName(sql));
    }

    [Fact]
    public void AddSqlServerContainer_TwiceInOneCheckout_SuffixesBothTheSameWay()
    {
        var first = DistributedApplication.CreateBuilder().AddSqlServerContainer();
        var second = DistributedApplication.CreateBuilder().AddSqlServerContainer();

        Assert.Equal(VolumeName(first), VolumeName(second));
    }

    [Fact]
    public void AddSqlServerContainer_NamedVolume_SuffixesThatNameInstead()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sql = builder.AddSqlServerContainer("payment-data");

        Assert.StartsWith("payment-data-", VolumeName(sql), StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerContainer_AlongsideAPostgresContainer_SharesTheOneCheckoutSuffix()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sql = builder.AddSqlServerContainer("payment-sql-data");
        var postgres = builder.AddPostgresContainer("payment-postgres-data");

        Assert.Equal(Suffix(VolumeName(sql)), Suffix(PostgresVolumeName(postgres)));
    }

    #endregion

    #region AddDatabase

    [Fact]
    public void AddDatabase_ANamedServiceDatabase_KeepsTheNameTheCompositionAsksFor()
    {
        var builder = DistributedApplication.CreateBuilder();

        IResourceBuilder<SqlServerDatabaseResource> database =
            builder.AddSqlServerContainer().AddDatabase("AuthDb");

        Assert.Equal("AuthDb", database.Resource.DatabaseName);
    }

    #endregion

    private static string VolumeName(IResourceBuilder<SqlServerServerResource> sql) =>
        Assert.Single(sql.Resource.Annotations.OfType<ContainerMountAnnotation>()).Source!;

    private static string PostgresVolumeName(IResourceBuilder<PostgresServerResource> postgres) =>
        Assert.Single(postgres.Resource.Annotations.OfType<ContainerMountAnnotation>()).Source!;

    private static string Suffix(string volumeName) => volumeName[(volumeName.LastIndexOf('-') + 1)..];
}
