using Restaurant.Domain.BuildingBlocks.Events;

namespace Restaurant.Domain.BuildingBlocks.Model;

public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
