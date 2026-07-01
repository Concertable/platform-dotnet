using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.Testing.Integration;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-Sub";
    public const string RoleHeader = "X-Test-Role";
    public const string EmailHeader = "X-Test-Email";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new("sub", userIdValues.ToString())
        };

        if (Request.Headers.TryGetValue(RoleHeader, out var roleValues))
            claims.Add(new Claim("role", roleValues.ToString()));

        if (Request.Headers.TryGetValue(EmailHeader, out var emailValues))
            claims.Add(new Claim("email", emailValues.ToString()));

        var identity = new ClaimsIdentity(claims, SchemeName, "sub", "role");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
