using PMQ.Domain;
using PMQ.Mediator;

namespace PMQ.Domain.Tests;

public class DomainEventTests
{
    private sealed record OrderPlacedDomainEvent(Guid OrderId) : DomainEvent;

    [Fact]
    public void NewEvent_ShouldGetUuidV7Identifier()
    {
        // UUID v7 embute o timestamp: eventos ficam naturalmente ordenáveis por criação.
        new OrderPlacedDomainEvent(Guid.CreateVersion7()).EventId.Version.ShouldBe(7);
    }

    [Fact]
    public void NewEvent_ShouldGetDistinctIdentifiers()
    {
        var first = new OrderPlacedDomainEvent(Guid.CreateVersion7());
        var second = new OrderPlacedDomainEvent(Guid.CreateVersion7());

        first.EventId.ShouldNotBe(second.EventId);
    }

    [Fact]
    public void NewEvent_ShouldStampOccurredOnInUtc()
    {
        var before = DateTimeOffset.UtcNow;

        var domainEvent = new OrderPlacedDomainEvent(Guid.CreateVersion7());

        domainEvent.OccurredOn.Offset.ShouldBe(TimeSpan.Zero);
        domainEvent.OccurredOn.ShouldBeGreaterThanOrEqualTo(before);
        domainEvent.OccurredOn.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void DomainEvent_ShouldBeDispatchableAsNotification()
    {
        // A herança de INotification é o que permite despachar pelo PMQ.Mediator sem cola manual.
        new OrderPlacedDomainEvent(Guid.CreateVersion7()).ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void Equals_ShouldCompareByValueWhenMetadataMatches()
    {
        var orderId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var occurredOn = DateTimeOffset.UtcNow;

        var first = new OrderPlacedDomainEvent(orderId) { EventId = eventId, OccurredOn = occurredOn };
        var second = new OrderPlacedDomainEvent(orderId) { EventId = eventId, OccurredOn = occurredOn };

        first.ShouldBe(second);
    }

    [Fact]
    public void Equals_WithDifferentPayload_ShouldNotBeEqual()
    {
        var eventId = Guid.CreateVersion7();
        var occurredOn = DateTimeOffset.UtcNow;

        var first = new OrderPlacedDomainEvent(Guid.CreateVersion7()) { EventId = eventId, OccurredOn = occurredOn };
        var second = new OrderPlacedDomainEvent(Guid.CreateVersion7()) { EventId = eventId, OccurredOn = occurredOn };

        first.ShouldNotBe(second);
    }
}
