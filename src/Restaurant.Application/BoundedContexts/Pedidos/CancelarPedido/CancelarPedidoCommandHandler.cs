using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.CancelarPedido;

internal sealed class CancelarPedidoCommandHandler(
    IPedidoRepository pedidos,
    ITenantContext tenant,
    TimeProvider relogio) : ICommandHandler<CancelarPedidoCommand>
{
    public async Task<Result> Handle(CancelarPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        var motivo = MotivoCancelamento.Criar(command.Motivo);

        if (motivo.Falhou)
        {
            return Result.Failure(motivo.Error);
        }

        return pedido.Cancelar(motivo.Value, relogio.GetUtcNow());
    }
}
