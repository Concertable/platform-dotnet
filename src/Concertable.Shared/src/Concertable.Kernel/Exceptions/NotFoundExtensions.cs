namespace Concertable.Kernel.Exceptions;

public static class NotFoundExtensions
{
    public static async Task<T> OrNotFound<T>(this Task<T?> task, string? entity = null) where T : class
        => await task ?? throw new NotFoundException($"{entity ?? DisplayName<T>()} not found");

    public static T OrNotFound<T>(this T? value, string? entity = null) where T : class
        => value ?? throw new NotFoundException($"{entity ?? DisplayName<T>()} not found");

    private static string DisplayName<T>()
    {
        var name = typeof(T).Name;
        return name.EndsWith("Entity", StringComparison.Ordinal) ? name[..^"Entity".Length] : name;
    }
}
