using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.CancelarPedido;

public sealed record CancelarPedidoCommand(Guid PedidoId, string Motivo) : ICommand;
