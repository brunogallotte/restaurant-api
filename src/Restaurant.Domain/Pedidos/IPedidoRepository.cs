using Restaurant.Domain.Abstractions;
using Restaurant.Domain.Compartilhado;

namespace Restaurant.Domain.Pedidos;

public interface IPedidoRepository : IRepository<Pedido, PedidoId>
{
    Task<Pedido?> ObterAbertoDaMesaAsync(MesaId mesaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteAbertoParaMesaAsync(MesaId mesaId, CancellationToken cancellationToken = default);
}
