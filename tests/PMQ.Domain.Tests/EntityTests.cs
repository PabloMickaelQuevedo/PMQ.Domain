using PMQ.Domain;

namespace PMQ.Domain.Tests;

public class EntityTests
{
    private sealed record OrderPlacedDomainEvent(Guid OrderId) : DomainEvent;

    private sealed class Order : Entity<Guid>, IAggregateRoot
    {
        public Order(Guid id) : base(id) { }

        public Order() { }

        public void Place() => Raise(new OrderPlacedDomainEvent(Id));

        public void Reject(string message) => AddNotification(nameof(Order), message);
    }

    private sealed class Invoice : Entity<Guid>
    {
        public Invoice(Guid id) : base(id) { }
    }

    [Fact]
    public void Constructor_WithId_ShouldExposeIt()
    {
        var id = Guid.CreateVersion7();

        new Order(id).Id.ShouldBe(id);
    }

    [Fact]
    public void ParameterlessConstructor_ShouldLeaveDefaultId()
    {
        new Order().Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Equals_WithSameId_ShouldBeEqual()
    {
        var id = Guid.CreateVersion7();

        var first = new Order(id);
        var second = new Order(id);

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldNotBeEqual()
    {
        new Order(Guid.CreateVersion7()).ShouldNotBe(new Order(Guid.CreateVersion7()));
    }

    [Fact]
    public void Equals_WithSameIdButDifferentType_ShouldNotBeEqual()
    {
        // Sem a checagem de tipo, um Order e uma Invoice com o mesmo Guid seriam "iguais".
        var id = Guid.CreateVersion7();

        new Order(id).Equals(new Invoice(id)).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldNotBeEqual()
    {
        var order = new Order(Guid.CreateVersion7());

        order.Equals(null).ShouldBeFalse();
        (order == null).ShouldBeFalse();
        (null == order).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ShouldBeEqual()
    {
        Order? left = null;
        Order? right = null;

        (left == right).ShouldBeTrue();
    }

    [Fact]
    public void NewEntity_ShouldHaveNoPendingDomainEvents()
    {
        new Order(Guid.CreateVersion7()).DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raise_ShouldQueueTheEvent()
    {
        var order = new Order(Guid.CreateVersion7());

        order.Place();

        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderPlacedDomainEvent>()
            .OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheCollection()
    {
        var order = new Order(Guid.CreateVersion7());
        order.Place();

        order.ClearDomainEvents();

        order.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Entity_ShouldBeValidatable()
    {
        var order = new Order(Guid.CreateVersion7());

        order.IsValid.ShouldBeTrue();

        order.Reject("Order cannot be empty.");

        order.IsInvalid.ShouldBeTrue();
        order.ValidationResult.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Order cannot be empty.");
    }

    [Fact]
    public void Entity_ShouldImplementIHasDomainEvents()
    {
        new Order(Guid.CreateVersion7()).ShouldBeAssignableTo<IHasDomainEvents>();
    }
}
