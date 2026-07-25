using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Pedidos.Ports;

public interface IPedidoRepository : IRepository<Pedido, PedidoId>
{
    Task<Pedido?> ObterAbertoDaMesaAsync(MesaId mesaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteAbertoParaMesaAsync(MesaId mesaId, CancellationToken cancellationToken = default);
}
