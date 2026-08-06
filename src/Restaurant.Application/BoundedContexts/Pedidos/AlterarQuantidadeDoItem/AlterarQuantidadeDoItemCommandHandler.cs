using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.AlterarQuantidadeDoItem;

internal sealed class AlterarQuantidadeDoItemCommandHandler(
    IPedidoRepository pedidos,
    ITenantContext tenant) : ICommandHandler<AlterarQuantidadeDoItemCommand>
{
    public async Task<Result> Handle(AlterarQuantidadeDoItemCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        var novaQuantidade = Quantidade.Criar(command.NovaQuantidade);

        if (novaQuantidade.Falhou)
        {
            return Result.Failure(novaQuantidade.Error);
        }

        return pedido.AlterarQuantidadeDoItem(new ItemPedidoId(command.ItemId), novaQuantidade.Value);
    }
}
