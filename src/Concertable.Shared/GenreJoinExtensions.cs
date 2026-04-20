namespace Concertable.Shared;

public static class GenreJoinExtensions
{
    public static void SyncGenres<TGenreJoin>(
        this IHasGenreJoins<TGenreJoin> entity,
        IEnumerable<int> genreIds)
        where TGenreJoin : class, IGenreJoin, new()
    {
        var newGenreIds = genreIds.ToHashSet();

        if (newGenreIds.Count == 0)
        {
            entity.GenreJoins.Clear();
            return;
        }

        entity.GenreJoins.RemoveWhere(x => !newGenreIds.Contains(x.GenreId));

        var existingGenreIds = entity.GenreJoins
            .Select(x => x.GenreId)
            .ToHashSet();

        foreach (var genreId in newGenreIds)
        {
            if (!existingGenreIds.Contains(genreId))
                entity.GenreJoins.Add(new TGenreJoin { GenreId = genreId });
        }
    }
}
