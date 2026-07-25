using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;

public sealed class ItemPedido : Entity<ItemPedidoId>
{
    private ItemPedido(
        ItemPedidoId id,
        ProdutoDoPedido produto,
        Quantidade quantidade,
        Observacao? observacao) : base(id)
    {
        Produto = produto;
        Quantidade = quantidade;
        Observacao = observacao;
        Status = StatusItemPedido.Pendente;
    }

    private ItemPedido()
    {
        Produto = null!;
        Quantidade = null!;
        Status = null!;
    }

    public ProdutoDoPedido Produto { get; private set; }

    public Quantidade Quantidade { get; private set; }

    public Observacao? Observacao { get; private set; }

    public StatusItemPedido Status { get; private set; }

    public MotivoCancelamento? MotivoCancelamento { get; private set; }

    public ProdutoId ProdutoId => Produto.ProdutoId;

    public bool EstaCancelado => Status == StatusItemPedido.Cancelado;

    public bool EstaPronto => Status == StatusItemPedido.Pronto;

    public Dinheiro Total => Produto.PrecoUnitario.MultiplicarPor(Quantidade.Valor);

    internal static ItemPedido Criar(ProdutoDoPedido produto, Quantidade quantidade, Observacao? observacao) =>
        new(ItemPedidoId.Novo(), produto, quantidade, observacao);

    internal Result AlterarQuantidade(Quantidade novaQuantidade)
    {
        if (!Status.PodeSerAlterado)
        {
            return Result.Failure(PedidoErrors.ItemNaoPodeSerAlterado);
        }

        Quantidade = novaQuantidade;

        return Result.Success();
    }

    internal Result Cancelar(MotivoCancelamento? motivo)
    {
        if (EstaCancelado)
        {
            return Result.Failure(PedidoErrors.ItemJaCancelado);
        }

        if (Status.JaEntrouEmProducao && motivo is null)
        {
            return Result.Failure(PedidoErrors.MotivoDeCancelamentoObrigatorio);
        }

        MotivoCancelamento = motivo;
        Status = StatusItemPedido.Cancelado;

        return Result.Success();
    }

    internal Result TransicionarPara(StatusItemPedido destino)
    {
        if (!Status.PodeTransicionarPara(destino))
        {
            return Result.Failure(PedidoErrors.TransicaoDeItemInvalida(Status, destino));
        }

        Status = destino;

        return Result.Success();
    }
}
