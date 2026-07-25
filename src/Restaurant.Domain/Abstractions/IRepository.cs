namespace Restaurant.Domain.Abstractions;

public interface IRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot
    where TId : struct
{
    Task<TAggregate?> ObterPorIdAsync(TId id, CancellationToken cancellationToken = default);

    void Adicionar(TAggregate aggregate);

    void Remover(TAggregate aggregate);
}
