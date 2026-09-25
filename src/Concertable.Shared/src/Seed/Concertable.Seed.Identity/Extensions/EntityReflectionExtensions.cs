using System.Reflection;

namespace Concertable.Seed.Identity.Extensions;

public static class EntityReflectionExtensions
{
    public static T New<T>() where T : class
        => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    extension<T>(T entity)
        where T : class
    {
        public T WithId(object id)
            => entity.With("Id", id);

        public T With(string propertyName, object? value)
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
}
