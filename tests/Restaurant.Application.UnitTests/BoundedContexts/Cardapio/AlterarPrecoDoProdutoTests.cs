using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Cardapio.AlterarPrecoDoProduto;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.BoundedContexts.Cardapio;

public sealed class AlterarPrecoDoProdutoTests
{
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Altera_o_preco_e_levanta_o_evento()
    {
        var produto = ProdutoDoTenant(ProdutoBuilder.Um().ComPreco(89.90m));

        var resultado = await Executar(new AlterarPrecoDoProdutoCommand(produto.Id.Valor, 99.90m));

        resultado.Sucesso.Should().BeTrue();
        produto.Preco.Valor.Should().Be(99.90m);
        produto.DomainEvents.Should().ContainItemsAssignableTo<PrecoDoProdutoAlteradoDomainEvent>();
    }

    [Fact]
    public async Task Produto_de_outro_tenant_devolve_nao_encontrado()
    {
        var produto = ProdutoBuilder.Um().DoEstabelecimento(EstabelecimentoId.Novo()).Construir();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await Executar(new AlterarPrecoDoProdutoCommand(produto.Id.Valor, 99.90m));

        resultado.Error.Should().Be(ProdutoErrors.NaoEncontrado);
        produto.Preco.Valor.Should().Be(89.90m);
    }

    [Fact]
    public async Task Produto_inexistente_devolve_nao_encontrado()
    {
        _produtos.ObterPorIdAsync(Arg.Any<ProdutoId>(), Arg.Any<CancellationToken>()).Returns((Produto?)null);

        var resultado = await Executar(new AlterarPrecoDoProdutoCommand(Guid.CreateVersion7(), 99.90m));

        resultado.Error.Should().Be(ProdutoErrors.NaoEncontrado);
    }

    [Fact]
    public async Task Preco_negativo_e_recusado_antes_de_tocar_no_agregado()
    {
        var produto = ProdutoDoTenant(ProdutoBuilder.Um().ComPreco(89.90m));

        var resultado = await Executar(new AlterarPrecoDoProdutoCommand(produto.Id.Valor, -1m));

        resultado.Error.Should().Be(Dinheiro.Negativo);
        produto.Preco.Valor.Should().Be(89.90m);
    }

    [Fact]
    public async Task Produto_descontinuado_nao_muda_de_preco()
    {
        var produto = ProdutoBuilder.Um()
            .DoEstabelecimento(_tenant.EstabelecimentoId)
            .ConstruirDescontinuado();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await Executar(new AlterarPrecoDoProdutoCommand(produto.Id.Valor, 99.90m));

        resultado.Error.Should().Be(ProdutoErrors.Descontinuado);
    }

    private Produto ProdutoDoTenant(ProdutoBuilder builder)
    {
        var produto = builder.DoEstabelecimento(_tenant.EstabelecimentoId).Construir();
        _produtos.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(produto);

        return produto;
    }

    private Task<Result> Executar(AlterarPrecoDoProdutoCommand comando) =>
        new AlterarPrecoDoProdutoCommandHandler(_produtos, _tenant)
            .Handle(comando, TestContext.Current.CancellationToken);
}
