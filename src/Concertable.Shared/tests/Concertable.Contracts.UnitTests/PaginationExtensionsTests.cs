using Concertable.Contracts;

namespace Concertable.Contracts.UnitTests;

public sealed class PaginationExtensionsTests
{
    private static Pagination<int> Page() => new([1, 2, 3], totalCount: 57, pageNumber: 2, pageSize: 3);

    [Fact]
    public void Map_ProjectsEveryItem()
    {
        var mapped = Page().Map(value => value * 10);

        Assert.Equal([10, 20, 30], mapped.Data);
    }

    [Fact]
    public void Map_CarriesThePagingMetadataAcross()
    {
        var mapped = Page().Map(value => value.ToString());

        Assert.Equal(57, mapped.TotalCount);
        Assert.Equal(2, mapped.PageNumber);
        Assert.Equal(3, mapped.PageSize);
    }

    [Fact]
    public void Map_OverAnEmptyPage_KeepsTheTotalCount()
    {
        var empty = new Pagination<int>([], totalCount: 57, pageNumber: 99, pageSize: 3);

        var mapped = empty.Map(value => value * 10);

        Assert.Empty(mapped.Data);
        Assert.Equal(57, mapped.TotalCount);
    }
}
