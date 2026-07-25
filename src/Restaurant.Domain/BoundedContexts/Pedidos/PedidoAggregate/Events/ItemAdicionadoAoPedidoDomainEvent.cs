using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;

public sealed record ItemAdicionadoAoPedidoDomainEvent(
    PedidoId PedidoId,
    ItemPedidoId ItemPedidoId,
    ProdutoId ProdutoId,
    string NomeDoProduto,
    int Quantidade,
    decimal PrecoUnitario) : IDomainEvent;
