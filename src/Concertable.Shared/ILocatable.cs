using NetTopologySuite.Geometries;
using System.Linq.Expressions;

namespace Concertable.Shared;

public interface ILocatable<TSelf> where TSelf : class, IEntity
{
    static abstract Expression<Func<TSelf, Point?>> LocationExpression { get; }
}
