using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.FecharPedido;

public sealed record FecharPedidoCommand(Guid PedidoId) : ICommand;
