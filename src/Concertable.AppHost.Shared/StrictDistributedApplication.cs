using Aspire.Hosting;

public static class StrictDistributedApplication
{
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var environmentIndex = Array.FindIndex(args, argument => string.Equals(argument, "--environment", StringComparison.OrdinalIgnoreCase));
        var inlineEnvironment = args.FirstOrDefault(argument => argument.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase));
        var environment = environmentIndex >= 0 && environmentIndex + 1 < args.Length
            ? args[environmentIndex + 1]
            : inlineEnvironment?.Split('=', 2)[1];
        if (environment is not null && !string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Concertable AppHosts require the Development environment for strict service-provider validation.");
        var environmentArgs = environment is null ? [.. args, "--environment", "Development"] : args;
        return DistributedApplication.CreateBuilder(environmentArgs);
    }
}
