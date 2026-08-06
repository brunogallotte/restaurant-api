using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.ConfirmarPedido;

public sealed record ConfirmarPedidoCommand(Guid PedidoId) : ICommand;
