using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.UnitTests.Builders;

internal sealed class MesaBuilder
{
    public static readonly DateTimeOffset Agora = new(2026, 7, 25, 19, 0, 0, TimeSpan.Zero);

    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private string _numero = "12";
    private int _lugares = 4;

    public static MesaBuilder Uma() => new();

    public MesaBuilder DoEstabelecimento(EstabelecimentoId id)
    {
        _estabelecimentoId = id;
        return this;
    }

    public MesaBuilder DeNumero(string numero)
    {
        _numero = numero;
        return this;
    }

    public MesaBuilder ComLugares(int lugares)
    {
        _lugares = lugares;
        return this;
    }

    public Mesa Construir()
    {
        var mesa = Mesa.Cadastrar(
            _estabelecimentoId,
            NumeroDaMesa.Criar(_numero).Value,
            _lugares).Value;

        mesa.ClearDomainEvents();

        return mesa;
    }

    public Mesa ConstruirOcupada()
    {
        var mesa = Construir();
        mesa.Ocupar(Agora);
        mesa.ClearDomainEvents();

        return mesa;
    }

    public Mesa ConstruirReservada()
    {
        var mesa = Construir();
        mesa.Reservar(Agora.AddHours(2), Agora);
        mesa.ClearDomainEvents();

        return mesa;
    }
}
