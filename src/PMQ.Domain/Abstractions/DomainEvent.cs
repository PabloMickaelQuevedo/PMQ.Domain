namespace PMQ.Domain;

/// <summary>
/// Base implementation of <see cref="IDomainEvent"/>.
/// </summary>
/// <remarks>
/// A <see langword="record"/> so events are immutable and compared by value. Derive from it
/// declaring only the data of the fact:
/// <code>public sealed record OrderPlacedDomainEvent(Guid OrderId) : DomainEvent;</code>
/// </remarks>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
