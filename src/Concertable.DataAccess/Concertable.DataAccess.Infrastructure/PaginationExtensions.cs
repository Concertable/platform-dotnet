using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public static class PaginationExtensions
{
    public static async Task<IPagination<T>> ToPaginationAsync<T>(
        this IQueryable<T> query, IPageParams pageParams)
    {
        int totalCount = await query.CountAsync();
        var data = await query
            .Skip((pageParams.PageNumber - 1) * pageParams.PageSize)
            .Take(pageParams.PageSize)
            .ToListAsync();

        return new Pagination<T>(data, totalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    /// <summary>
    /// Superseded by <c>Concertable.Contracts.PaginationExtensions.Map</c>, which lives beside
    /// <see cref="IPagination{T}"/> where every layer — including <c>*.Api</c>, which cannot reference
    /// this package — can reach it. Kept only until consumers move onto the published <c>Map</c>; the
    /// pin has to carry it before a call site can migrate.
    /// </summary>
    public static IPagination<TDestination> Select<TSource, TDestination>(
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
