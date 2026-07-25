using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record PedidoConfirmadoDomainEvent(
    PedidoId PedidoId,
    EstabelecimentoId EstabelecimentoId,
    MesaId MesaId,
    int QuantidadeDeItens,
    decimal Subtotal,
    DateTimeOffset ConfirmadoEm) : IDomainEvent;
