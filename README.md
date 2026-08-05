# PMQ.Domain

Domain-Driven Design building blocks for .NET: entities with identity-based equality, aggregate roots, value objects and domain events.

Validation is **accumulated as notifications, never thrown as exceptions**.

[![NuGet](https://img.shields.io/nuget/v/PMQ.Domain.svg)](https://www.nuget.org/packages/PMQ.Domain)

> 🇧🇷 [Leia em português](README.pt-BR.md)

## Installation

```bash
dotnet add package PMQ.Domain
```

Requires **.NET 10**. Depends on [PMQ.Mediator](https://github.com/PabloMickaelQuevedo/PMQ.Mediator) (to dispatch domain events) and [PMQ.Notifications](https://github.com/PabloMickaelQuevedo/PMQ.Notifications) (for `Validatable`).

## Why no exceptions

Exceptions are for programming errors. A violated business rule is an expected outcome — and treating it as an exception is expensive and, worse, reports only the **first** failure. An entity that accumulates notifications returns all of them at once:

```json
{
  "status": 422,
  "errors": [
    { "field": "Items", "message": "Item must be at most 200 characters." },
    { "field": "Items", "message": "Provide at least one item." }
  ]
}
```

## Components

| Type | Role |
|---|---|
| `Entity<TId>` | Own identity, identity-based equality, validation and domain events |
| `IAggregateRoot` | Marks the boundary of transactional consistency |
| `ValueObject` | Value-based equality, from the declared components |
| `IDomainEvent` / `DomainEvent` | A fact that occurred, dispatchable by PMQ.Mediator |
| `IHasDomainEvents` | Non-generic contract for infrastructure to collect events |

## Entity

```csharp
public sealed class Order : Entity<Guid>, IAggregateRoot
{
    private readonly List<string> _items = [];

    private Order() { }                                   // ORM

    private Order(Guid id) : base(id) { }

    public IReadOnlyCollection<string> Items => _items;

    public static Order Create(IEnumerable<string>? items)
    {
        var order = new Order(Guid.CreateVersion7());

        foreach (var item in items ?? [])
            order.AddItem(item);

        if (order._items.Count == 0)
            order.AddNotification(nameof(Items), "Provide at least one item.");

        // An invalid aggregate does not announce a fact that never happened.
        if (order.IsValid)
            order.Raise(new OrderPlacedDomainEvent(order.Id));

        return order;
    }

    public void AddItem(string? item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            AddNotification(nameof(Items), "Item cannot be empty.");
            return;
        }

        _items.Add(item.Trim());
    }
}
```

The handler inspects the aggregate and promotes its failures to the request context, using `AddFrom` from PMQ.Notifications:

```csharp
var order = Order.Create(request.Items);

if (order.IsInvalid)
{
    notificationContext.AddFrom(order, NotificationType.BusinessRule);   // → HTTP 422
    return Guid.Empty;
}

await repository.AddAsync(order, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

### Identity-based equality

Two instances with the same `Id` **are** the same entity, even if everything else differs. The comparison also checks the type: an `Order` and an `Invoice` sharing a `Guid` are not equal.

## Value object

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            AddNotification(nameof(Amount), "Amount cannot be negative.");

        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

A `record` already gives value equality for free. Prefer `ValueObject` when the type also needs to accumulate notifications, or when equality must ignore some of its members.

## Domain events

```csharp
public sealed record OrderPlacedDomainEvent(Guid OrderId) : DomainEvent;
```

`DomainEvent` already carries `EventId` (UUID v7, useful for idempotency) and `OccurredOn` in UTC. Since it inherits from `INotification`, it is dispatchable by PMQ.Mediator with no glue at all:

```csharp
internal sealed class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} created.", notification.OrderId);
        return Task.CompletedTask;
    }
}
```

### Publishing after the commit

Events stay pending on the aggregate until the transaction commits. `IHasDomainEvents` lets you collect them without reflection — with EF Core:

```csharp
public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
{
    var entities = context.ChangeTracker
        .Entries<IHasDomainEvents>()
        .Where(entry => entry.Entity.DomainEvents.Count > 0)
        .Select(entry => entry.Entity)
        .ToList();

    var domainEvents = entities.SelectMany(entity => entity.DomainEvents).ToList();

    foreach (var entity in entities)
        entity.ClearDomainEvents();

    await context.SaveChangesAsync(cancellationToken);

    // The cast to object selects the publisher's dynamic-dispatch overload, which resolves
    // handlers by the event's concrete type. Without it, the generic overload would be
    // inferred as Publish<IDomainEvent> and no handler would ever be found.
    foreach (var domainEvent in domainEvents)
        await publisher.Publish((object)domainEvent, cancellationToken);

    return true;
}
```

Publishing **after** the commit is deliberate: a handler must never observe a fact the transaction ended up rolling back.

## License

MIT
