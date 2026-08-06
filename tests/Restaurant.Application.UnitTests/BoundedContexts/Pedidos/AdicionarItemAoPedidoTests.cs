using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.AdicionarItemAoPedido;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class AdicionarItemAoPedidoTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(PedidoBuilder.AberturaPadrao);

    [Fact]
    public async Task Copia_nome_e_preco_do_produto_para_dentro_do_pedido()
    {
        var pedido = PedidoDoTenant(builder => builder.Construir());
        var produto = ProdutoDoTenant("Picanha na Chapa", 89.90m);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 2);

        resultado.Sucesso.Should().BeTrue();
        var item = pedido.Itens.Should().ContainSingle().Subject;
        item.Produto.Nome.Should().Be("Picanha na Chapa");
        item.Produto.PrecoUnitario.Valor.Should().Be(89.90m);
        item.Quantidade.Valor.Should().Be(2);
    }

    [Fact]
    public async Task Preco_no_pedido_nao_muda_quando_o_produto_muda_depois()
    {
        var pedido = PedidoDoTenant(builder => builder.Construir());
        var produto = ProdutoDoTenant("Picanha", 89.90m);
        await Executar(pedido.Id, produto.Id, quantidade: 1);

        produto.AlterarPreco(Domain.SharedKernel.ValueObjects.Dinheiro.CriarEmReal(129.90m).Value);

        pedido.Itens[0].Produto.PrecoUnitario.Valor.Should().Be(89.90m);
        pedido.Total.Valor.Should().Be(89.90m);
    }

    [Fact]
    public async Task Produto_esgotado_nao_entra_no_pedido()
    {
        var pedido = PedidoDoTenant(builder => builder.Construir());
        var produto = ProdutoBuilder.Um()
            .DoEstabelecimento(_tenant.EstabelecimentoId)
            .ConstruirEsgotado();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 1);

        resultado.Error.Should().Be(ProdutoErrors.NaoPodeEntrarEmPedido);
        pedido.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Produto_de_outro_tenant_devolve_nao_encontrado()
    {
        var pedido = PedidoDoTenant(builder => builder.Construir());
        var produto = ProdutoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).Construir();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 1);

        resultado.Error.Should().Be(ProdutoErrors.NaoEncontrado);
    }

    [Fact]
    public async Task Pedido_de_outro_tenant_devolve_nao_encontrado()
    {
        var pedido = PedidoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).Construir();
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);
        var produto = ProdutoDoTenant("Picanha", 89.90m);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 1);

        resultado.Error.Should().Be(PedidoErrors.NaoEncontrado);
        pedido.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Quantidade_fora_da_faixa_e_recusada()
    {
        var pedido = PedidoDoTenant(builder => builder.Construir());
        var produto = ProdutoDoTenant("Picanha", 89.90m);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 0);

        resultado.Error.Should().Be(Quantidade.ForaDaFaixa);
        pedido.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Pedido_entregue_nao_aceita_item_novo()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirEntregue());
        var produto = ProdutoDoTenant("Picanha", 89.90m);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 1);

        resultado.Error.Codigo.Should().Be("Pedido.NaoAceitaItens");
    }

    [Fact]
    public async Task Item_novo_em_pedido_pronto_derruba_ele_de_volta_para_preparo()
    {
        var pedido = PedidoDoTenant(builder => builder.ComItem().ConstruirPronto());
        var produto = ProdutoDoTenant("Costela", 79.90m);

        var resultado = await Executar(pedido.Id, produto.Id, quantidade: 1);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Nome.Should().Be("EmPreparo");
    }

    private Pedido PedidoDoTenant(Func<PedidoBuilder, Pedido> construir)
    {
        var pedido = construir(PedidoBuilder.Um().DoEstabelecimento(_tenant.EstabelecimentoId));
        _pedidos.ObterPorIdAsync(pedido.Id, Arg.Any<CancellationToken>()).Returns(pedido);

        return pedido;
    }

    private Produto ProdutoDoTenant(string nome, decimal preco)
    {
        var produto = ProdutoBuilder.Um()
            .DoEstabelecimento(_tenant.EstabelecimentoId)
            .Chamado(nome)
            .ComPreco(preco)
            .Construir();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        return produto;
    }

    private Task<Result> Executar(PedidoId pedidoId, ProdutoId produtoId, int quantidade) =>
        new AdicionarItemAoPedidoCommandHandler(_pedidos, _produtos, _tenant, _relogio)
            .Handle(
                new AdicionarItemAoPedidoCommand(pedidoId.Valor, produtoId.Valor, quantidade, null),
                TestContext.Current.CancellationToken);
}
