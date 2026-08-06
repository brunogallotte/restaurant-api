namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPainelDePedidos;

public sealed record PedidoNoPainel(
    Guid PedidoId,
    string Numero,
    string NumeroDaMesa,
    string? NomeCliente,
    string Status,
    string PrioridadeEfetiva,
    int MinutosDecorridos,
    int QuantidadeDeItensAtivos,
    DateTimeOffset AbertoEm);
