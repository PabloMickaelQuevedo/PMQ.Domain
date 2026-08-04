using PMQ.Notifications;

namespace PMQ.Domain;

/// <summary>
/// Base class for value objects: no identity, compared by the values that compose them.
/// </summary>
/// <remarks>
/// <para>
/// A value object describes a characteristic (money, an address, a document number) rather than
/// a thing with a lifecycle. Two instances holding the same values are interchangeable.
/// </para>
/// <para>
/// Inherits <see cref="Validatable"/> so a malformed value reports why it is invalid instead of
/// throwing. Derived types declare their components:
/// </para>
/// <code>
/// public sealed class Money : ValueObject
/// {
///     public decimal Amount { get; }
///     public string Currency { get; }
///
///     protected override IEnumerable&lt;object?&gt; GetEqualityComponents()
///     {
///         yield return Amount;
///         yield return Currency;
///     }
/// }
/// </code>
/// <para>
/// A <see langword="record"/> gives value equality for free; prefer this base class when the
/// type also needs to accumulate validation notifications, or when equality must ignore some
/// of its members.
/// </para>
/// </remarks>
public abstract class ValueObject : Validatable, IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the values that define equality for this object, in a stable order.
    /// </summary>
    /// <returns>The components compared between two instances.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
        => other is not null
        && other.GetType() == GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(GetType());

        foreach (var component in GetEqualityComponents())
            hash.Add(component);

        return hash.ToHashCode();
    }

    /// <summary>Compares two value objects by their components.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left?.Equals(right) ?? right is null;

    /// <summary>Compares two value objects by their components.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
