namespace Concertable.Kernel.UnitTests;

public sealed class EfSetTests
{
    private enum Colour { Red, Green, Blue, Amber }

    [Fact]
    public void Constructor_DuplicateValues_KeepsFirstOccurrenceOnlyInInsertionOrder()
    {
        var set = new EfSet<Colour>([Colour.Green, Colour.Red, Colour.Green, Colour.Blue, Colour.Red]);

        Assert.Equal([Colour.Green, Colour.Red, Colour.Blue], set);
    }

    [Fact]
    public void Add_ValueAlreadyPresent_IsIgnored()
    {
        var set = new EfSet<Colour>([Colour.Red]);

        set.Add(Colour.Red);
        set.Add(Colour.Blue);

        Assert.Equal([Colour.Red, Colour.Blue], set);
    }

    [Fact]
    public void SetOperations_ReflectContents()
    {
        var set = new EfSet<Colour>([Colour.Red, Colour.Blue]);

        Assert.True(set.Overlaps([Colour.Blue, Colour.Green]));
        Assert.True(set.IsSubsetOf([Colour.Red, Colour.Blue, Colour.Green]));
        Assert.True(set.SetEquals([Colour.Blue, Colour.Red]));
        Assert.False(set.Overlaps([Colour.Green]));
    }

    [Fact]
    public void PositionalMutation_ThroughIListSurface_Throws()
    {
        IList<Colour> set = new EfSet<Colour>([Colour.Red]);

        Assert.Throws<NotSupportedException>(() => set.Insert(0, Colour.Blue));
        Assert.Throws<NotSupportedException>(() => set[0] = Colour.Blue);
    }

    [Fact]
    public void ToEfSet_DedupesAndPreservesOrder()
    {
        IReadOnlyCollection<Colour> source = [Colour.Amber, Colour.Red, Colour.Amber];

        Assert.Equal([Colour.Amber, Colour.Red], source.ToEfSet());
    }
}
