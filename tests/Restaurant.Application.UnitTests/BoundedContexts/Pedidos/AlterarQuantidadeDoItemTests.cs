using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.AlterarQuantidadeDoItem;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class AlterarQuantidadeDoItemTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Altera_a_quantidade_do_item_pendente()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem(quantidade: 1).Construir());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, novaQuantidade: 3);

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].Quantidade.Valor.Should().Be(3);
        pedido.DomainEvents.Should().ContainItemsAssignableTo<QuantidadeDoItemAlteradaDomainEvent>();
    }

    [Fact]
    public async Task Item_ja_em_preparo_nao_muda_de_quantidade()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem(quantidade: 1).ConstruirEmPreparo());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, novaQuantidade: 3);

        resultado.Error.Should().Be(PedidoErrors.ItemNaoPodeSerAlterado);
        pedido.Itens[0].Quantidade.Valor.Should().Be(1);
    }

    [Fact]
    public async Task Item_de_outro_pedido_devolve_nao_encontrado()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().Construir());

        var resultado = await Executar(pedido.Id, ItemPedidoId.Novo(), novaQuantidade: 3);

        resultado.Error.Should().Be(PedidoErrors.ItemNaoEncontrado);
    }

    [Fact]
    public async Task Pedido_de_outro_tenant_devolve_nao_encontrado()
    {
        var pedido = PedidoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).ComItem().Construir();
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, novaQuantidade: 3);

        resultado.Error.Should().Be(PedidoErrors.NaoEncontrado);
    }

    [Fact]
    public async Task Quantidade_acima_do_maximo_e_recusada_antes_do_agregado()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem(quantidade: 1).Construir());

        var resultado = await Executar(pedido.Id, pedido.Itens[0].Id, novaQuantidade: 100);

        resultado.Error.Should().Be(Quantidade.ForaDaFaixa);
        pedido.Itens[0].Quantidade.Valor.Should().Be(1);
    }

    private Pedido PedidoDoTenant(Func<PedidoBuilder, Pedido> construir)
    {
        var pedido = construir(PedidoBuilder.Um().DoEstabelecimento(_tenant.EstabelecimentoId));
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        return pedido;
    }

    private Task<Result> Executar(PedidoId pedidoId, ItemPedidoId itemId, int novaQuantidade) =>
        new AlterarQuantidadeDoItemCommandHandler(_pedidos, _tenant)
            .Handle(
                new AlterarQuantidadeDoItemCommand(pedidoId.Valor, itemId.Valor, novaQuantidade),
                TestContext.Current.CancellationToken);
}
