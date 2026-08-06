using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.Builders;

internal sealed class CategoriaBuilder
{
    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private string _nome = "Carnes";
    private int _ordem = 1;

    public static CategoriaBuilder Uma() => new();

    public CategoriaBuilder DoEstabelecimento(EstabelecimentoId estabelecimentoId)
    {
        _estabelecimentoId = estabelecimentoId;
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
