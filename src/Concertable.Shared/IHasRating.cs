namespace Concertable.Shared;

public interface IHasRating
{
    int Id { get; set; }
    double? Rating { get; set; }
}
