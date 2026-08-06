namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPedidoPorId;

public sealed record ItemDoPedidoDetalhado(
    Guid ItemId,
    Guid ProdutoId,
    string Nome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Total,
    string Status,
    string? Observacao,
    string? MotivoCancelamento);
