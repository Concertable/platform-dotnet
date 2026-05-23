using Concertable.Kernel;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.DataAccess.Infrastructure.Data;

public class DomainEventDispatchInterceptor(
    IDomainEventDispatcher dispatcher,
    IDbContextAccessor contextAccessor) : SaveChangesInterceptor
{
    private readonly Stack<List<IDomainEvent>> pendingEventsStack = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var pendingEvents = eventData.Context!.ChangeTracker.Entries<IEventRaiser>()
            .SelectMany(e => e.Entity.DomainEvents).ToList();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IEventRaiser>())
            entry.Entity.ClearDomainEvents();

        pendingEventsStack.Push(pendingEvents);

        var previous = contextAccessor.Context;
        contextAccessor.Context = eventData.Context;
        try
        {
            await dispatcher.DispatchPreCommitAsync(pendingEvents, cancellationToken);
        }
        finally
        {
            contextAccessor.Context = previous;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var pendingEvents = pendingEventsStack.Pop();
        await dispatcher.DispatchAsync(pendingEvents, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
