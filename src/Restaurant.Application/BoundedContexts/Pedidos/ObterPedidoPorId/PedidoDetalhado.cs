namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPedidoPorId;

public sealed record PedidoDetalhado(
    Guid PedidoId,
    string Numero,
    Guid MesaId,
    string NumeroDaMesa,
    string? NomeCliente,
    string? Observacao,
    string Status,
    string PrioridadeEfetiva,
    int MinutosDecorridos,
    decimal TaxaDeServico,
    decimal Subtotal,
    decimal ValorDaTaxaDeServico,
    decimal Total,
    DateTimeOffset AbertoEm,
    DateTimeOffset? FechadoEm,
    IReadOnlyList<ItemDoPedidoDetalhado> Itens);
