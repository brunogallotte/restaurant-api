namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;

public readonly record struct ItemPedidoId(Guid Valor)
{
    public static ItemPedidoId Novo() => new(Guid.CreateVersion7());
}
