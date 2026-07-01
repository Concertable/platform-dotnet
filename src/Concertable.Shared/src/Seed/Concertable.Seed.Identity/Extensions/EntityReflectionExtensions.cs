using System.Reflection;

namespace Concertable.Seed.Identity.Extensions;

public static class EntityReflectionExtensions
{
    public static T New<T>() where T : class
        => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    public static T WithId<T>(this T entity, object id) where T : class
        => entity.With("Id", id);

    public static T With<T>(this T entity, string propertyName, object? value) where T : class
    {
        var type = typeof(T) as Type;
        while (type is not null)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (prop is not null)
            {
                prop.SetValue(entity, value);
                return entity;
            }
            type = type.BaseType;
        }
        throw new InvalidOperationException($"Property '{propertyName}' not found on {typeof(T).Name} or its base types.");
    }
}
