using System.Collections;

namespace Concertable.Kernel;

/// <summary>
/// A distinct, insertion-ordered set of value-type elements that EF Core can persist as a primitive
/// collection. Duplicates are structurally impossible: <see cref="Add"/> is the only way a value
/// enters and it ignores one already present. Consumers see an <see cref="IReadOnlySet{T}"/>.
/// <para>
/// The <see cref="IList{T}"/> surface is explicit-interface only and exists solely for EF Core:
/// its primitive-collection materializer, change-tracker comparer and JSON reader all hard-cast the
/// collection to <c>IList&lt;T&gt;</c>, so <see cref="HashSet{T}"/> cannot be used
/// (dotnet/efcore#33115, #35502 — both open, Backlog, no fix). This type is the set-shaped stand-in:
/// map it with <c>builder.PrimitiveCollection(x =&gt; x.Property)</c> exactly like a <c>List&lt;T&gt;</c>.
/// </para>
/// </summary>
public sealed class EfSet<T> : IReadOnlySet<T>, IReadOnlyList<T>, IList<T>
    where T : struct
{
    private readonly List<T> items = [];

    public EfSet() { }

    public EfSet(IReadOnlyCollection<T> values)
    {
        foreach (var value in values)
            Add(value);
    }

    public int Count => items.Count;

    public T this[int index] => items[index];

    public bool Contains(T item) => items.Contains(item);

    public void Add(T item)
    {
        if (!items.Contains(item))
            items.Add(item);
    }

    public bool Remove(T item) => items.Remove(item);

    public void Clear() => items.Clear();

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool ICollection<T>.IsReadOnly => false;

    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

    T IList<T>.this[int index]
    {
        get => items[index];
        set => throw new NotSupportedException("EfSet is set-shaped; positional assignment is not supported.");
    }

    int IList<T>.IndexOf(T item) => items.IndexOf(item);

    void IList<T>.Insert(int index, T item) =>
        throw new NotSupportedException("EfSet is set-shaped; positional insert is not supported.");

    void IList<T>.RemoveAt(int index) => items.RemoveAt(index);

    public bool IsProperSubsetOf(IEnumerable<T> other) => AsHashSet().IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => AsHashSet().IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other) => AsHashSet().IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other) => AsHashSet().IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other) => AsHashSet().Overlaps(other);

    public bool SetEquals(IEnumerable<T> other) => AsHashSet().SetEquals(other);

    private HashSet<T> AsHashSet() => [.. items];
}

public static class EfSetExtensions
{
    extension<T>(IReadOnlyCollection<T> values)
        where T : struct
    {
        /// <summary>The distinct, order-preserving <see cref="EfSet{T}"/> of these values.</summary>
        public EfSet<T> ToEfSet() => new(values);
    }
}
