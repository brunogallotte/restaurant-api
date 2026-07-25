using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record PedidoEntregueDomainEvent(
    PedidoId PedidoId,
    MesaId MesaId,
    DateTimeOffset EntregueEm) : IDomainEvent;
