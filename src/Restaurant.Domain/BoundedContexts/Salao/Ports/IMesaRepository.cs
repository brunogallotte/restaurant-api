using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Salao.Ports;

public interface IMesaRepository : IRepository<Mesa, MesaId>
{
    Task<Mesa?> ObterPorNumeroAsync(
        EstabelecimentoId estabelecimentoId,
        NumeroDaMesa numero,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Mesa>> ListarLivresAsync(
        EstabelecimentoId estabelecimentoId,
        CancellationToken cancellationToken = default);
}
