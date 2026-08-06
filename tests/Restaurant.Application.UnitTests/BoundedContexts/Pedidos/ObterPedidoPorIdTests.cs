using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.Contracts;
using Restaurant.Application.BoundedContexts.Pedidos.ObterPedidoPorId;
using Restaurant.Application.BoundedContexts.Pedidos.Ports;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Policies;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class ObterPedidoPorIdTests
{
    private static readonly DateTimeOffset Abertura = PedidoBuilder.AberturaPadrao;
    private static readonly Guid Identificador = Guid.CreateVersion7();

    private readonly IPedidoQueries _pedidos = Substitute.For<IPedidoQueries>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(Abertura.AddMinutes(25));

    [Fact]
    public async Task Recompoe_subtotal_taxa_e_total_a_partir_dos_itens()
    {
        PedidoLidoE(Pedido(
            taxaDeServico: 10m,
            Item("Picanha", quantidade: 2, precoUnitario: 100m),
            Item("Refrigerante", quantidade: 3, precoUnitario: 8m)));

        var resultado = await Executar();

        resultado.Value.Subtotal.Should().Be(224m);
        resultado.Value.ValorDaTaxaDeServico.Should().Be(22.40m);
        resultado.Value.Total.Should().Be(246.40m);
    }

    [Fact]
    public async Task Item_cancelado_nao_entra_no_subtotal_mas_aparece_na_lista()
    {
        PedidoLidoE(Pedido(
            taxaDeServico: 0m,
            Item("Picanha", quantidade: 1, precoUnitario: 100m),
            Item("Costela", quantidade: 1, precoUnitario: 80m, status: "Cancelado")));

        var resultado = await Executar();

        resultado.Value.Subtotal.Should().Be(100m);
        resultado.Value.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Total_de_cada_item_e_preco_vezes_quantidade()
    {
        PedidoLidoE(Pedido(taxaDeServico: 0m, Item("Picanha", quantidade: 3, precoUnitario: 89.90m)));

        var resultado = await Executar();

        resultado.Value.Itens[0].Total.Should().Be(269.70m);
    }

    [Fact]
    public async Task Prioridade_efetiva_e_minutos_vem_do_relogio_injetado()
    {
        PedidoLidoE(Pedido(taxaDeServico: 0m, Item("Picanha", 1, 100m)));

        var resultado = await Executar();

        resultado.Value.MinutosDecorridos.Should().Be(25);
        resultado.Value.PrioridadeEfetiva.Should().Be("Alta");
    }

    [Fact]
    public async Task Pedido_inexistente_ou_de_outro_tenant_devolve_nao_encontrado()
    {
        _pedidos
            .ObterPorIdAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<PedidoId>(), Arg.Any<CancellationToken>())
            .Returns((PedidoLido?)null);

        var resultado = await Executar();

        resultado.Error.Should().Be(PedidoErrors.NaoEncontrado);
        resultado.Error.Tipo.Should().Be(ErrorType.NaoEncontrado);
    }

    [Fact]
    public async Task A_leitura_e_escopada_pelo_tenant_na_propria_porta()
    {
        PedidoLidoE(Pedido(taxaDeServico: 0m, Item("Picanha", 1, 100m)));

        await Executar();

        await _pedidos.Received(1).ObterPorIdAsync(
            _tenant.EstabelecimentoId,
            new PedidoId(Identificador),
            Arg.Any<CancellationToken>());
    }

    private static ItemLido Item(string nome, int quantidade, decimal precoUnitario, string status = "Pendente") =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            nome,
            quantidade,
            precoUnitario,
            status,
            Observacao: null,
            MotivoCancelamento: null);

    private static PedidoLido Pedido(decimal taxaDeServico, params ItemLido[] itens) =>
        new(
            Identificador,
            "20260725-0001",
            Guid.CreateVersion7(),
            NumeroDaMesa: "12",
            NomeCliente: "Bruno",
            Observacao: null,
            Status: "EmPreparo",
            PrioridadeManual: "Normal",
            taxaDeServico,
            Abertura,
            FechadoEm: null,
            itens);

    private void PedidoLidoE(PedidoLido lido) =>
        _pedidos
            .ObterPorIdAsync(_tenant.EstabelecimentoId, new PedidoId(Identificador), Arg.Any<CancellationToken>())
            .Returns(lido);

    private Task<Result<PedidoDetalhado>> Executar() =>
        new ObterPedidoPorIdQueryHandler(_pedidos, PoliticaDePrioridade.Padrao, _tenant, _relogio)
            .Handle(new ObterPedidoPorIdQuery(Identificador), TestContext.Current.CancellationToken);
}
