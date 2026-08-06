using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.MarcarPedidoComoPronto;

public sealed record MarcarPedidoComoProntoCommand(Guid PedidoId) : ICommand;
