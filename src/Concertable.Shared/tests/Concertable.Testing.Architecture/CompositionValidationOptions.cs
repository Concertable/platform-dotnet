using System.Reflection;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.Testing.Architecture;

public sealed class CompositionValidationOptions
{
    public IReadOnlyCollection<Assembly> RootAssemblies { get; init; } = [];

    public IReadOnlyCollection<Type> HandlerServiceDefinitions { get; init; } =
    [
        typeof(IDomainEventHandler<>),
        typeof(IPreCommitDomainEventHandler<>),
        typeof(IIntegrationCommandHandler<>),
        typeof(IIntegrationEventHandler<>)
    ];

    public Func<MethodInfo, bool>? IsFunction { get; init; }
}
