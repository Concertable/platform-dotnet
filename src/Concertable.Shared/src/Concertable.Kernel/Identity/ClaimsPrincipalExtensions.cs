using System.Security.Claims;

namespace Concertable.Kernel.Identity;

public static class ClaimsPrincipalExtensions
{
    public static string GetId(this ClaimsPrincipal user)
    {
        return user?.FindFirst("sub")?.Value ?? string.Empty;
    }
}
