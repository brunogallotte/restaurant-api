using Restaurant.Application.BoundedContexts.Cardapio.Contracts;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.BoundedContexts.Cardapio.Ports;

public interface ICardapioQueries
{
    Task<IReadOnlyList<ItemDoCardapio>> ListarAsync(
        EstabelecimentoId estabelecimentoId,
        CancellationToken cancellationToken = default);
}
