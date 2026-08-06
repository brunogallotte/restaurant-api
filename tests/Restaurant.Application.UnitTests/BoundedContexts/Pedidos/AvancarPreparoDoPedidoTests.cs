using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.ConfirmarPedido;
using Restaurant.Application.BoundedContexts.Pedidos.IniciarPreparoDoPedido;
using Restaurant.Application.BoundedContexts.Pedidos.MarcarItemComoPronto;
using Restaurant.Application.BoundedContexts.Pedidos.MarcarPedidoComoPronto;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class AvancarPreparoDoPedidoTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(PedidoBuilder.AberturaPadrao.AddMinutes(5));

    [Fact]
    public async Task Confirmar_registra_o_instante_do_relogio_injetado()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().Construir());

        var resultado = await Confirmar(pedido.Id);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Confirmado);
        pedido.ConfirmadoEm.Should().Be(PedidoBuilder.AberturaPadrao.AddMinutes(5));
        pedido.DomainEvents.Should().ContainItemsAssignableTo<PedidoConfirmadoDomainEvent>();
    }

    [Fact]
    public async Task Confirmar_pedido_sem_item_e_recusado_pelo_agregado()
    {
        var pedido = PedidoDoTenant(builder => builder.Construir());

        var resultado = await Confirmar(pedido.Id);

        resultado.Error.Should().Be(PedidoErrors.SemItens);
        pedido.Status.Should().Be(StatusPedido.Aberto);
    }

    [Fact]
    public async Task Confirmar_pedido_de_outro_tenant_devolve_nao_encontrado()
    {
        var pedido = PedidoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).ComItem().Construir();
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        var resultado = await Confirmar(pedido.Id);

        resultado.Error.Should().Be(PedidoErrors.NaoEncontrado);
        pedido.Status.Should().Be(StatusPedido.Aberto);
    }

    [Fact]
    public async Task IniciarPreparo_leva_os_itens_pendentes_junto()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ComItem("Costela").ConstruirConfirmado());

        var resultado = await IniciarPreparo(pedido.Id);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.EmPreparo);
        pedido.Itens.Should().AllSatisfy(item => item.Status.Should().Be(StatusItemPedido.EmPreparo));
    }

    [Fact]
    public async Task IniciarPreparo_em_pedido_aberto_e_transicao_invalida()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().Construir());

        var resultado = await IniciarPreparo(pedido.Id);

        resultado.Error.Codigo.Should().Be("Pedido.TransicaoInvalida");
    }

    [Fact]
    public async Task MarcarItemComoPronto_avanca_so_o_item()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ComItem("Costela").ConstruirEmPreparo());

        var resultado = await MarcarItemPronto(pedido.Id, pedido.Itens[0].Id);

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].EstaPronto.Should().BeTrue();
        pedido.Itens[1].EstaPronto.Should().BeFalse();
        pedido.Status.Should().Be(StatusPedido.EmPreparo);
    }

    [Fact]
    public async Task MarcarItemComoPronto_de_item_inexistente_devolve_nao_encontrado()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEmPreparo());

        var resultado = await MarcarItemPronto(pedido.Id, ItemPedidoId.Novo());

        resultado.Error.Should().Be(PedidoErrors.ItemNaoEncontrado);
    }

    [Fact]
    public async Task MarcarPedidoComoPronto_exige_todos_os_itens_prontos()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ComItem("Costela").ConstruirEmPreparo());
        await MarcarItemPronto(pedido.Id, pedido.Itens[0].Id);

        var resultado = await MarcarPronto(pedido.Id);

        resultado.Error.Should().Be(PedidoErrors.ItensPendentes);
        pedido.Status.Should().Be(StatusPedido.EmPreparo);
    }

    [Fact]
    public async Task MarcarPedidoComoPronto_com_todos_os_itens_prontos_avanca()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ComItem("Costela").ConstruirEmPreparo());
        await MarcarItemPronto(pedido.Id, pedido.Itens[0].Id);
        await MarcarItemPronto(pedido.Id, pedido.Itens[1].Id);

        var resultado = await MarcarPronto(pedido.Id);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Pronto);
        pedido.ProntoEm.Should().Be(PedidoBuilder.AberturaPadrao.AddMinutes(5));
    }

    private Pedido PedidoDoTenant(Func<PedidoBuilder, Pedido> construir)
    {
        var pedido = construir(PedidoBuilder.Um().DoEstabelecimento(_tenant.EstabelecimentoId));
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        return pedido;
    }

    private Task<Result> Confirmar(PedidoId pedidoId) =>
        new ConfirmarPedidoCommandHandler(_pedidos, _tenant, _relogio)
            .Handle(new ConfirmarPedidoCommand(pedidoId.Valor), TestContext.Current.CancellationToken);

    private Task<Result> IniciarPreparo(PedidoId pedidoId) =>
        new IniciarPreparoDoPedidoCommandHandler(_pedidos, _tenant, _relogio)
            .Handle(new IniciarPreparoDoPedidoCommand(pedidoId.Valor), TestContext.Current.CancellationToken);

    private Task<Result> MarcarItemPronto(PedidoId pedidoId, ItemPedidoId itemId) =>
        new MarcarItemComoProntoCommandHandler(_pedidos, _tenant)
            .Handle(new MarcarItemComoProntoCommand(pedidoId.Valor, itemId.Valor), TestContext.Current.CancellationToken);

    private Task<Result> MarcarPronto(PedidoId pedidoId) =>
        new MarcarPedidoComoProntoCommandHandler(_pedidos, _tenant, _relogio)
            .Handle(new MarcarPedidoComoProntoCommand(pedidoId.Valor), TestContext.Current.CancellationToken);
}
