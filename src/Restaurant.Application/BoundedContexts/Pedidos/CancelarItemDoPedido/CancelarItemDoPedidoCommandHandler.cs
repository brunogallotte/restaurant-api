using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.CancelarItemDoPedido;

internal sealed class CancelarItemDoPedidoCommandHandler(
    IPedidoRepository pedidos,
    ITenantContext tenant) : ICommandHandler<CancelarItemDoPedidoCommand>
{
    public async Task<Result> Handle(CancelarItemDoPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        var motivo = CriarMotivo(command.Motivo);

        if (motivo.Falhou)
        {
            return Result.Failure(motivo.Error);
        }

        return pedido.CancelarItem(new ItemPedidoId(command.ItemId), motivo.Value);
    }

    private static Result<MotivoCancelamento?> CriarMotivo(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Success<MotivoCancelamento?>(null);
        }

        return MotivoCancelamento.Criar(entrada).Map(motivo => (MotivoCancelamento?)motivo);
    }
}
