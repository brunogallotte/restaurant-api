using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record PedidoAbertoDomainEvent(
    PedidoId PedidoId,
    EstabelecimentoId EstabelecimentoId,
    MesaId MesaId,
    FuncionarioId AbertoPor,
    string NumeroPedido,
    DateTimeOffset AbertoEm) : IDomainEvent;
