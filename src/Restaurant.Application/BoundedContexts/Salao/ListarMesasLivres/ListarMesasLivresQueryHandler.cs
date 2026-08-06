using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.BoundedContexts.Salao.Contracts;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Salao.ListarMesasLivres;

internal sealed class ListarMesasLivresQueryHandler(
    IMesaRepository mesas,
    ITenantContext tenant) : IQueryHandler<ListarMesasLivresQuery, IReadOnlyList<MesaLivre>>
{
    public async Task<Result<IReadOnlyList<MesaLivre>>> Handle(
        ListarMesasLivresQuery query,
        CancellationToken cancellationToken)
    {
        var livres = await mesas.ListarLivresAsync(tenant.EstabelecimentoId, cancellationToken);

        IReadOnlyList<MesaLivre> comCapacidade =
        [
            .. livres
                .Where(mesa => mesa.Lugares >= query.LugaresMinimos)
                .OrderBy(mesa => mesa.Lugares)
                .ThenBy(mesa => mesa.Numero.Valor, StringComparer.Ordinal)
                .Select(Projetar)
        ];

        return Result.Success(comCapacidade);
    }

    private static MesaLivre Projetar(Mesa mesa) => new(mesa.Id.Valor, mesa.Numero.Valor, mesa.Lugares);
}
