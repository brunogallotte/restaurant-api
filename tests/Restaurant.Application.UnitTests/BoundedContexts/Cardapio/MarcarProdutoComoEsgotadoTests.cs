using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Cardapio.MarcarProdutoComoEsgotado;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Cardapio;

public sealed class MarcarProdutoComoEsgotadoTests
{
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Marca_o_produto_como_esgotado_e_tira_ele_do_pedido()
    {
        var produto = ProdutoDoTenant(builder => builder.Construir());

        var resultado = await Executar(produto.Id);

        resultado.Sucesso.Should().BeTrue();
        produto.PodeEntrarEmPedido.Should().BeFalse();
        produto.DomainEvents.Should().ContainItemsAssignableTo<ProdutoEsgotadoDomainEvent>();
    }

    [Fact]
    public async Task Produto_de_outro_tenant_devolve_nao_encontrado()
    {
        var produto = ProdutoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).Construir();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await Executar(produto.Id);

        resultado.Error.Should().Be(ProdutoErrors.NaoEncontrado);
        produto.PodeEntrarEmPedido.Should().BeTrue();
    }

    [Fact]
    public async Task Produto_ja_esgotado_e_recusado_pelo_agregado()
    {
        var produto = ProdutoDoTenant(builder => builder.ConstruirEsgotado());

        var resultado = await Executar(produto.Id);

        resultado.Error.Should().Be(ProdutoErrors.JaEsgotado);
    }

    private Produto ProdutoDoTenant(Func<ProdutoBuilder, Produto> construir)
    {
        var produto = construir(ProdutoBuilder.Um().DoEstabelecimento(_tenant.EstabelecimentoId));
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        return produto;
    }

    private Task<Result> Executar(ProdutoId produtoId) =>
        new MarcarProdutoComoEsgotadoCommandHandler(_produtos, _tenant)
            .Handle(new MarcarProdutoComoEsgotadoCommand(produtoId.Valor), TestContext.Current.CancellationToken);
}
