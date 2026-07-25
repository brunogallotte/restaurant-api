using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;

public sealed record ProdutoDescontinuadoDomainEvent(
    ProdutoId ProdutoId,
    EstabelecimentoId EstabelecimentoId,
    string Nome) : IDomainEvent;
