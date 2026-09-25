using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Concertable.Testing.Integration;

public static class RateLimitingTestConfig
{
    extension(IConfigurationBuilder config)
    {
        public IConfigurationBuilder RelaxRateLimiting(IEnumerable<string> policyNames)
            => config.ConstrainRateLimiting(policyNames, int.MaxValue);

        public IConfigurationBuilder ConstrainRateLimiting(IEnumerable<string> policyNames, int permitLimit)
        {
            var overrides = new Dictionary<string, string?>();
            foreach (var name in policyNames)
                overrides[$"RateLimiting:{name}:PermitLimit"] = permitLimit.ToString(CultureInfo.InvariantCulture);

            return config.AddInMemoryCollection(overrides);
        }
    }
}
