using PMQ.Notifications;

namespace PMQ.Domain;

/// <summary>
/// Base class for domain entities: own identity, notification-based validation and pending
/// domain events.
/// </summary>
/// <typeparam name="TId">Type of the entity identifier.</typeparam>
/// <remarks>
/// <para>
/// Inherits <see cref="Validatable"/>: a broken invariant becomes a notification accumulated on
/// the entity, never an exception. Exceptions are for programming errors — a violated business
/// rule is an expected outcome, and unwinding the stack for it both costs more and reports only
/// the first problem instead of all of them.
/// </para>
/// <para>
/// Equality is by identity, not by value, per the DDD definition of an entity. Two instances
/// with the same <see cref="Id"/> are the same entity even if every other field differs.
/// For value semantics use <see cref="ValueObject"/> or a <see langword="record"/>.
/// </para>
/// </remarks>
public abstract class Entity<TId> : Validatable, IHasDomainEvents, IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Parameterless constructor required by ORMs such as Entity Framework Core.</summary>
    protected Entity()
    {
    }

    /// <summary>Creates the entity with its definitive identity.</summary>
    /// <param name="id">The entity identifier.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>The entity identifier.</summary>
    public TId Id { get; private set; } = default!;

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Records an event to be published once the transaction commits.
    /// </summary>
    /// <param name="domainEvent">The event that occurred.</param>
    /// <remarks>
    /// Call this only after the invariants hold: an invalid aggregate must not announce a fact
    /// that never happened.
    /// </remarks>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
        => other is not null
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Compares two entities by identity.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left?.Equals(right) ?? right is null;

    /// <summary>Compares two entities by identity.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
