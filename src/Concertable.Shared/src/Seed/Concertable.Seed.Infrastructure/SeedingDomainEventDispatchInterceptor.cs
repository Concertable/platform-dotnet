using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Seed.Shared.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.Seed.Infrastructure;

public sealed class SeedingDomainEventDispatchInterceptor : SaveChangesInterceptor, IDomainEventDispatchInterceptor
{
    private readonly IDomainEventDispatcher dispatcher;
    private readonly IDbContextAccessor contextAccessor;
    private readonly SeedingScope seedingScope;

    private readonly Stack<List<IDomainEvent>> pendingEventsStack = new();

    public SeedingDomainEventDispatchInterceptor(
        IDomainEventDispatcher dispatcher,
        IDbContextAccessor contextAccessor,
        SeedingScope seedingScope)
    {
        this.dispatcher = dispatcher;
        this.contextAccessor = contextAccessor;
        this.seedingScope = seedingScope;
    }

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

        if (!seedingScope.IsActive)
            await DispatchPreCommitAsync(eventData.Context, pendingEvents, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var pendingEvents = pendingEventsStack.Pop();

        if (seedingScope.IsActive)
            await DispatchPreCommitAsync(eventData.Context!, pendingEvents, cancellationToken);

        await dispatcher.DispatchAsync(pendingEvents, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchPreCommitAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        List<IDomainEvent> pendingEvents,
        CancellationToken cancellationToken)
    {
        var previous = contextAccessor.Context;
        contextAccessor.Context = context;
        try
        {
            await dispatcher.DispatchPreCommitAsync(pendingEvents, cancellationToken);
        }
        finally
        {
            contextAccessor.Context = previous;
        }
    }
}
