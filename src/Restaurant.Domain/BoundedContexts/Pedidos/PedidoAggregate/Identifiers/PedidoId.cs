namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;

public readonly record struct PedidoId(Guid Valor)
{
    public static PedidoId Novo() => new(Guid.CreateVersion7());
}
