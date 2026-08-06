using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Cardapio.CadastrarProduto;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Cardapio;

public sealed class CadastrarProdutoTests
{
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IVerificadorDeNomeUnicoDeProduto _verificadorDeNome =
        Substitute.For<IVerificadorDeNomeUnicoDeProduto>();

    private readonly TenantContextFake _tenant = new();
    private readonly CategoriaId _categoriaId = CategoriaId.Novo();

    public CadastrarProdutoTests() => NomeEhUnico(true);

    [Fact]
    public async Task Cadastra_o_produto_na_categoria_informada()
    {
        CategoriaDoTenant();
        Produto? adicionado = null;
        _produtos.Adicionar(Arg.Do<Produto>(argumento => adicionado = argumento));

        var resultado = await Executar(Comando());

        resultado.Sucesso.Should().BeTrue();
        adicionado!.EstabelecimentoId.Should().Be(_tenant.EstabelecimentoId);
        adicionado.CategoriaId.Should().Be(_categoriaId);
        adicionado.Preco.Valor.Should().Be(89.90m);
    }

    [Fact]
    public async Task Categoria_de_outro_tenant_devolve_nao_encontrada()
    {
        _categorias
            .ObterPorIdAsync(_categoriaId, Arg.Any<CancellationToken>())
            .Returns(CategoriaBuilder.Uma().DoEstabelecimento(EstabelecimentoId.Novo()).Construir());

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(CategoriaErrors.NaoEncontrada);
        _produtos.DidNotReceive().Adicionar(Arg.Any<Produto>());
    }

    [Fact]
    public async Task Categoria_inexistente_devolve_nao_encontrada()
    {
        _categorias
            .ObterPorIdAsync(Arg.Any<CategoriaId>(), Arg.Any<CancellationToken>())
            .Returns((Categoria?)null);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(CategoriaErrors.NaoEncontrada);
    }

    [Fact]
    public async Task Categoria_desativada_nao_recebe_produto_novo()
    {
        _categorias
            .ObterPorIdAsync(_categoriaId, Arg.Any<CancellationToken>())
            .Returns(CategoriaBuilder.Uma()
                .DoEstabelecimento(_tenant.EstabelecimentoId)
                .ConstruirDesativada());

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(CategoriaErrors.Desativada);
    }

    [Fact]
    public async Task Nome_repetido_no_estabelecimento_e_recusado_pela_factory_do_agregado()
    {
        CategoriaDoTenant();
        NomeEhUnico(false);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(ProdutoErrors.NomeJaUtilizado);
        _produtos.DidNotReceive().Adicionar(Arg.Any<Produto>());
    }

    [Fact]
    public async Task Preco_zerado_e_recusado()
    {
        CategoriaDoTenant();

        var resultado = await Executar(Comando() with { Preco = 0m });

        resultado.Error.Should().Be(ProdutoErrors.PrecoZerado);
    }

    [Fact]
    public async Task Tempo_de_preparo_fora_da_faixa_e_recusado()
    {
        CategoriaDoTenant();

        var resultado = await Executar(Comando() with { MinutosDePreparo = 0 });

        resultado.Error.Should().Be(TempoDePreparo.ForaDaFaixa);
    }

    private void CategoriaDoTenant() =>
        _categorias
            .ObterPorIdAsync(_categoriaId, Arg.Any<CancellationToken>())
            .Returns(CategoriaBuilder.Uma().DoEstabelecimento(_tenant.EstabelecimentoId).Construir());

    private void NomeEhUnico(bool unico) =>
        _verificadorDeNome
            .EhUnicoAsync(
                Arg.Any<EstabelecimentoId>(),
                Arg.Any<NomeDeProduto>(),
                Arg.Any<ProdutoId?>(),
                Arg.Any<CancellationToken>())
            .Returns(unico);

    private CadastrarProdutoCommand Comando() =>
        new(_categoriaId.Valor, "Picanha na Chapa", "Acompanha farofa", 89.90m, 25);

    private Task<Result<Guid>> Executar(CadastrarProdutoCommand comando) =>
        new CadastrarProdutoCommandHandler(_produtos, _categorias, _verificadorDeNome, _tenant)
            .Handle(comando, TestContext.Current.CancellationToken);
}
