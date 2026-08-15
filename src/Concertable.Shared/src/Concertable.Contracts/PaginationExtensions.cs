namespace Concertable.Contracts;

public static class PaginationExtensions
{
    /// <summary>
    /// Projects each item of a page, carrying <see cref="IPagination{T}.TotalCount"/>,
    /// <see cref="IPagination{T}.PageNumber"/> and <see cref="IPagination{T}.PageSize"/> across unchanged.
    /// Named <c>Map</c>, not <c>Select</c>: it preserves the carrier rather than behaving like LINQ's lazy,
    /// composable projection over a bare sequence.
    /// </summary>
    public static IPagination<TDestination> Map<TSource, TDestination>(
        this IPagination<TSource> source,
        Func<TSource, TDestination> selector)
    {
        return new Pagination<TDestination>(
            source.Data.Select(selector).ToList(),
            source.TotalCount,
            source.PageNumber,
            source.PageSize);
    }
}
