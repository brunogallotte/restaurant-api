using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.AbrirPedido;

public sealed record AbrirPedidoCommand(
    Guid MesaId,
    string? NomeCliente,
    string? Observacao) : ICommand<Guid>;
