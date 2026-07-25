using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.Events;

public sealed record TaxaDeServicoAlteradaDomainEvent(
    EstabelecimentoId EstabelecimentoId,
    decimal TaxaAnterior,
    decimal TaxaNova) : IDomainEvent;
