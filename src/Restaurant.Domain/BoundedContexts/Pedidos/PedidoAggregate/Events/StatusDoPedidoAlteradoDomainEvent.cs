using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record StatusDoPedidoAlteradoDomainEvent(
    PedidoId PedidoId,
    StatusPedido StatusAnterior,
    StatusPedido StatusNovo,
    DateTimeOffset AlteradoEm) : IDomainEvent;
