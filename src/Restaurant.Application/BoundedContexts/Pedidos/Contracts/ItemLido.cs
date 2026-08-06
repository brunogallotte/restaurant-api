namespace Restaurant.Application.BoundedContexts.Pedidos.Contracts;

public sealed record ItemLido(
    Guid ItemId,
    Guid ProdutoId,
    string Nome,
    int Quantidade,
    decimal PrecoUnitario,
    string Status,
    string? Observacao,
    string? MotivoCancelamento);
