using MediatR;
using Microsoft.Extensions.Logging;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Ports;

namespace Restaurant.Application.BoundedContexts.Salao.EventHandlers;

internal sealed class LiberarMesaQuandoPedidoCancelado(
    IMesaRepository mesas,
    IUnitOfWork unitOfWork,
    ILogger<LiberarMesaQuandoPedidoCancelado> logger) : INotificationHandler<PedidoCanceladoDomainEvent>
{
    public async Task Handle(PedidoCanceladoDomainEvent notification, CancellationToken cancellationToken)
    {
        var mesa = await mesas.ObterPorIdAsync(notification.MesaId, cancellationToken);

        if (mesa is null)
        {
            logger.LogWarning(
                "Mesa {MesaId} do pedido {PedidoId} nao foi encontrada para liberacao.",
                notification.MesaId.Valor,
                notification.PedidoId.Valor);

            return;
        }

        var liberacao = mesa.Liberar(notification.CanceladoEm);

        if (liberacao.Falhou)
        {
            logger.LogWarning(
                "Mesa {MesaId} nao pode ser liberada pelo cancelamento do pedido {PedidoId}: {CodigoDoErro}.",
                notification.MesaId.Valor,
                notification.PedidoId.Valor,
                liberacao.Error.Codigo);

            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
