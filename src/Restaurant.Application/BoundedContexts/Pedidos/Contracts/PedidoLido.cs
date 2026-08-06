namespace Restaurant.Application.BoundedContexts.Pedidos.Contracts;

public sealed record PedidoLido(
    Guid PedidoId,
    string Numero,
    Guid MesaId,
    string NumeroDaMesa,
    string? NomeCliente,
    string? Observacao,
    string Status,
    string PrioridadeManual,
    decimal TaxaDeServico,
    DateTimeOffset AbertoEm,
    DateTimeOffset? FechadoEm,
    IReadOnlyList<ItemLido> Itens);
