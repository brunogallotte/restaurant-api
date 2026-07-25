using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.Events;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.Tenancy;

namespace Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;

public sealed class Categoria : AggregateRoot<CategoriaId>, ITenantScoped
{
    private Categoria(
        CategoriaId id,
        EstabelecimentoId estabelecimentoId,
        NomeDeCategoria nome,
        int ordem) : base(id)
    {
        EstabelecimentoId = estabelecimentoId;
        Nome = nome;
        Ordem = ordem;
        Ativa = true;
    }

    private Categoria()
    {
        Nome = null!;
    }

    public EstabelecimentoId EstabelecimentoId { get; private set; }

    public NomeDeCategoria Nome { get; private set; }

    public int Ordem { get; private set; }

    public bool Ativa { get; private set; }

    public static Result<Categoria> Criar(
        EstabelecimentoId estabelecimentoId,
        NomeDeCategoria nome,
        int ordem)
    {
        if (ordem < 0)
        {
            return Result.Failure<Categoria>(CategoriaErrors.OrdemNegativa);
        }

        var categoria = new Categoria(CategoriaId.Novo(), estabelecimentoId, nome, ordem);

        categoria.Raise(new CategoriaCriadaDomainEvent(categoria.Id, estabelecimentoId, nome.Valor, ordem));

        return categoria;
    }

    public Result Renomear(NomeDeCategoria novoNome)
    {
        if (!Ativa)
        {
            return Result.Failure(CategoriaErrors.Desativada);
        }

        if (novoNome == Nome)
        {
            return Result.Success();
        }

        var nomeAnterior = Nome;
        Nome = novoNome;

        Raise(new CategoriaRenomeadaDomainEvent(Id, nomeAnterior.Valor, novoNome.Valor));

        return Result.Success();
    }

    public Result Reordenar(int novaOrdem)
    {
        if (!Ativa)
        {
            return Result.Failure(CategoriaErrors.Desativada);
        }

        if (novaOrdem < 0)
        {
            return Result.Failure(CategoriaErrors.OrdemNegativa);
        }

        Ordem = novaOrdem;

        return Result.Success();
    }

    public Result Desativar()
    {
        if (!Ativa)
        {
            return Result.Failure(CategoriaErrors.JaDesativada);
        }

        Ativa = false;

        Raise(new CategoriaDesativadaDomainEvent(Id, EstabelecimentoId));

        return Result.Success();
    }
}
