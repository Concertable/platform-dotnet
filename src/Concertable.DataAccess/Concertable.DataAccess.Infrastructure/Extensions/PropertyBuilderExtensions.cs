using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class PropertyBuilderExtensions
{
    extension<TPoint>(PropertyBuilder<TPoint> builder)
        where TPoint : Point?
    {
        public PropertyBuilder<TPoint> HasGeographyColumn() =>
            builder.HasColumnType("geography");
    }
}
