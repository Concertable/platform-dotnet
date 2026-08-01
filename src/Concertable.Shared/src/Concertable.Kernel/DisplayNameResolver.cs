using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Concertable.Kernel;

/// <summary>Resolves and caches a type's required <see cref="DisplayNameAttribute"/>.</summary>
public static class DisplayNameResolver
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    public static string Of<T>() => Cache.GetOrAdd(typeof(T), Resolve);

    private static string Resolve(Type t)
        => t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
           ?? throw new InvalidOperationException(
               $"{t.Name} has no [DisplayName]; add one so caller-facing errors can name it.");
}
