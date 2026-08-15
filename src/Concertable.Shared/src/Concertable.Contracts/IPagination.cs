namespace Concertable.Contracts;

/// <summary>
/// One page of results plus the counts describing the whole set. Deliberately **not**
/// <see cref="System.Collections.Generic.IEnumerable{T}"/>: this type carries two different sizes, and
/// making it enumerable would let <c>Count()</c> compile and silently mean the page size rather than
/// <see cref="TotalCount"/>, while <c>Where</c>/<c>Take</c> would quietly discard the paging metadata.
/// Read the items through <see cref="Data"/>, and project a whole page with <c>Map</c>.
/// </summary>
public interface IPagination<out T>
{
    IReadOnlyList<T> Data { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    int PageNumber { get; }
    int PageSize { get; }
}
