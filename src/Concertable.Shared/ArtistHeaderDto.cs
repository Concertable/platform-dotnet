namespace Concertable.Shared;

public record ArtistHeaderDto : IHeader, IAddress
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public double? Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public IEnumerable<GenreDto> Genres { get; set; } = [];
}
