using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.Events;

public sealed record EstabelecimentoDesativadoDomainEvent(
    EstabelecimentoId EstabelecimentoId,
    DateTimeOffset DesativadoEm) : IDomainEvent;
