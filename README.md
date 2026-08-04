# PMQ.Domain

Blocos de construção de Domain-Driven Design para .NET: entidades com igualdade por identidade, raízes de agregado, value objects e domain events.

A validação é **acumulada como notificação, nunca lançada como exceção**.

[![NuGet](https://img.shields.io/nuget/v/PMQ.Domain.svg)](https://www.nuget.org/packages/PMQ.Domain)

## Instalação

```bash
dotnet add package PMQ.Domain
```

Requer **.NET 10**. Depende de [PMQ.Mediator](https://github.com/PabloMickaelQuevedo/PMQ.Mediator) (para despachar domain events) e [PMQ.Notifications](https://github.com/PabloMickaelQuevedo/PMQ.Notifications) (para o `Validatable`).

## Por que sem exceções

Exceção é para erro de programação. Regra de negócio violada é resultado esperado — e tratá-la como exceção custa caro e, pior, reporta apenas a **primeira** falha. Uma entidade que acumula notificações devolve todas de uma vez:

```json
{
  "status": 422,
  "errors": [
    { "field": "Items", "message": "O item deve ter no máximo 200 caracteres." },
    { "field": "Items", "message": "Informe ao menos um item." }
  ]
}
```

## Componentes

| Tipo | Papel |
|---|---|
| `Entity<TId>` | Identidade própria, igualdade por identidade, validação e domain events |
| `IAggregateRoot` | Marca a fronteira de consistência transacional |
| `ValueObject` | Igualdade por valor, a partir dos componentes declarados |
| `IDomainEvent` / `DomainEvent` | Fato ocorrido, despachável pelo PMQ.Mediator |
| `IHasDomainEvents` | Contrato não genérico para a infraestrutura coletar eventos |

## Entidade

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
            order.AddNotification(nameof(Items), "Informe ao menos um item.");

        // Um agregado inválido não anuncia um fato que não aconteceu.
        if (order.IsValid)
            order.Raise(new OrderPlacedDomainEvent(order.Id));

        return order;
    }

    public void AddItem(string? item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            AddNotification(nameof(Items), "O item não pode ser vazio.");
            return;
        }

        _items.Add(item.Trim());
    }
}
```

O handler consulta o agregado e promove as falhas para o contexto da requisição, usando o `AddFrom` do PMQ.Notifications:

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

### Igualdade por identidade

Duas instâncias com o mesmo `Id` **são** a mesma entidade, ainda que todo o resto difira. A comparação também confere o tipo: um `Order` e uma `Invoice` com o mesmo `Guid` não são iguais.

## Value object

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            AddNotification(nameof(Amount), "O valor não pode ser negativo.");

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

`record` já dá igualdade por valor de graça. Prefira `ValueObject` quando o tipo também precisar acumular notificações, ou quando a igualdade tiver que ignorar parte dos membros.

## Domain events

```csharp
public sealed record OrderPlacedDomainEvent(Guid OrderId) : DomainEvent;
```

`DomainEvent` já traz `EventId` (UUID v7, útil para idempotência) e `OccurredOn` em UTC. Como herda de `INotification`, é despachável pelo PMQ.Mediator sem cola nenhuma:

```csharp
internal sealed class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Pedido {OrderId} criado.", notification.OrderId);
        return Task.CompletedTask;
    }
}
```

### Publicando depois do commit

Os eventos ficam pendentes no agregado até a transação confirmar. `IHasDomainEvents` permite coletá-los sem reflexão — com EF Core:

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

    // O cast para object seleciona a sobrecarga de despacho dinâmico do publisher, que
    // resolve os handlers pelo tipo concreto. Sem ele a sobrecarga genérica seria inferida
    // como Publish<IDomainEvent> e nenhum handler seria encontrado.
    foreach (var domainEvent in domainEvents)
        await publisher.Publish((object)domainEvent, cancellationToken);

    return true;
}
```

Publicar **depois** do commit é deliberado: um handler nunca deve observar um fato que a transação acabou não confirmando.

## Licença

MIT
