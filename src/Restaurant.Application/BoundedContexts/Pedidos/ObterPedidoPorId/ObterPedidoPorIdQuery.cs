using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPedidoPorId;

public sealed record ObterPedidoPorIdQuery(Guid PedidoId) : IQuery<PedidoDetalhado>;
