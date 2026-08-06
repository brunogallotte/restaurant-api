using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.Builders;

internal sealed class MesaBuilder
{
    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private string _numero = "12";
    private int _lugares = 4;

    public static MesaBuilder Uma() => new();

    public MesaBuilder DoEstabelecimento(EstabelecimentoId estabelecimentoId)
    {
        _estabelecimentoId = estabelecimentoId;
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

    public Mesa ConstruirOcupada(DateTimeOffset ocupadaEm)
    {
        var mesa = Construir();
        mesa.Ocupar(ocupadaEm);
        mesa.ClearDomainEvents();

        return mesa;
    }
}
