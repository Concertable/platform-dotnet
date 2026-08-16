namespace Concertable.Contracts;

public static class PaginationExtensions
{
    extension<TSource>(IPagination<TSource> source)
    {
        /// <summary>
        /// Projects each item of a page, carrying <see cref="IPagination{T}.TotalCount"/>,
        /// <see cref="IPagination{T}.PageNumber"/> and <see cref="IPagination{T}.PageSize"/> across
        /// unchanged. Named <c>Map</c>, not <c>Select</c>: this is eager, returns a different container,
        /// and composes with nothing, so it is not LINQ's lazy composable projection. <c>Map</c> matches
        /// how the rest of the codebase names "transform the payload, preserve the carrier".
        /// </summary>
        public IPagination<TDestination> Map<TDestination>(Func<TSource, TDestination> selector) =>
            new Pagination<TDestination>(
                source.Data.Select(selector).ToList(),
                source.TotalCount,
                source.PageNumber,
                source.PageSize);
    }
}
