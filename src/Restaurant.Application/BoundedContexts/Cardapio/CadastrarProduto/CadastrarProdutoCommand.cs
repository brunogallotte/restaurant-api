using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Cardapio.CadastrarProduto;

public sealed record CadastrarProdutoCommand(
    Guid CategoriaId,
    string Nome,
    string? Descricao,
    decimal Preco,
    int MinutosDePreparo) : ICommand<Guid>;
