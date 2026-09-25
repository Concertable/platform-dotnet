using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;

namespace Concertable.Kernel.Exceptions;

public sealed class NotFoundException : HttpException
{
    public NotFoundException(string detail) : base(detail, HttpStatusCode.NotFound)
    {
        Title = "Not Found";
    }

    public static void ThrowIfNull(
        [NotNull] object? argument,
        string? message = null,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
            throw new NotFoundException(message ?? $"{paramName} was not found.");
    }
}

/// <summary>Inline "must exist or it's a 404" guards — the expression-returning companion to the
/// statement-form <see cref="NotFoundException.ThrowIfNull"/>. Returns the non-null value so it chains
/// inline. Must be an extension class (postfix on <c>Task</c>/nullable), so it lives beside the exception
/// rather than on it.</summary>
public static class NotFoundExtensions
{
    extension<T>(Task<T?> task)
        where T : class
    {
        // Self-naming — the type carries its own display name via [DisplayName]; ZERO string at the call site.
        public async Task<T> OrNotFound()
            => await task ?? throw new NotFoundException($"{DisplayNameResolver.Of<T>()} not found");

        // Explicit label — DTOs/projections + id-bearing/contextual messages (name is irreducible here).
        public async Task<T> OrNotFound(string entity)
            => await task ?? throw new NotFoundException($"{entity} not found");
    }

    extension<T>(Task<T?> task)
        where T : struct
    {
        // Value types — the sites a `where T : class` helper can't touch (Guid?/int? id projections).
        public async Task<T> OrNotFound(string entity)
            => await task ?? throw new NotFoundException($"{entity} not found");
    }
}
