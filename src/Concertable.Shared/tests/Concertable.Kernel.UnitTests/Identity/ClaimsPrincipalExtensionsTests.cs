using System.Security.Claims;
using Concertable.Kernel.Identity;

namespace Concertable.Kernel.UnitTests.Identity;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetId_WithSubClaim_ReturnsSubValue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-123")]));

        Assert.Equal("user-123", principal.GetId());
    }

    [Fact]
    public void GetId_WithoutSubClaim_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Null(principal.GetId());
    }
}
