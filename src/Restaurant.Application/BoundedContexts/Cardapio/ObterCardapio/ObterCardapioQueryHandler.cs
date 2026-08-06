using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.BoundedContexts.Cardapio.Contracts;
using Restaurant.Application.BoundedContexts.Cardapio.Ports;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Cardapio.ObterCardapio;

internal sealed class ObterCardapioQueryHandler(
    ICardapioQueries cardapio,
    ITenantContext tenant) : IQueryHandler<ObterCardapioQuery, IReadOnlyList<ItemDoCardapio>>
{
    public async Task<Result<IReadOnlyList<ItemDoCardapio>>> Handle(
        ObterCardapioQuery query,
        CancellationToken cancellationToken)
    {
        var itens = await cardapio.ListarAsync(tenant.EstabelecimentoId, cancellationToken);

        IReadOnlyList<ItemDoCardapio> visiveis =
        [
            .. itens
                .Where(item => item.Disponivel || !query.ApenasDisponiveis)
                .OrderBy(item => item.OrdemDaCategoria)
                .ThenBy(item => item.Nome, StringComparer.Ordinal)
        ];

        return Result.Success(visiveis);
    }
}
