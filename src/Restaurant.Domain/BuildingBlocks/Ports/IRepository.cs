using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.Domain.BuildingBlocks.Ports;

public interface IRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot
    where TId : struct
{
    Task<TAggregate?> ObterPorIdAsync(TId id, CancellationToken cancellationToken = default);

    void Adicionar(TAggregate aggregate);

    void Remover(TAggregate aggregate);
}
