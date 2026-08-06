using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.ElevarPrioridadeDoPedido;

public sealed record ElevarPrioridadeDoPedidoCommand(Guid PedidoId, string Prioridade) : ICommand;
