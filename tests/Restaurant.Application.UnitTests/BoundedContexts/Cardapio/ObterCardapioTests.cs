using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Cardapio.Contracts;
using Restaurant.Application.BoundedContexts.Cardapio.ObterCardapio;
using Restaurant.Application.BoundedContexts.Cardapio.Ports;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Cardapio;

public sealed class ObterCardapioTests
{
    private static readonly Guid CarnesId = Guid.CreateVersion7();
    private static readonly Guid BebidasId = Guid.CreateVersion7();

    private readonly ICardapioQueries _cardapio = Substitute.For<ICardapioQueries>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Lista_o_cardapio_do_estabelecimento_do_tenant()
    {
        CardapioTem(Item("Picanha", disponivel: true));

        var resultado = await Executar(apenasDisponiveis: false);

        resultado.Sucesso.Should().BeTrue();
        await _cardapio.Received(1).ListarAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ordena_por_categoria_e_depois_por_nome()
    {
        CardapioTem(
            Item("Refrigerante", disponivel: true, categoria: BebidasId, ordemDaCategoria: 2),
            Item("Picanha", disponivel: true),
            Item("Costela", disponivel: true),
            Item("Cerveja", disponivel: true, categoria: BebidasId, ordemDaCategoria: 2));

        var resultado = await Executar(apenasDisponiveis: false);

        resultado.Value.Select(item => item.Nome)
            .Should().Equal("Costela", "Picanha", "Cerveja", "Refrigerante");
    }

    [Fact]
    public async Task Filtra_indisponiveis_quando_pedido()
    {
        CardapioTem(
            Item("Picanha", disponivel: true),
            Item("Costela", disponivel: false));

        var resultado = await Executar(apenasDisponiveis: true);

        resultado.Value.Select(item => item.Nome).Should().Equal("Picanha");
    }

    [Fact]
    public async Task Mostra_indisponiveis_quando_nao_filtrado()
    {
        CardapioTem(
            Item("Picanha", disponivel: true),
            Item("Costela", disponivel: false));

        var resultado = await Executar(apenasDisponiveis: false);

        resultado.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Cardapio_vazio_e_sucesso_com_lista_vazia()
    {
        CardapioTem();

        var resultado = await Executar(apenasDisponiveis: false);

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.Should().BeEmpty();
    }

    private static ItemDoCardapio Item(
        string nome,
        bool disponivel,
        Guid? categoria = null,
        int ordemDaCategoria = 1) =>
        new(
            Guid.CreateVersion7(),
            nome,
            Descricao: null,
            Preco: 50m,
            MinutosDePreparo: 20,
            disponivel,
            categoria ?? CarnesId,
            categoria is null ? "Carnes" : "Bebidas",
            ordemDaCategoria);

    private void CardapioTem(params ItemDoCardapio[] itens) =>
        _cardapio
            .ListarAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>())
            .Returns(itens);

    private Task<Result<IReadOnlyList<ItemDoCardapio>>> Executar(bool apenasDisponiveis) =>
        new ObterCardapioQueryHandler(_cardapio, _tenant)
            .Handle(new ObterCardapioQuery(apenasDisponiveis), TestContext.Current.CancellationToken);
}
