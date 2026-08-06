using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.ConfirmarPedido;

internal sealed class ConfirmarPedidoCommandHandler(
    IPedidoRepository pedidos,
    ITenantContext tenant,
    TimeProvider relogio) : ICommandHandler<ConfirmarPedidoCommand>
{
    public async Task<Result> Handle(ConfirmarPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        return pedido.Confirmar(relogio.GetUtcNow());
    }
}
