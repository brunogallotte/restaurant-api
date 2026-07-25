using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Events;

public sealed record MesaOcupadaDomainEvent(
    MesaId MesaId,
    EstabelecimentoId EstabelecimentoId,
    string Numero,
    DateTimeOffset OcupadaEm) : IDomainEvent;
