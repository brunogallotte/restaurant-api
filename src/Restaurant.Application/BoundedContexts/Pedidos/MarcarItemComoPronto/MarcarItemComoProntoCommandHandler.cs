using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.MarcarItemComoPronto;

internal sealed class MarcarItemComoProntoCommandHandler(
    IPedidoRepository pedidos,
    ITenantContext tenant) : ICommandHandler<MarcarItemComoProntoCommand>
{
    public async Task<Result> Handle(MarcarItemComoProntoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        return pedido.MarcarItemComoPronto(new ItemPedidoId(command.ItemId));
    }
}
