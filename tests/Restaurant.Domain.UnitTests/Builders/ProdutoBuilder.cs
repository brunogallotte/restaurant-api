using NSubstitute;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.UnitTests.Builders;

internal sealed class ProdutoBuilder
{
    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private CategoriaId _categoriaId = CategoriaId.Novo();
    private string _nome = "Picanha na Chapa";
    private string? _descricao;
    private decimal _preco = 89.90m;
    private int _minutosDePreparo = 25;

    public static ProdutoBuilder Um() => new();

    public ProdutoBuilder DoEstabelecimento(EstabelecimentoId id)
    {
        _estabelecimentoId = id;
        return this;
    }

    public ProdutoBuilder NaCategoria(CategoriaId id)
    {
        _categoriaId = id;
        return this;
    }

    public ProdutoBuilder Chamado(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoBuilder ComDescricao(string descricao)
    {
        _descricao = descricao;
        return this;
    }

    public ProdutoBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    public ProdutoBuilder ComPreparoDe(int minutos)
    {
        _minutosDePreparo = minutos;
        return this;
    }

    public static IVerificadorDeNomeUnicoDeProduto VerificadorQueAceita()
    {
        var verificador = Substitute.For<IVerificadorDeNomeUnicoDeProduto>();
        verificador
            .EhUnicoAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<NomeDeProduto>(), Arg.Any<ProdutoId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        return verificador;
    }

    public static IVerificadorDeNomeUnicoDeProduto VerificadorQueRecusa()
    {
        var verificador = Substitute.For<IVerificadorDeNomeUnicoDeProduto>();
        verificador
            .EhUnicoAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<NomeDeProduto>(), Arg.Any<ProdutoId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        return verificador;
    }

    public Produto Construir()
    {
        var produto = Produto.CadastrarAsync(
            _estabelecimentoId,
            _categoriaId,
            NomeDeProduto.Criar(_nome).Value,
            DescricaoDeProduto.Criar(_descricao).Value,
            Dinheiro.CriarEmReal(_preco).Value,
            TempoDePreparo.DeMinutos(_minutosDePreparo).Value,
            VerificadorQueAceita()).GetAwaiter().GetResult().Value;

        produto.ClearDomainEvents();

        return produto;
    }

    public Produto ConstruirEsgotado()
    {
        var produto = Construir();
        produto.MarcarComoEsgotado();
        produto.ClearDomainEvents();

        return produto;
    }

    public Produto ConstruirDescontinuado()
    {
        var produto = Construir();
        produto.Descontinuar();
        produto.ClearDomainEvents();

        return produto;
    }
}

internal sealed class CategoriaBuilder
{
    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private string _nome = "Carnes";
    private int _ordem;

    public static CategoriaBuilder Uma() => new();

    public CategoriaBuilder DoEstabelecimento(EstabelecimentoId id)
    {
        _estabelecimentoId = id;
        return this;
    }

    public CategoriaBuilder Chamada(string nome)
    {
        _nome = nome;
        return this;
    }

    public CategoriaBuilder NaOrdem(int ordem)
    {
        _ordem = ordem;
        return this;
    }

    public Categoria Construir()
    {
        var categoria = Categoria.Criar(
            _estabelecimentoId,
            NomeDeCategoria.Criar(_nome).Value,
            _ordem).Value;

        categoria.ClearDomainEvents();

        return categoria;
    }

    public Categoria ConstruirDesativada()
    {
        var categoria = Construir();
        categoria.Desativar();
        categoria.ClearDomainEvents();

        return categoria;
    }
}
