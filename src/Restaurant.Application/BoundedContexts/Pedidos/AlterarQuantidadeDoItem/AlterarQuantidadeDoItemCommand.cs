using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.AlterarQuantidadeDoItem;

public sealed record AlterarQuantidadeDoItemCommand(
    Guid PedidoId,
    Guid ItemId,
    int NovaQuantidade) : ICommand;
