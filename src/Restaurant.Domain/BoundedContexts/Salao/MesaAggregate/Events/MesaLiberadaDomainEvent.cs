using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Events;

public sealed record MesaLiberadaDomainEvent(
    MesaId MesaId,
    EstabelecimentoId EstabelecimentoId,
    string Numero,
    DateTimeOffset LiberadaEm) : IDomainEvent;
