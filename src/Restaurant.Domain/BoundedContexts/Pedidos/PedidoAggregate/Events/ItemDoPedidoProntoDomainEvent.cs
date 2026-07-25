using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record ItemDoPedidoProntoDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    ProdutoId ProdutoId) : IDomainEvent;
