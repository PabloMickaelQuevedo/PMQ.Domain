namespace PMQ.Domain;

/// <summary>
/// Marks the root of an aggregate.
/// </summary>
/// <remarks>
/// The aggregate root is the only entity of the aggregate reachable through a repository, and
/// the boundary of transactional consistency. Constrain repositories to it
/// (<c>where TAggregate : IAggregateRoot</c>) so that inner entities cannot be loaded or saved
/// behind the root's back.
/// </remarks>
public interface IAggregateRoot;
