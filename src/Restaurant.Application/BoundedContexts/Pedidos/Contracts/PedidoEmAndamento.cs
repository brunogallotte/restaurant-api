namespace Restaurant.Application.BoundedContexts.Pedidos.Contracts;

public sealed record PedidoEmAndamento(
    Guid PedidoId,
    string Numero,
    string NumeroDaMesa,
    string? NomeCliente,
    string Status,
    string PrioridadeManual,
    DateTimeOffset AbertoEm,
    DateTimeOffset? FechadoEm,
    int QuantidadeDeItensAtivos);
