using Concertable.Kernel.ValueObjects;

namespace Concertable.Kernel.UnitTests.ValueObjects;

public sealed class HrefTests
{
    [Theory]
    [InlineData("/api/application/42/accept", "/api/application/42/accept")]
    [InlineData("  /api/concert/7/contract/pdf  ", "/api/concert/7/contract/pdf")]
    [InlineData("/api/message?page=2&size=10", "/api/message?page=2&size=10")]
    [InlineData("/", "/")]
    public void From_RootRelativePath_NormalizesToTrimmed(string input, string expected)
    {
        Assert.Equal(expected, Href.From(input).Value);
    }

    [Theory]
    [InlineData("/api/files?path=/a/../b")]
    [InlineData("/api/concert/7#notes/..")]
    public void From_ParentSegmentOutsideThePath_IsAccepted(string input)
    {
        Assert.Equal(input, Href.From(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("api/application/42/accept")]
    [InlineData("https://example.com/api/application/42/accept")]
    public void From_NotRootRelative_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => Href.From(input));
    }

    [Theory]
    [InlineData("//example.com/api/application/42/accept")]
    [InlineData("/\\example.com/api/application")]
    [InlineData("/\t/example.com")]
    [InlineData("/\r/example.com")]
    [InlineData("/\n/example.com")]
    public void From_ValueThatResolvesCrossOrigin_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => Href.From(input));
    }

    [Theory]
    [InlineData("/api/x\u0001y")]
    [InlineData("/api/x y")]
    public void From_ControlCharacterOrSpace_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => Href.From(input));
    }

    [Theory]
    [InlineData("/api/../../etc/passwd")]
    [InlineData("/api/%2e%2e/admin")]
    [InlineData("/api/%2E%2E/admin")]
    public void From_TraversingPath_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => Href.From(input));
    }

    [Fact]
    public void TryFrom_RootRelativePath_ReturnsTrueAndNormalizes()
    {
        Assert.True(Href.TryFrom("  /api/venue/3  ", out var href));
        Assert.Equal("/api/venue/3", href.Value);
    }

    [Theory]
    [InlineData("https://example.com/api/venue/3")]
    [InlineData("/\\example.com")]
    [InlineData("/api/%2e%2e/admin")]
    public void TryFrom_InvalidHref_ReturnsFalse(string input)
    {
        Assert.False(Href.TryFrom(input, out _));
    }

    [Fact]
    public void Equality_IsByCanonicalValue()
    {
        Assert.Equal(Href.From("/api/venue/3"), Href.From("  /api/venue/3  "));
    }
}
