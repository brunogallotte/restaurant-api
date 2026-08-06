using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Salao.Contracts;
using Restaurant.Application.BoundedContexts.Salao.ListarMesasLivres;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.BoundedContexts.Salao;

public sealed class ListarMesasLivresTests
{
    private readonly IMesaRepository _mesas = Substitute.For<IMesaRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Lista_as_mesas_livres_do_estabelecimento_do_tenant()
    {
        MesasLivres(Mesa("12", lugares: 4));

        var resultado = await Executar(lugaresMinimos: 1);

        resultado.Sucesso.Should().BeTrue();
        await _mesas.Received(1).ListarLivresAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filtra_mesas_que_nao_comportam_o_grupo()
    {
        MesasLivres(
            Mesa("12", lugares: 2),
            Mesa("14", lugares: 6));

        var resultado = await Executar(lugaresMinimos: 4);

        resultado.Value.Select(mesa => mesa.Numero).Should().Equal("14");
    }

    [Fact]
    public async Task Ordena_pela_menor_mesa_que_serve()
    {
        MesasLivres(
            Mesa("20", lugares: 8),
            Mesa("12", lugares: 4),
            Mesa("14", lugares: 4));

        var resultado = await Executar(lugaresMinimos: 4);

        resultado.Value.Select(mesa => mesa.Numero).Should().Equal("12", "14", "20");
    }

    [Fact]
    public async Task Salao_lotado_devolve_lista_vazia_com_sucesso()
    {
        MesasLivres();

        var resultado = await Executar(lugaresMinimos: 2);

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.Should().BeEmpty();
    }

    private Mesa Mesa(string numero, int lugares) =>
        MesaBuilder.Uma()
            .DoEstabelecimento(_tenant.EstabelecimentoId)
            .DeNumero(numero)
            .ComLugares(lugares)
            .Construir();

    private void MesasLivres(params Mesa[] mesas) =>
        _mesas
            .ListarLivresAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>())
            .Returns(mesas);

    private Task<Result<IReadOnlyList<MesaLivre>>> Executar(int lugaresMinimos) =>
        new ListarMesasLivresQueryHandler(_mesas, _tenant)
            .Handle(new ListarMesasLivresQuery(lugaresMinimos), TestContext.Current.CancellationToken);
}
