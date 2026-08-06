using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.CancelarItemDoPedido;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class CancelarItemDoPedidoTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Item_pendente_cancela_sem_motivo()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().Construir());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, motivo: null);

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].EstaCancelado.Should().BeTrue();
        pedido.DomainEvents.Should().ContainItemsAssignableTo<ItemDoPedidoCanceladoDomainEvent>();
    }

    [Fact]
    public async Task Item_que_ja_produziu_exige_motivo()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEmPreparo());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, motivo: null);

        resultado.Error.Should().Be(PedidoErrors.MotivoDeCancelamentoObrigatorio);
        pedido.Itens[0].EstaCancelado.Should().BeFalse();
    }

    [Fact]
    public async Task Item_que_ja_produziu_cancela_com_motivo()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEmPreparo());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, "Cliente desistiu do prato");

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].MotivoCancelamento!.Valor.Should().Be("Cliente desistiu do prato");
    }

    [Fact]
    public async Task Motivo_curto_demais_e_recusado_pelo_value_object()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEmPreparo());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, "abc");

        resultado.Error.Should().Be(MotivoCancelamento.MuitoCurto);
        pedido.Itens[0].EstaCancelado.Should().BeFalse();
    }

    [Fact]
    public async Task Item_ja_cancelado_e_recusado()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().Construir());
        await Executar(pedido.Id, pedido.Itens[0].Id, motivo: null);

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, motivo: null);

        resultado.Error.Should().Be(PedidoErrors.ItemJaCancelado);
    }

    private Pedido PedidoDoTenant(Func<PedidoBuilder, Pedido> construir)
    {
        var pedido = construir(PedidoBuilder.Um().DoEstabelecimento(_tenant.EstabelecimentoId));
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        return pedido;
    }

    private Task<Result> Executar(PedidoId pedidoId, ItemPedidoId itemId, string? motivo) =>
        new CancelarItemDoPedidoCommandHandler(_pedidos, _tenant)
            .Handle(
                new CancelarItemDoPedidoCommand(pedidoId.Valor, itemId.Valor, motivo),
                TestContext.Current.CancellationToken);
}
