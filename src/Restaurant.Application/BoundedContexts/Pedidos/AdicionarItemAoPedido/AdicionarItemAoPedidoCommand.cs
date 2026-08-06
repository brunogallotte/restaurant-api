using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.AdicionarItemAoPedido;

public sealed record AdicionarItemAoPedidoCommand(
    Guid PedidoId,
    Guid ProdutoId,
    int Quantidade,
    string? Observacao) : ICommand;
