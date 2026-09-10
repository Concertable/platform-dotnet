using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NetTopologySuite.Geometries;

namespace Concertable.DataAccess.UnitTests;

public sealed class PropertyBuilderExtensionsTests
{
    [Fact]
    public void HasGeographyColumn_PointProperty_SetsGeographyColumnType()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        var property = modelBuilder.Entity<SpatialEntity>().Property(entity => entity.Location);

        property.HasGeographyColumn();

        Assert.Equal("geography", property.Metadata.GetColumnType());
    }

    private sealed class SpatialEntity
    {
        public Point? Location { get; set; }
    }
}
