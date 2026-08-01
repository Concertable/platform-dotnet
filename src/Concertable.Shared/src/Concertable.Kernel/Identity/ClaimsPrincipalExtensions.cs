using System.Security.Claims;

namespace Concertable.Kernel.Identity;

public static class ClaimsPrincipalExtensions
{
    public static string? GetId(this ClaimsPrincipal user) => user?.FindFirst("sub")?.Value;
}
