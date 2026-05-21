using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.DataAccess.Infrastructure;

public class DomainEventDispatchInterceptor(
    IDomainEventDispatcher dispatcher,
    IDbContextAccessor contextAccessor) : SaveChangesInterceptor
{
    private List<IDomainEvent> _pendingEvents = [];

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _pendingEvents = eventData.Context!.ChangeTracker.Entries<IEventRaiser>()
            .SelectMany(e => e.Entity.DomainEvents).ToList();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IEventRaiser>())
            entry.Entity.ClearDomainEvents();

        var previous = contextAccessor.Context;
        contextAccessor.Context = eventData.Context;
        try
        {
            await dispatcher.DispatchPreCommitAsync(_pendingEvents, cancellationToken);
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
        await dispatcher.DispatchAsync(_pendingEvents, cancellationToken);
        _pendingEvents = [];

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
