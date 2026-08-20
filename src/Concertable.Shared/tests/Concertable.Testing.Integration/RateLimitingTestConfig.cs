using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Concertable.Testing.Integration;

public static class RateLimitingTestConfig
{
    public static IConfigurationBuilder RelaxRateLimiting(this IConfigurationBuilder config, IEnumerable<string> policyNames)
        => ConstrainRateLimiting(config, policyNames, int.MaxValue);

    public static IConfigurationBuilder ConstrainRateLimiting(this IConfigurationBuilder config, IEnumerable<string> policyNames, int permitLimit)
    {
        var overrides = new Dictionary<string, string?>();
        foreach (var name in policyNames)
            overrides[$"RateLimiting:{name}:PermitLimit"] = permitLimit.ToString(CultureInfo.InvariantCulture);

        return config.AddInMemoryCollection(overrides);
    }
}
