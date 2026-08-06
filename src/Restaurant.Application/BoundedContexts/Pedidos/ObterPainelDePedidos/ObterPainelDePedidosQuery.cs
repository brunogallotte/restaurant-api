using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPainelDePedidos;

public sealed record ObterPainelDePedidosQuery : IQuery<IReadOnlyList<PedidoNoPainel>>;
