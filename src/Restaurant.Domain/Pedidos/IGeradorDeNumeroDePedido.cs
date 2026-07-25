using Restaurant.Domain.Abstractions;
using Restaurant.Domain.Compartilhado;
using Restaurant.Domain.Pedidos.ValueObjects;

namespace Restaurant.Domain.Pedidos;

public interface IGeradorDeNumeroDePedido
{
    Task<Result<NumeroPedido>> GerarAsync(
        EstabelecimentoId estabelecimentoId,
        DateOnly dia,
        CancellationToken cancellationToken = default);
}
