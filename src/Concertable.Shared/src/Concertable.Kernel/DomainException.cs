using System.Diagnostics.CodeAnalysis;

namespace Concertable.Kernel;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public static void ThrowIfNull([NotNull] object? value, string name)
    {
        if (value is null)
            throw new DomainException($"{name} is required.");
    }

    public static void ThrowIfNullOrWhiteSpace([NotNull] string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required.");
    }
}
