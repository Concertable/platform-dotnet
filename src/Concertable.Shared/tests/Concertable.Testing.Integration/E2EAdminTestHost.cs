using Concertable.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.Testing.Integration;

/// <summary>Hosts a service-owned E2E admin module in-process for integration coverage.</summary>
public sealed class E2EAdminTestHost : IAsyncDisposable
{
    private readonly WebApplication application;

    private E2EAdminTestHost(WebApplication application)
    {
        this.application = application;
        this.Client = application.GetTestClient();
    }

    public HttpClient Client { get; }

    public static WebApplicationBuilder CreateBuilder(
        string? adminKey,
        string connectionStringName,
        string? environmentName = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName ?? Environments.E2E,
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["E2E:AdminKey"] = adminKey,
            [$"ConnectionStrings:{connectionStringName}"] = "Server=e2e-admin-test",
        });
        builder.Services.AddSingleton<IDbInitializer, NoOpDbInitializer>();
        return builder;
    }

    public static async Task<E2EAdminTestHost> StartAsync(
        string adminKey,
        string connectionStringName,
        Action<IServiceCollection, IConfiguration, IHostEnvironment> registerAdmin,
        Action<WebApplication> mapAdmin,
        CancellationToken cancellationToken = default)
    {
        var builder = CreateBuilder(adminKey, connectionStringName);
        registerAdmin(builder.Services, builder.Configuration, builder.Environment);
        var application = builder.Build();
        mapAdmin(application);
        application.MapFallback(() => Results.StatusCode(StatusCodes.Status418ImATeapot));

        try
        {
            await application.StartAsync(cancellationToken);
            return new E2EAdminTestHost(application);
        }
        catch
        {
            await application.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        this.Client.Dispose();
        await this.application.DisposeAsync();
    }

    private sealed class NoOpDbInitializer : IDbInitializer
    {
        public Task InitializeAsync() => Task.CompletedTask;
    }
}
