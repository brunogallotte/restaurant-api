using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Pedidos;

public sealed class StatusItemPedido : SmartEnum<StatusItemPedido>
{
    public static readonly StatusItemPedido Pendente = new(1, nameof(Pendente));
    public static readonly StatusItemPedido EmPreparo = new(2, nameof(EmPreparo));
    public static readonly StatusItemPedido Pronto = new(3, nameof(Pronto));
    public static readonly StatusItemPedido Entregue = new(4, nameof(Entregue));
    public static readonly StatusItemPedido Cancelado = new(5, nameof(Cancelado));

    private StatusItemPedido(int valor, string nome) : base(valor, nome)
    {
    }

    public bool JaEntrouEmProducao => this == EmPreparo || this == Pronto || this == Entregue;

    public bool PodeSerAlterado => this == Pendente;

    public bool PodeTransicionarPara(StatusItemPedido destino) => TransicoesPermitidas().Contains(destino);

    private IReadOnlyCollection<StatusItemPedido> TransicoesPermitidas()
    {
        if (this == Pendente)
        {
            return [EmPreparo, Cancelado];
        }

        if (this == EmPreparo)
        {
            return [Pronto, Cancelado];
        }

        if (this == Pronto)
        {
            return [Entregue, Cancelado];
        }

        return [];
    }
}
