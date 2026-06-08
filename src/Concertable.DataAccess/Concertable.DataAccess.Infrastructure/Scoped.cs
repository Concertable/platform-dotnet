using Concertable.DataAccess.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.DataAccess.Infrastructure;

public sealed class Scoped<T> : IScoped<T> where T : notnull
{
    private readonly IServiceScopeFactory scopeFactory;

    public Scoped(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task RunAsync(Func<T, Task> action)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<T>());
    }

    public async Task<TResult> RunAsync<TResult>(Func<T, Task<TResult>> action)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<T>());
    }
}
