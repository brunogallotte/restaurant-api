using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.Events;

public sealed record CategoriaCriadaDomainEvent(
    CategoriaId CategoriaId,
    EstabelecimentoId EstabelecimentoId,
    string Nome,
    int Ordem) : IDomainEvent;
