using MediatR;
using Microsoft.Extensions.Logging;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Ports;

namespace Restaurant.Application.BoundedContexts.Salao.EventHandlers;

internal sealed class OcuparMesaQuandoPedidoAberto(
    IMesaRepository mesas,
    IUnitOfWork unitOfWork,
    ILogger<OcuparMesaQuandoPedidoAberto> logger) : INotificationHandler<PedidoAbertoDomainEvent>
{
    public async Task Handle(PedidoAbertoDomainEvent notification, CancellationToken cancellationToken)
    {
        var mesa = await mesas.ObterPorIdAsync(notification.MesaId, cancellationToken);

        if (mesa is null)
        {
            logger.LogWarning(
                "Mesa {MesaId} do pedido {PedidoId} nao foi encontrada para ocupacao.",
                notification.MesaId.Valor,
                notification.PedidoId.Valor);

            return;
        }

        var ocupacao = mesa.Ocupar(notification.AbertoEm);

        if (ocupacao.Falhou)
        {
            logger.LogWarning(
                "Mesa {MesaId} nao pode ser ocupada pelo pedido {PedidoId}: {CodigoDoErro}.",
                notification.MesaId.Valor,
                notification.PedidoId.Valor,
                ocupacao.Error.Codigo);

            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
