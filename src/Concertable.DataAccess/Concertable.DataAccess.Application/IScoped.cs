namespace Concertable.DataAccess.Application;

/// <summary>
/// Executes an action against a <typeparamref name="T"/> resolved inside a fresh dependency-injection
/// scope. Use from contexts that have no ambient scope of their own — singletons, hosted/background
/// services, timer triggers, message-loop roots — where a scoped service (and the <c>DbContext</c> it
/// depends on) cannot be constructor-injected directly. Each call opens its own scope, resolves
/// <typeparamref name="T"/> from it, and disposes the scope when the action completes, so every call is
/// an isolated unit of work with its own change tracker and transaction.
/// </summary>
/// <remarks>
/// Do not use this from inside an existing request scope (e.g. a controller or another scoped service):
/// it would open a second, separate scope whose <c>DbContext</c> diverges from the ambient one, so writes
/// would not share the surrounding transaction. In those contexts inject <typeparamref name="T"/> directly.
/// </remarks>
/// <typeparam name="T">The scoped service to resolve and act on. Must be registered in the container.</typeparam>
public interface IScoped<T> where T : notnull
{
    /// <summary>
    /// Opens a fresh scope, resolves <typeparamref name="T"/>, and runs <paramref name="action"/> against it.
    /// </summary>
    /// <param name="action">The work to perform with the scoped service.</param>
    /// <returns>A task that completes once <paramref name="action"/> finishes and the scope is disposed.</returns>
    Task RunAsync(Func<T, Task> action);

    /// <summary>
    /// Opens a fresh scope, resolves <typeparamref name="T"/>, runs <paramref name="action"/> against it,
    /// and returns the action's result.
    /// </summary>
    /// <typeparam name="TResult">The value produced by <paramref name="action"/>.</typeparam>
    /// <param name="action">The work to perform with the scoped service.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    Task<TResult> RunAsync<TResult>(Func<T, Task<TResult>> action);
}
