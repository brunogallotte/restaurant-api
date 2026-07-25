using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.Tenancy;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;

public sealed class Produto : AggregateRoot<ProdutoId>, ITenantScoped
{
    private Produto(
        ProdutoId id,
        EstabelecimentoId estabelecimentoId,
        CategoriaId categoriaId,
        NomeDeProduto nome,
        DescricaoDeProduto? descricao,
        Dinheiro preco,
        TempoDePreparo tempoDePreparo) : base(id)
    {
        EstabelecimentoId = estabelecimentoId;
        CategoriaId = categoriaId;
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        TempoDePreparo = tempoDePreparo;
        Disponibilidade = DisponibilidadeProduto.Disponivel;
        Ativo = true;
    }

    private Produto()
    {
        Nome = null!;
        Preco = null!;
        TempoDePreparo = null!;
        Disponibilidade = null!;
    }

    public EstabelecimentoId EstabelecimentoId { get; private set; }

    public CategoriaId CategoriaId { get; private set; }

    public NomeDeProduto Nome { get; private set; }

    public DescricaoDeProduto? Descricao { get; private set; }

    public Dinheiro Preco { get; private set; }

    public TempoDePreparo TempoDePreparo { get; private set; }

    public DisponibilidadeProduto Disponibilidade { get; private set; }

    public bool Ativo { get; private set; }

    public bool PodeEntrarEmPedido => Ativo && Disponibilidade.PodeSerPedido;

    public static async Task<Result<Produto>> CadastrarAsync(
        EstabelecimentoId estabelecimentoId,
        CategoriaId categoriaId,
        NomeDeProduto nome,
        DescricaoDeProduto? descricao,
        Dinheiro preco,
        TempoDePreparo tempoDePreparo,
        IVerificadorDeNomeUnicoDeProduto verificador,
        CancellationToken cancellationToken = default)
    {
        if (preco.EstaZerado)
        {
            return Result.Failure<Produto>(ProdutoErrors.PrecoZerado);
        }

        var nomeEhUnico = await verificador.EhUnicoAsync(
            estabelecimentoId,
            nome,
            ignorando: null,
            cancellationToken);

        if (!nomeEhUnico)
        {
            return Result.Failure<Produto>(ProdutoErrors.NomeJaUtilizado);
        }

        var produto = new Produto(
            ProdutoId.Novo(),
            estabelecimentoId,
            categoriaId,
            nome,
            descricao,
            preco,
            tempoDePreparo);

        produto.Raise(new ProdutoCadastradoDomainEvent(
            produto.Id,
            estabelecimentoId,
            categoriaId,
            nome.Valor,
            preco.Valor));

        return produto;
    }

    public Result AlterarPreco(Dinheiro novoPreco)
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        if (novoPreco.EstaZerado)
        {
            return Result.Failure(ProdutoErrors.PrecoZerado);
        }

        if (!novoPreco.MesmaMoedaQue(Preco))
        {
            return Result.Failure(Dinheiro.MoedasDiferentes);
        }

        if (novoPreco == Preco)
        {
            return Result.Success();
        }

        var precoAnterior = Preco;
        Preco = novoPreco;

        Raise(new PrecoDoProdutoAlteradoDomainEvent(
            Id,
            EstabelecimentoId,
            precoAnterior.Valor,
            novoPreco.Valor));

        return Result.Success();
    }

    public async Task<Result> RenomearAsync(
        NomeDeProduto novoNome,
        IVerificadorDeNomeUnicoDeProduto verificador,
        CancellationToken cancellationToken = default)
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        if (novoNome == Nome)
        {
            return Result.Success();
        }

        var nomeEhUnico = await verificador.EhUnicoAsync(
            EstabelecimentoId,
            novoNome,
            ignorando: Id,
            cancellationToken);

        if (!nomeEhUnico)
        {
            return Result.Failure(ProdutoErrors.NomeJaUtilizado);
        }

        Nome = novoNome;

        return Result.Success();
    }

    public Result AlterarDescricao(DescricaoDeProduto? novaDescricao)
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        Descricao = novaDescricao;

        return Result.Success();
    }

    public Result MoverParaCategoria(CategoriaId novaCategoria)
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        if (novaCategoria == CategoriaId)
        {
            return Result.Success();
        }

        var categoriaAnterior = CategoriaId;
        CategoriaId = novaCategoria;

        Raise(new ProdutoMovidoDeCategoriaDomainEvent(Id, categoriaAnterior, novaCategoria));

        return Result.Success();
    }

    public Result AlterarTempoDePreparo(TempoDePreparo novoTempo)
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        TempoDePreparo = novoTempo;

        return Result.Success();
    }

    public Result MarcarComoEsgotado()
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        if (Disponibilidade == DisponibilidadeProduto.Esgotado)
        {
            return Result.Failure(ProdutoErrors.JaEsgotado);
        }

        Disponibilidade = DisponibilidadeProduto.Esgotado;

        Raise(new ProdutoEsgotadoDomainEvent(Id, EstabelecimentoId, Nome.Valor));

        return Result.Success();
    }

    public Result Repor()
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.Descontinuado);
        }

        if (Disponibilidade == DisponibilidadeProduto.Disponivel)
        {
            return Result.Failure(ProdutoErrors.JaDisponivel);
        }

        Disponibilidade = DisponibilidadeProduto.Disponivel;

        Raise(new ProdutoRepostoDomainEvent(Id, EstabelecimentoId, Nome.Valor));

        return Result.Success();
    }

    public Result Descontinuar()
    {
        if (!Ativo)
        {
            return Result.Failure(ProdutoErrors.JaDescontinuado);
        }

        Ativo = false;

        Raise(new ProdutoDescontinuadoDomainEvent(Id, EstabelecimentoId, Nome.Valor));

        return Result.Success();
    }
}
