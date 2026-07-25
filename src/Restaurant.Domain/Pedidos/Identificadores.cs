namespace Restaurant.Domain.Pedidos;

public readonly record struct PedidoId(Guid Valor)
{
    public static PedidoId Novo() => new(Guid.CreateVersion7());
}

public readonly record struct ItemPedidoId(Guid Valor)
{
    public static ItemPedidoId Novo() => new(Guid.CreateVersion7());
}
