namespace Concertable.Testing;

public static class DirectoryInfoExtensions
{
    extension(DirectoryInfo directory)
    {
        public DirectoryInfo NearestSolutionDirectory =>
            directory.AncestorsAndSelf()
                .First(candidate => candidate.EnumerateFiles("*.slnx").Any());

        public IEnumerable<DirectoryInfo> AncestorsAndSelf()
        {
            for (var candidate = directory; candidate is not null; candidate = candidate.Parent)
                yield return candidate;
        }
    }
}
