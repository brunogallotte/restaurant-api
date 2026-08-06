using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Cardapio.AlterarPrecoDoProduto;

public sealed record AlterarPrecoDoProdutoCommand(Guid ProdutoId, decimal NovoPreco) : ICommand;
