using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;

public sealed class PrioridadePedido : SmartEnum<PrioridadePedido>
{
    public static readonly PrioridadePedido Normal = new(1, nameof(Normal));
    public static readonly PrioridadePedido Alta = new(2, nameof(Alta));
    public static readonly PrioridadePedido Urgente = new(3, nameof(Urgente));

    private PrioridadePedido(int valor, string nome) : base(valor, nome)
    {
    }

    public bool EhMaiorQue(PrioridadePedido outra) => Valor > outra.Valor;

    public static PrioridadePedido MaisAlta(PrioridadePedido primeira, PrioridadePedido segunda) =>
        primeira.EhMaiorQue(segunda) ? primeira : segunda;
}
