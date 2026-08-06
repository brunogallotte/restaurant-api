namespace Restaurant.Application.BoundedContexts.Cardapio.Contracts;

public sealed record ItemDoCardapio(
    Guid ProdutoId,
    string Nome,
    string? Descricao,
    decimal Preco,
    int MinutosDePreparo,
    bool Disponivel,
    Guid CategoriaId,
    string Categoria,
    int OrdemDaCategoria);
