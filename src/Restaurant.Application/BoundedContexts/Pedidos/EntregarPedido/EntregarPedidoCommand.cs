using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.EntregarPedido;

public sealed record EntregarPedidoCommand(Guid PedidoId) : ICommand;
