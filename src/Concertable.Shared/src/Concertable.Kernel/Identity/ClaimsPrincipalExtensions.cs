using System.Security.Claims;

namespace Concertable.Kernel.Identity;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public string? GetId() => user?.FindFirst("sub")?.Value;
    }
}
