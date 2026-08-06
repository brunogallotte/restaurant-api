using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.ElevarPrioridadeDoPedido;

internal sealed class ElevarPrioridadeDoPedidoCommandHandler(
    IPedidoRepository pedidos,
    ITenantContext tenant) : ICommandHandler<ElevarPrioridadeDoPedidoCommand>
{
    public async Task<Result> Handle(ElevarPrioridadeDoPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        return pedido.ElevarPrioridade(PrioridadePedido.DeNome(command.Prioridade));
    }
}
