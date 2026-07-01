using NetTopologySuite.Geometries;

namespace Concertable.Kernel;

/// <summary>
/// Implementing this interface guarantees a non-null, geometry-queryable location.
/// Types whose location is genuinely optional must not implement it.
/// </summary>
public interface IHasLocation
{
    Point Location { get; set; }
}
