namespace Concertable.Kernel;

public interface IHasRating
{
    int Id { get; }
    double? Rating { get; }
}
