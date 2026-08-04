using PMQ.Mediator;

namespace PMQ.Domain;

/// <summary>
/// A relevant fact that has already happened in the domain.
/// </summary>
/// <remarks>
/// Extends <see cref="INotification"/> so it can be dispatched by PMQ.Mediator to any number of
/// decoupled handlers. Publish it only after the transaction commits — a handler must never
/// observe a fact the transaction ended up rolling back.
/// </remarks>
public interface IDomainEvent : INotification
{
    /// <summary>Unique identifier of the event, useful for idempotency and tracing.</summary>
    Guid EventId { get; }

    /// <summary>The moment (UTC) the fact occurred.</summary>
    DateTimeOffset OccurredOn { get; }
}
