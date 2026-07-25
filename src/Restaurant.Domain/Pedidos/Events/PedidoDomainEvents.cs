using Restaurant.Domain.Abstractions;
using Restaurant.Domain.Compartilhado;

namespace Restaurant.Domain.Pedidos.Events;

public sealed record PedidoAbertoDomainEvent(
    PedidoId PedidoId,
    EstabelecimentoId EstabelecimentoId,
    MesaId MesaId,
    FuncionarioId AbertoPor,
    string NumeroPedido,
    DateTimeOffset AbertoEm) : IDomainEvent;

public sealed record ItemAdicionadoAoPedidoDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    ProdutoId ProdutoId,
    string NomeDoProduto,
    int Quantidade,
    decimal PrecoUnitario) : IDomainEvent;

public sealed record QuantidadeDoItemAlteradaDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    int QuantidadeAnterior,
    int QuantidadeNova) : IDomainEvent;

public sealed record ItemDoPedidoCanceladoDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    ProdutoId ProdutoId,
    int Quantidade,
    bool JaEstavaEmProducao,
    string? Motivo) : IDomainEvent;

public sealed record PedidoConfirmadoDomainEvent(
    PedidoId PedidoId,
    EstabelecimentoId EstabelecimentoId,
    MesaId MesaId,
    int QuantidadeDeItens,
    decimal Subtotal,
    DateTimeOffset ConfirmadoEm) : IDomainEvent;

public sealed record StatusDoPedidoAlteradoDomainEvent(
    PedidoId PedidoId,
    StatusPedido StatusAnterior,
    StatusPedido StatusNovo,
    DateTimeOffset AlteradoEm) : IDomainEvent;

public sealed record ItemDoPedidoProntoDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    ProdutoId ProdutoId) : IDomainEvent;

public sealed record PedidoProntoDomainEvent(
    PedidoId PedidoId,
    MesaId MesaId,
    DateTimeOffset ProntoEm) : IDomainEvent;

public sealed record PedidoEntregueDomainEvent(
    PedidoId PedidoId,
    MesaId MesaId,
    DateTimeOffset EntregueEm) : IDomainEvent;

public sealed record PedidoFechadoDomainEvent(
    PedidoId PedidoId,
    EstabelecimentoId EstabelecimentoId,
    MesaId MesaId,
    decimal Subtotal,
    decimal TaxaDeServico,
    decimal Total,
    DateTimeOffset FechadoEm) : IDomainEvent;

public sealed record PedidoCanceladoDomainEvent(
    PedidoId PedidoId,
    MesaId MesaId,
    string Motivo,
    DateTimeOffset CanceladoEm) : IDomainEvent;

public sealed record PrioridadeDoPedidoElevadaDomainEvent(
    PedidoId PedidoId,
    PrioridadePedido PrioridadeAnterior,
    PrioridadePedido PrioridadeNova) : IDomainEvent;
