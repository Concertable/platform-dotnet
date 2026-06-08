namespace Concertable.Messaging.Contracts;

/// <summary>
/// Dispatches integration messages on behalf of application code. Delivery strategy is
/// implementation-defined: <c>Bus</c> forwards directly to the <c>IBusTransport</c>; <c>OutboxBus</c>
/// persists to the transactional outbox for deferred dispatch. Each host registers exactly one
/// implementation.
/// </summary>
public interface IBus
{
    /// <summary>
    /// Publishes an integration event using fan-out (topic) delivery: every handler subscribed to
    /// <typeparamref name="TEvent"/>, in every subscribed service, receives an independent copy.
    /// Zero subscribers is valid. Event types use past-tense naming (e.g. <c>PaymentSucceededEvent</c>).
    /// </summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Sends an integration command using point-to-point (queue) delivery: the command is consumed exactly
    /// once by the single registered <c>IIntegrationCommandHandler{TCommand}</c>; a missing handler
    /// registration is an error. Command types use imperative naming (e.g. <c>ProcessStripeWebhookCommand</c>).
    /// </summary>
    /// <typeparam name="TCommand">The integration command type.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : IIntegrationCommand;
}
