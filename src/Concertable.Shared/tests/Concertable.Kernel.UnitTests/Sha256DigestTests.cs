using System.Globalization;
using Concertable.Kernel.ValueObjects;

namespace Concertable.Kernel.UnitTests;

public sealed class Sha256DigestTests
{
    #region Of

    [Fact]
    public void Of_SeparatorInsideParts_DoesNotCollide()
    {
        var first = Sha256Digest.Of("a\u001fb", "c");

        var second = Sha256Digest.Of("a", "b\u001fc");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Of_NullAndLiteralSentinel_DoNotCollide()
    {
        var nullPart = Sha256Digest.Of((object?)null);

        var literal = Sha256Digest.Of("\u0000");

        Assert.NotEqual(nullPart, literal);
    }

    [Fact]
    public void Of_FormattableValue_IsCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            var british = Sha256Digest.Of(1234.56m);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            var french = Sha256Digest.Of(1234.56m);

            Assert.Equal(british, french);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Of_DateTimesForTheSameInstant_AreEqual()
    {
        var utc = new DateTime(2026, 9, 20, 12, 34, 56, DateTimeKind.Utc);

        var local = Sha256Digest.Of(utc.ToLocalTime());

        Assert.Equal(Sha256Digest.Of(utc), local);
    }

    [Fact]
    public void Of_NoParts_IsTheSha256OfNothing()
    {
        var digest = Sha256Digest.Of();

        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", digest.Value);
    }

    #endregion

    #region From

    [Fact]
    public void From_UppercaseDigest_CanonicalizesToTheComputedDigest()
    {
        var computed = Sha256Digest.Of("payload");

        var restored = Sha256Digest.From(computed.Value.ToUpperInvariant());

        Assert.Equal(computed, restored);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    [InlineData("é000000000000000000000000000000000000000000000000000000000000000")]
    public void From_NotADigest_Throws(string value)
    {
        Assert.Throws<DomainException>(() => Sha256Digest.From(value));
    }

    #endregion
}
