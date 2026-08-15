namespace Concertable.Contracts;

/// <summary>
/// One page of results plus the counts describing the whole set. Deliberately not
/// <see cref="System.Collections.Generic.IEnumerable{T}"/>: the type carries two different sizes, so
/// enumerating it directly would let <c>Count()</c> read as the whole result set when it is only this
/// page. Read the items through <see cref="Data"/>, and project a whole page with <c>Map</c>.
/// </summary>
public interface IPagination<out T>
{
    IReadOnlyList<T> Data { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    int PageNumber { get; }
    int PageSize { get; }
}
