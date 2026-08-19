namespace Concertable.ServiceDefaults;

/// <summary>
/// Named rate-limiting policy identifiers. Consumers reference these constants across the package
/// boundary — via <c>RequireRateLimiting</c> on an endpoint or a hub-side check — instead of magic
/// strings, so a policy rename fails to compile rather than silently detaching a limiter.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Apply = "apply";
    public const string Messaging = "messaging";
    public const string Upload = "upload";
}
