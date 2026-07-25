using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Enumerations;

public sealed class StatusMesa : SmartEnum<StatusMesa>
{
    public static readonly StatusMesa Livre = new(1, nameof(Livre));
    public static readonly StatusMesa Reservada = new(2, nameof(Reservada));
    public static readonly StatusMesa Ocupada = new(3, nameof(Ocupada));

    private StatusMesa(int valor, string nome) : base(valor, nome)
    {
    }

    public bool AceitaNovoPedido => this == Livre || this == Reservada;

    public bool PodeTransicionarPara(StatusMesa destino) => TransicoesPermitidas().Contains(destino);

    private IReadOnlyCollection<StatusMesa> TransicoesPermitidas()
    {
        if (this == Livre)
        {
            return [Reservada, Ocupada];
        }

        if (this == Reservada)
        {
            return [Ocupada, Livre];
        }

        if (this == Ocupada)
        {
            return [Livre];
        }

        return [];
    }
}
