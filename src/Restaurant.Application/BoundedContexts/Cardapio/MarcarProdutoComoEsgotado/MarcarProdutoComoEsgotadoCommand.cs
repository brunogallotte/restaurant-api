using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Cardapio.MarcarProdutoComoEsgotado;

public sealed record MarcarProdutoComoEsgotadoCommand(Guid ProdutoId) : ICommand;
