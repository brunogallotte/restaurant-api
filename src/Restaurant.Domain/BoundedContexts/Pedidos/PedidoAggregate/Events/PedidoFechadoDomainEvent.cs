using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record PedidoFechadoDomainEvent(
    PedidoId PedidoId,
    EstabelecimentoId EstabelecimentoId,
    MesaId MesaId,
    decimal Subtotal,
    decimal TaxaDeServico,
    decimal Total,
    DateTimeOffset FechadoEm) : IDomainEvent;
