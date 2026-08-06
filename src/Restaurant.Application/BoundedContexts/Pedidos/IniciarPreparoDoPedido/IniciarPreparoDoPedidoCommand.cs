using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.IniciarPreparoDoPedido;

public sealed record IniciarPreparoDoPedidoCommand(Guid PedidoId) : ICommand;
