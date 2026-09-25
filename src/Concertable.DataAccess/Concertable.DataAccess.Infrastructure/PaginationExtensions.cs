using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public static class PaginationExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public async Task<IPagination<T>> ToPaginationAsync(IPageParams pageParams, CancellationToken ct = default)
        {
            int totalCount = await query.CountAsync(ct);
            var data = await query
                .Skip((pageParams.PageNumber - 1) * pageParams.PageSize)
                .Take(pageParams.PageSize)
                .ToListAsync(ct);

            return new Pagination<T>(data, totalCount, pageParams.PageNumber, pageParams.PageSize);
        }
    }
}
