using Restaurant.Application.BoundedContexts.Pedidos.Contracts;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.BoundedContexts.Pedidos.Ports;

public interface IPedidoQueries
{
    Task<IReadOnlyList<PedidoEmAndamento>> ListarEmAndamentoAsync(
        EstabelecimentoId estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<PedidoLido?> ObterPorIdAsync(
        EstabelecimentoId estabelecimentoId,
        PedidoId pedidoId,
        CancellationToken cancellationToken = default);
}
