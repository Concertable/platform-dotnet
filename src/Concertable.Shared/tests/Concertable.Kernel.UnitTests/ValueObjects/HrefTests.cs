using Concertable.Kernel.ValueObjects;

namespace Concertable.Kernel.UnitTests.ValueObjects;

public sealed class HrefTests
{
    [Theory]
    [InlineData("/api/application/42/accept", "/api/application/42/accept")]
    [InlineData("  /api/concert/7/contract/pdf  ", "/api/concert/7/contract/pdf")]
    [InlineData("/api/message?page=2&size=10", "/api/message?page=2&size=10")]
    public void From_RootRelativePath_NormalizesToTrimmed(string input, string expected)
    {
        Assert.Equal(expected, Href.From(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("api/application/42/accept")]
    [InlineData("https://example.com/api/application/42/accept")]
    [InlineData("//example.com/api/application/42/accept")]
    [InlineData("/api/../../etc/passwd")]
    public void From_NonRootRelativeOrTraversingPath_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => Href.From(input));
    }

    [Fact]
    public void TryFrom_RootRelativePath_ReturnsTrueAndNormalizes()
    {
        Assert.True(Href.TryFrom("  /api/venue/3  ", out var href));
        Assert.Equal("/api/venue/3", href.Value);
    }

    [Fact]
    public void TryFrom_AbsoluteUrl_ReturnsFalse()
    {
        Assert.False(Href.TryFrom("https://example.com/api/venue/3", out _));
    }

    [Fact]
    public void Equality_IsByCanonicalValue()
    {
        Assert.Equal(Href.From("/api/venue/3"), Href.From("  /api/venue/3  "));
    }
}
