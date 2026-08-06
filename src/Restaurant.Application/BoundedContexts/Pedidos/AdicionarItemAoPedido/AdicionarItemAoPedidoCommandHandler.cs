using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.BoundedContexts.Pedidos.AdicionarItemAoPedido;

internal sealed class AdicionarItemAoPedidoCommandHandler(
    IPedidoRepository pedidos,
    IProdutoRepository produtos,
    ITenantContext tenant,
    TimeProvider relogio) : ICommandHandler<AdicionarItemAoPedidoCommand>
{
    public async Task<Result> Handle(AdicionarItemAoPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoId(command.PedidoId), cancellationToken);

        if (pedido is null || pedido.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(PedidoErrors.NaoEncontrado);
        }

        var produto = await produtos.ObterPorIdAsync(new ProdutoId(command.ProdutoId), cancellationToken);

        if (produto is null || produto.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure(ProdutoErrors.NaoEncontrado);
        }

        if (!produto.PodeEntrarEmPedido)
        {
            return Result.Failure(ProdutoErrors.NaoPodeEntrarEmPedido);
        }

        var produtoDoPedido = ProdutoDoPedido.Criar(produto.Id, produto.Nome.Valor, produto.Preco);
        var quantidade = Quantidade.Criar(command.Quantidade);
        var observacao = Observacao.Criar(command.Observacao);

        var entradas = Result.PrimeiraFalha(produtoDoPedido, quantidade, observacao);

        if (entradas.Falhou)
        {
            return Result.Failure(entradas.Error);
        }

        return pedido.AdicionarItem(
            produtoDoPedido.Value,
            quantidade.Value,
            observacao.Value,
            relogio.GetUtcNow());
    }
}
