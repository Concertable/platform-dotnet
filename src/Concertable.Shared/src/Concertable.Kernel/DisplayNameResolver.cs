using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Concertable.Kernel;

/// <summary>Resolves a type's human-readable name from its <see cref="DisplayNameAttribute"/> for
/// self-naming "not found" messages. Cached per <see cref="Type"/>: one reflection walk per distinct
/// type, ever — sits on the already-exceptional <c>OrNotFound</c> throw path.</summary>
public static class DisplayNameResolver
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    public static string Of<T>() => Cache.GetOrAdd(typeof(T), Resolve);

    private static string Resolve(Type t)
        => t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
           ?? throw new InvalidOperationException(
               $"{t.Name} has no [DisplayName]; add one so OrNotFound can name it.");
}
