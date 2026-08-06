using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.BoundedContexts.Cardapio.MarcarProdutoComoEsgotado;

internal sealed class MarcarProdutoComoEsgotadoCommandHandler(
    IProdutoRepository produtos,
    ITenantContext tenant) : ICommandHandler<MarcarProdutoComoEsgotadoCommand>
{
    public async Task<Result> Handle(MarcarProdutoComoEsgotadoCommand command, CancellationToken cancellationToken)
    {
        var produto = await produtos.ObterPorIdAsync(new ProdutoId(command.ProdutoId), cancellationToken);

        if (produto is null || produto.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(ProdutoErrors.NaoEncontrado);
        }

        return produto.MarcarComoEsgotado();
    }
}
