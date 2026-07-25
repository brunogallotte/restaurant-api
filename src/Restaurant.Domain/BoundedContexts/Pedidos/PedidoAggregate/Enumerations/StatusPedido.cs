using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;

public sealed class StatusPedido : SmartEnum<StatusPedido>
{
    public static readonly StatusPedido Aberto = new(1, nameof(Aberto));
    public static readonly StatusPedido Confirmado = new(2, nameof(Confirmado));
    public static readonly StatusPedido EmPreparo = new(3, nameof(EmPreparo));
    public static readonly StatusPedido Pronto = new(4, nameof(Pronto));
    public static readonly StatusPedido Entregue = new(5, nameof(Entregue));
    public static readonly StatusPedido Fechado = new(6, nameof(Fechado));
    public static readonly StatusPedido Cancelado = new(7, nameof(Cancelado));

    private StatusPedido(int valor, string nome) : base(valor, nome)
    {
    }

    public bool EhFinal => this == Fechado || this == Cancelado;

    public bool AceitaNovosItens => this == Aberto || this == Confirmado || this == EmPreparo || this == Pronto;

    public bool PodeTransicionarPara(StatusPedido destino) => TransicoesPermitidas().Contains(destino);

    private IReadOnlyCollection<StatusPedido> TransicoesPermitidas()
    {
        if (this == Aberto)
        {
            return [Confirmado, Cancelado];
        }

        if (this == Confirmado)
        {
            return [EmPreparo, Cancelado];
        }

        if (this == EmPreparo)
        {
            return [Pronto, Cancelado];
        }

        if (this == Pronto)
        {
            return [Entregue, EmPreparo];
        }

        if (this == Entregue)
        {
            return [Fechado];
        }

        return [];
    }
}
