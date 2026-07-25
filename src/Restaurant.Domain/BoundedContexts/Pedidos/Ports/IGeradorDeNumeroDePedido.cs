using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Pedidos.Ports;

public interface IGeradorDeNumeroDePedido
{
    Task<Result<NumeroPedido>> GerarAsync(
        EstabelecimentoId estabelecimentoId,
        DateOnly dia,
        CancellationToken cancellationToken = default);
}
