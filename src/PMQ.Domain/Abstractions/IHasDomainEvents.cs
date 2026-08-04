namespace PMQ.Domain;

/// <summary>
/// Non-generic contract for objects that accumulate domain events.
/// </summary>
/// <remarks>
/// Lets infrastructure collect events from any aggregate regardless of its identifier type,
/// without reflection. With Entity Framework Core, for instance:
/// <code>context.ChangeTracker.Entries&lt;IHasDomainEvents&gt;()</code>
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>Domain events awaiting publication.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Drops the pending events. Called by the unit of work after publishing them.</summary>
    void ClearDomainEvents();
}
