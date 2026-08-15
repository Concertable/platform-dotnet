namespace Concertable.Contracts;

/// <summary>One page of results plus the counts describing the whole set. Project a whole page onto
/// another item type with <c>Map</c>.</summary>
public interface IPagination<out T>
{
    IReadOnlyList<T> Data { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    int PageNumber { get; }
    int PageSize { get; }
}
