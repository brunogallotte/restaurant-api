using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;

public sealed record ProdutoMovidoDeCategoriaDomainEvent(
    ProdutoId ProdutoId,
    CategoriaId CategoriaAnterior,
    CategoriaId CategoriaNova) : IDomainEvent;
