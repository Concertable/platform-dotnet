using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class DisplayNameResolverTests
{
    [Fact]
    public void Of_AttributeOnClass_ResolvesName()
    {
        Assert.Equal("Widget", DisplayNameResolver.Of<AnnotatedClass>());
    }

    [Fact]
    public void Of_AttributeOnBaseClass_ResolvesViaInheritance()
    {
        // GetCustomAttribute defaults to inherit:true — a derived type inherits its base's name.
        Assert.Equal("Widget", DisplayNameResolver.Of<DerivedWidget>());
    }

    [Fact]
    public void Of_MissingAttribute_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => DisplayNameResolver.Of<Unannotated>());
        Assert.Contains(nameof(Unannotated), ex.Message);
    }

    [Fact]
    public void Of_SameType_CachesResolvedName()
    {
        DisplayNameResolver.Of<CacheProbe>();

        var cache = (ConcurrentDictionary<Type, string>)typeof(DisplayNameResolver)
            .GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        Assert.True(cache.ContainsKey(typeof(CacheProbe)));
        Assert.Equal("Cacheable", cache[typeof(CacheProbe)]);
        Assert.Equal("Cacheable", DisplayNameResolver.Of<CacheProbe>());
    }

    [DisplayName("Widget")]
    private class AnnotatedClass;

    private sealed class DerivedWidget : AnnotatedClass;

    private sealed class Unannotated;

    [DisplayName("Cacheable")]
    private sealed class CacheProbe;
}
