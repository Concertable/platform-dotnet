namespace Concertable.Shared;

public interface IGenreJoin
{
    int GenreId { get; set; }
}

public interface IHasGenreJoins<TGenreJoin>
    where TGenreJoin : class, IGenreJoin, new()
{
    HashSet<TGenreJoin> GenreJoins { get; }
}
