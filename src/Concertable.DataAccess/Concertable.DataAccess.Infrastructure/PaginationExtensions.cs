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
}
