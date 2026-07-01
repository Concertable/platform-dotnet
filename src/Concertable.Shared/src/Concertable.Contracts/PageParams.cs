namespace Concertable.Contracts;

public sealed class PageParams : IPageParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
