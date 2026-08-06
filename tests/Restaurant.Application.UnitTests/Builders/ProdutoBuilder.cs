using NSubstitute;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.Builders;

internal sealed class ProdutoBuilder
{
    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private CategoriaId _categoriaId = CategoriaId.Novo();
    private string _nome = "Picanha";
    private decimal _preco = 89.90m;

    public static ProdutoBuilder Um() => new();

    public ProdutoBuilder DoEstabelecimento(EstabelecimentoId estabelecimentoId)
    {
        _estabelecimentoId = estabelecimentoId;
        return this;
    }

    public ProdutoBuilder NaCategoria(CategoriaId categoriaId)
    {
        _categoriaId = categoriaId;
        return this;
    }

    public ProdutoBuilder Chamado(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    public Produto Construir()
    {
        var verificador = Substitute.For<IVerificadorDeNomeUnicoDeProduto>();
        verificador
            .EhUnicoAsync(
                Arg.Any<EstabelecimentoId>(),
                Arg.Any<NomeDeProduto>(),
                Arg.Any<ProdutoId?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var produto = Produto.CadastrarAsync(
            _estabelecimentoId,
            _categoriaId,
            NomeDeProduto.Criar(_nome).Value,
            descricao: null,
            Dinheiro.Criar(_preco, Moeda.Real).Value,
            TempoDePreparo.DeMinutos(25).Value,
            verificador).GetAwaiter().GetResult();

        produto.Value.ClearDomainEvents();

        return produto.Value;
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
