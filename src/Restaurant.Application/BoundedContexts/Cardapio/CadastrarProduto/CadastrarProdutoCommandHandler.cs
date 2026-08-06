using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.BoundedContexts.Cardapio.CadastrarProduto;

internal sealed class CadastrarProdutoCommandHandler(
    IProdutoRepository produtos,
    ICategoriaRepository categorias,
    IVerificadorDeNomeUnicoDeProduto verificadorDeNome,
    ITenantContext tenant) : ICommandHandler<CadastrarProdutoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CadastrarProdutoCommand command, CancellationToken cancellationToken)
    {
        var categoriaId = new CategoriaId(command.CategoriaId);

        var categoria = await categorias.ObterPorIdAsync(categoriaId, cancellationToken);

        if (categoria is null || categoria.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure<Guid>(CategoriaErrors.NaoEncontrada);
        }

        if (!categoria.Ativa)
        {
            return Result.Failure<Guid>(CategoriaErrors.Desativada);
        }

        var nome = NomeDeProduto.Criar(command.Nome);
        var descricao = DescricaoDeProduto.Criar(command.Descricao);
        var preco = Dinheiro.Criar(command.Preco, Moeda.Real);
        var tempoDePreparo = TempoDePreparo.DeMinutos(command.MinutosDePreparo);

        var entradas = Result.PrimeiraFalha(nome, descricao, preco, tempoDePreparo);

        if (entradas.Falhou)
        {
            return Result.Failure<Guid>(entradas.Error);
        }

        var cadastro = await Produto.CadastrarAsync(
            tenant.EstabelecimentoId,
            categoriaId,
            nome.Value,
            descricao.Value,
            preco.Value,
            tempoDePreparo.Value,
            verificadorDeNome,
            cancellationToken);

        if (cadastro.Falhou)
        {
            return Result.Failure<Guid>(cadastro.Error);
        }

        produtos.Adicionar(cadastro.Value);

        return cadastro.Value.Id.Valor;
    }
}
