using Concertable.Kernel.ValueObjects;

namespace Concertable.Kernel.UnitTests.ValueObjects;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("  User@Example.COM  ", "user@example.com")]
    [InlineData("Firstname.Lastname@Sub.Example.co.uk", "firstname.lastname@sub.example.co.uk")]
    public void From_ValidEmail_NormalizesToTrimmedLowercase(string input, string expected)
    {
        Assert.Equal(expected, EmailAddress.From(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    [InlineData("two@at@signs.com")]
    [InlineData("Name <real@example.com>")]
    public void From_InvalidEmail_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => EmailAddress.From(input));
    }

    [Fact]
    public void Equality_IsCaseInsensitiveViaCanonicalForm()
    {
        Assert.Equal(EmailAddress.From("Person@Example.com"), EmailAddress.From("person@example.com"));
    }

    [Fact]
    public void TryFrom_ValidEmail_ReturnsTrueAndNormalizes()
    {
        Assert.True(EmailAddress.TryFrom("  A@B.com ", out var email));
        Assert.Equal("a@b.com", email.Value);
    }

    [Fact]
    public void TryFrom_InvalidEmail_ReturnsFalse()
    {
        Assert.False(EmailAddress.TryFrom("nope", out _));
    }
}
