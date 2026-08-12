namespace Concertable.Kernel.Exceptions;

public abstract class DependencyException : Exception
{
    protected DependencyException(
        string dependencyName,
        string message,
        Exception? innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyName);
        this.DependencyName = dependencyName;
    }

    public string DependencyName { get; }
}

public sealed class DependencyUnavailableException : DependencyException
{
    public DependencyUnavailableException(
        string dependencyName,
        Exception? innerException = null)
        : base(
            dependencyName,
            $"Dependency '{dependencyName}' is unavailable.",
            innerException) { }
}

public sealed class DependencyTimeoutException : DependencyException
{
    public DependencyTimeoutException(
        string dependencyName,
        Exception? innerException = null)
        : base(
            dependencyName,
            $"Dependency '{dependencyName}' did not respond before its deadline.",
            innerException) { }
}
