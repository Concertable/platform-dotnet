using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Concertable.AppHost.Shared.UnitTests;

public sealed class PostgresContainerResourceTests
{
    #region AddPostgresContainer

    [Fact]
    public void AddPostgresContainer_DefaultVolumeName_SuffixesTheVolumePerCheckout()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgresContainer();

        var mount = Assert.Single(postgres.Resource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.StartsWith("concertable-postgres-data-", mount.Source, StringComparison.Ordinal);
        Assert.NotEqual("concertable-postgres-data-", mount.Source);
    }

    [Fact]
    public void AddPostgresContainer_NamedVolume_SuffixesThatNameInstead()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgresContainer("search-data");

        var mount = Assert.Single(postgres.Resource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.StartsWith("search-data-", mount.Source, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgresContainer_NonSpatialComposition_RunsPostgreSql17()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgresContainer();

        var image = postgres.Resource.Annotations.OfType<ContainerImageAnnotation>().Last();
        Assert.Equal("library/postgres", image.Image);
        Assert.StartsWith("17.", image.Tag, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDatabase_KeepsTheNameTheCompositionAsksFor()
    {
        var builder = DistributedApplication.CreateBuilder();

        IResourceBuilder<PostgresDatabaseResource> database =
            builder.AddPostgresContainer().AddDatabase("SearchDb");

        Assert.Equal("SearchDb", database.Resource.DatabaseName);
    }

    #endregion

    #region WithPostGis

    [Fact]
    public void WithPostGis_SpatialComposition_RunsThePostGisImage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgresContainer().WithPostGis();

        var image = postgres.Resource.Annotations.OfType<ContainerImageAnnotation>().Last();
        Assert.Equal("postgis/postgis", image.Image);
        Assert.Equal("17-3.5", image.Tag);
    }

    [Fact]
    public void WithPostGis_AfterAddPostgresContainer_KeepsThePerCheckoutVolume()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres = builder.AddPostgresContainer().WithPostGis();

        var mount = Assert.Single(postgres.Resource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.StartsWith("concertable-postgres-data-", mount.Source, StringComparison.Ordinal);
    }

    #endregion
}
