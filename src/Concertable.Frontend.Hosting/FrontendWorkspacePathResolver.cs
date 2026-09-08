namespace Concertable.Frontend.Hosting;

internal static class FrontendWorkspacePathResolver
{
    public static string Resolve(
        string appHostDirectory,
        IReadOnlyList<string[]> workspacePathCandidates) =>
        Resolve(appHostDirectory, workspacePathCandidates, Directory.Exists);

    internal static string Resolve(
        string appHostDirectory,
        IReadOnlyList<string[]> workspacePathCandidates,
        Func<string, bool> directoryExists)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appHostDirectory);
        ArgumentNullException.ThrowIfNull(workspacePathCandidates);
        ArgumentNullException.ThrowIfNull(directoryExists);

        if (workspacePathCandidates.Count == 0
            || workspacePathCandidates.Any(candidate => candidate.Length == 0))
        {
            throw new ArgumentException(
                "At least one non-empty frontend workspace path candidate is required.",
                nameof(workspacePathCandidates));
        }

        for (var directory = new DirectoryInfo(appHostDirectory); directory is not null; directory = directory.Parent)
        {
            foreach (var candidate in workspacePathCandidates)
            {
                var path = Path.Combine([directory.FullName, .. candidate]);
                if (directoryExists(path))
                    return path;
            }
        }

        var candidates = string.Join(", ", workspacePathCandidates.Select(candidate => Path.Combine(candidate)));
        throw new InvalidOperationException(
            $"Could not locate a frontend workspace ({candidates}) walking up from '{appHostDirectory}'.");
    }
}
