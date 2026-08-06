using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.BoundedContexts.Cardapio.AlterarPrecoDoProduto;

internal sealed class AlterarPrecoDoProdutoCommandHandler(
    IProdutoRepository produtos,
    ITenantContext tenant) : ICommandHandler<AlterarPrecoDoProdutoCommand>
{
    public async Task<Result> Handle(AlterarPrecoDoProdutoCommand command, CancellationToken cancellationToken)
    {
        var produto = await produtos.ObterPorIdAsync(new ProdutoId(command.ProdutoId), cancellationToken);

        if (produto is null || produto.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(ProdutoErrors.NaoEncontrado);
        }

        var novoPreco = Dinheiro.Criar(command.NovoPreco, Moeda.Real);

        if (novoPreco.Falhou)
        {
            return Result.Failure(novoPreco.Error);
        }

        return produto.AlterarPreco(novoPreco.Value);
    }
}
