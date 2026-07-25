using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.Events;

public sealed record CategoriaDesativadaDomainEvent(
    CategoriaId CategoriaId,
    EstabelecimentoId EstabelecimentoId) : IDomainEvent;
