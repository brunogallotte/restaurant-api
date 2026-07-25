using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;

public sealed record PrecoDoProdutoAlteradoDomainEvent(
    ProdutoId ProdutoId,
    EstabelecimentoId EstabelecimentoId,
    decimal PrecoAnterior,
    decimal PrecoNovo) : IDomainEvent;
