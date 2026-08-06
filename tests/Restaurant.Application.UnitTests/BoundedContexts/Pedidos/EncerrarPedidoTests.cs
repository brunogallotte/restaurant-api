using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.CancelarPedido;
using Restaurant.Application.BoundedContexts.Pedidos.ElevarPrioridadeDoPedido;
using Restaurant.Application.BoundedContexts.Pedidos.EntregarPedido;
using Restaurant.Application.BoundedContexts.Pedidos.FecharPedido;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class EncerrarPedidoTests
{
    private static readonly DateTimeOffset Agora = PedidoBuilder.AberturaPadrao.AddMinutes(45);

    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(Agora);

    [Fact]
    public async Task Entregar_leva_os_itens_ativos_junto()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirPronto());

        var resultado = await Entregar(pedido.Id);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Entregue);
        pedido.EntregueEm.Should().Be(Agora);
        pedido.Itens.Should().AllSatisfy(item => item.Status.Should().Be(StatusItemPedido.Entregue));
    }

    [Fact]
    public async Task Fechar_congela_o_total_com_a_taxa_de_servico()
    {
        var pedido = PedidoDoTenant(builder =>
            builder.ComTaxaDeServico(10m).ComItem(preco: 100m, quantidade: 2).ConstruirEntregue());

        var resultado = await Fechar(pedido.Id);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Fechado);
        pedido.Subtotal.Valor.Should().Be(200m);
        pedido.ValorDaTaxaDeServico.Valor.Should().Be(20m);
        pedido.Total.Valor.Should().Be(220m);
        pedido.DomainEvents.Should().ContainItemsAssignableTo<PedidoFechadoDomainEvent>();
    }

    [Fact]
    public async Task Fechar_pedido_que_nao_foi_entregue_e_transicao_invalida()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirPronto());

        var resultado = await Fechar(pedido.Id);

        resultado.Error.Codigo.Should().Be("Pedido.TransicaoInvalida");
        pedido.Status.Should().Be(StatusPedido.Pronto);
    }

    [Fact]
    public async Task Fechar_congela_o_tempo_decorrido()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEntregue());
        await Fechar(pedido.Id);

        _relogio.Advance(TimeSpan.FromHours(3));

        pedido.TempoDecorrido(_relogio.GetUtcNow()).Should().Be(TimeSpan.FromMinutes(45));
    }

    [Fact]
    public async Task Cancelar_cancela_os_itens_ativos_com_o_mesmo_motivo()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ComItem("Costela").ConstruirEmPreparo());

        var resultado = await Cancelar(pedido.Id, "Cliente foi embora sem esperar");

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Cancelado);
        pedido.Itens.Should().AllSatisfy(item =>
            item.MotivoCancelamento!.Valor.Should().Be("Cliente foi embora sem esperar"));
        pedido.DomainEvents.Should().ContainItemsAssignableTo<PedidoCanceladoDomainEvent>();
    }

    [Fact]
    public async Task Cancelar_pedido_entregue_e_proibido()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEntregue());

        var resultado = await Cancelar(pedido.Id, "Cliente desistiu do pedido");

        resultado.Error.Codigo.Should().Be("Pedido.TransicaoInvalida");
        pedido.Status.Should().Be(StatusPedido.Entregue);
    }

    [Fact]
    public async Task Cancelar_sem_motivo_valido_e_recusado_pelo_value_object()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirConfirmado());

        var resultado = await Cancelar(pedido.Id, "abc");

        resultado.Error.Should().Be(MotivoCancelamento.MuitoCurto);
        pedido.Status.Should().Be(StatusPedido.Confirmado);
    }

    [Fact]
    public async Task ElevarPrioridade_persiste_a_escolha_manual()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirConfirmado());

        var resultado = await ElevarPrioridade(pedido.Id, "Urgente");

        resultado.Sucesso.Should().BeTrue();
        pedido.Prioridade.Should().Be(PrioridadePedido.Urgente);
        pedido.DomainEvents.Should().ContainItemsAssignableTo<PrioridadeDoPedidoElevadaDomainEvent>();
    }

    [Fact]
    public async Task ElevarPrioridade_nunca_rebaixa()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirConfirmado());
        await ElevarPrioridade(pedido.Id, "Urgente");

        var resultado = await ElevarPrioridade(pedido.Id, "Normal");

        resultado.Sucesso.Should().BeTrue();
        pedido.Prioridade.Should().Be(PrioridadePedido.Urgente);
    }

    [Theory]
    [InlineData("Urgente", true)]
    [InlineData("urgente", true)]
    [InlineData("Altissima", false)]
    public void Validator_aceita_apenas_prioridades_conhecidas(string prioridade, bool esperado)
    {
        var validacao = new ElevarPrioridadeDoPedidoCommandValidator()
            .Validate(new ElevarPrioridadeDoPedidoCommand(Guid.CreateVersion7(), prioridade));

        validacao.IsValid.Should().Be(esperado);
    }

    [Fact]
    public async Task Pedido_de_outro_tenant_devolve_nao_encontrado()
    {
        var pedido = PedidoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).ComItem().ConstruirPronto();
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        var resultado = await Entregar(pedido.Id);

        resultado.Error.Should().Be(PedidoErrors.NaoEncontrado);
        pedido.Status.Should().Be(StatusPedido.Pronto);
    }

    private Pedido PedidoDoTenant(Func<PedidoBuilder, Pedido> construir)
    {
        var pedido = construir(PedidoBuilder.Um().DoEstabelecimento(_tenant.EstabelecimentoId));
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        return pedido;
    }

    private Task<Result> Entregar(PedidoId pedidoId) =>
        new EntregarPedidoCommandHandler(_pedidos, _tenant, _relogio)
            .Handle(new EntregarPedidoCommand(pedidoId.Valor), TestContext.Current.CancellationToken);

    private Task<Result> Fechar(PedidoId pedidoId) =>
        new FecharPedidoCommandHandler(_pedidos, _tenant, _relogio)
            .Handle(new FecharPedidoCommand(pedidoId.Valor), TestContext.Current.CancellationToken);

    private Task<Result> Cancelar(PedidoId pedidoId, string motivo) =>
        new CancelarPedidoCommandHandler(_pedidos, _tenant, _relogio)
            .Handle(new CancelarPedidoCommand(pedidoId.Valor, motivo), TestContext.Current.CancellationToken);

    private Task<Result> ElevarPrioridade(PedidoId pedidoId, string prioridade) =>
        new ElevarPrioridadeDoPedidoCommandHandler(_pedidos, _tenant)
            .Handle(
                new ElevarPrioridadeDoPedidoCommand(pedidoId.Valor, prioridade),
                TestContext.Current.CancellationToken);
}
