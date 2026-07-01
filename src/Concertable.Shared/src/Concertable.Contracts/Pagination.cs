namespace Concertable.Contracts;

public sealed class Pagination<T> : IPagination<T>
{
    public IReadOnlyList<T> Data { get; set; } = null!;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public Pagination(IReadOnlyList<T> data, int totalCount, int pageNumber, int pageSize)
    {
        Data = data;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
