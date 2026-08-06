using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.CancelarItemDoPedido;

public sealed record CancelarItemDoPedidoCommand(
    Guid PedidoId,
    Guid ItemId,
    string? Motivo) : ICommand;
