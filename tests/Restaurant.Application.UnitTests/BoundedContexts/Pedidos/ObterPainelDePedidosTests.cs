using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.Contracts;
using Restaurant.Application.BoundedContexts.Pedidos.ObterPainelDePedidos;
using Restaurant.Application.BoundedContexts.Pedidos.Ports;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.Policies;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class ObterPainelDePedidosTests
{
    private static readonly DateTimeOffset Abertura = PedidoBuilder.AberturaPadrao;

    private readonly IPedidoQueries _pedidos = Substitute.For<IPedidoQueries>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(Abertura);

    [Fact]
    public async Task Prioridade_efetiva_sobe_com_o_tempo_sem_mudar_a_manual()
    {
        PainelTem(Linha("Aberto", prioridadeManual: "Normal", abertoEm: Abertura));

        _relogio.Advance(TimeSpan.FromMinutes(36));
        var resultado = await Executar();

        var pedido = resultado.Value.Should().ContainSingle().Subject;
        pedido.PrioridadeEfetiva.Should().Be("Urgente");
        pedido.MinutosDecorridos.Should().Be(36);
    }

    [Theory]
    [InlineData(0, "Normal")]
    [InlineData(19, "Normal")]
    [InlineData(20, "Alta")]
    [InlineData(35, "Urgente")]
    public async Task Prioridade_efetiva_segue_os_limiares_da_politica(int minutos, string esperada)
    {
        PainelTem(Linha("EmPreparo", prioridadeManual: "Normal", abertoEm: Abertura));

        _relogio.Advance(TimeSpan.FromMinutes(minutos));
        var resultado = await Executar();

        resultado.Value[0].PrioridadeEfetiva.Should().Be(esperada);
    }

    [Fact]
    public async Task Prioridade_manual_nunca_e_rebaixada_pelo_tempo()
    {
        PainelTem(Linha("Aberto", prioridadeManual: "Urgente", abertoEm: Abertura));

        var resultado = await Executar();

        resultado.Value[0].PrioridadeEfetiva.Should().Be("Urgente");
    }

    [Fact]
    public async Task Painel_ordena_por_prioridade_efetiva_e_desempata_pelo_mais_antigo()
    {
        PainelTem(
            Linha("Aberto", "Normal", Abertura.AddMinutes(-5), numero: "recente-normal"),
            Linha("Aberto", "Normal", Abertura.AddMinutes(-40), numero: "antigo-urgente"),
            Linha("Aberto", "Normal", Abertura.AddMinutes(-38), numero: "menos-antigo-urgente"));

        var resultado = await Executar();

        resultado.Value.Select(pedido => pedido.Numero)
            .Should().Equal("antigo-urgente", "menos-antigo-urgente", "recente-normal");
    }

    [Fact]
    public async Task Pedido_em_status_final_nao_escala_pelo_tempo()
    {
        PainelTem(Linha("Fechado", "Normal", Abertura.AddHours(-3), fechadoEm: Abertura.AddHours(-2)));

        var resultado = await Executar();

        resultado.Value[0].PrioridadeEfetiva.Should().Be("Normal");
        resultado.Value[0].MinutosDecorridos.Should().Be(60);
    }

    [Fact]
    public async Task Painel_le_apenas_o_estabelecimento_do_tenant()
    {
        PainelTem();

        await Executar();

        await _pedidos.Received(1).ListarEmAndamentoAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>());
    }

    private static PedidoEmAndamento Linha(
        string status,
        string prioridadeManual,
        DateTimeOffset abertoEm,
        DateTimeOffset? fechadoEm = null,
        string numero = "20260725-0001") =>
        new(
            Guid.CreateVersion7(),
            numero,
            NumeroDaMesa: "12",
            NomeCliente: null,
            status,
            prioridadeManual,
            abertoEm,
            fechadoEm,
            QuantidadeDeItensAtivos: 2);

    private void PainelTem(params PedidoEmAndamento[] linhas) =>
        _pedidos
            .ListarEmAndamentoAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>())
            .Returns(linhas);

    private Task<Result<IReadOnlyList<PedidoNoPainel>>> Executar() =>
        new ObterPainelDePedidosQueryHandler(_pedidos, PoliticaDePrioridade.Padrao, _tenant, _relogio)
            .Handle(new ObterPainelDePedidosQuery(), TestContext.Current.CancellationToken);
}
