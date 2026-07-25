using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record QuantidadeDoItemAlteradaDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    int QuantidadeAnterior,
    int QuantidadeNova) : IDomainEvent;
