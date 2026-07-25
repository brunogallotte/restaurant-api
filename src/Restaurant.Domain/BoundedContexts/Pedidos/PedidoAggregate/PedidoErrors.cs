using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;

public static class PedidoErrors
{
    public static readonly Error SemItens = Error.ConflitoDeEstado(
        "Pedido.SemItens",
        "Pedido sem itens nao pode ser confirmado.");

    public static readonly Error ItensPendentes = Error.ConflitoDeEstado(
        "Pedido.ItensPendentes",
        "Pedido so fica pronto quando todos os itens estao prontos.");

    public static readonly Error ItemNaoEncontrado = Error.NaoEncontrado(
        "Pedido.ItemNaoEncontrado",
        "Item nao pertence a este pedido.");

    public static readonly Error ItemJaCancelado = Error.ConflitoDeEstado(
        "Pedido.ItemJaCancelado",
        "Item ja esta cancelado.");

    public static readonly Error MotivoDeCancelamentoObrigatorio = Error.Validacao(
        "Pedido.MotivoDeCancelamentoObrigatorio",
        "Item que ja entrou em producao exige motivo de cancelamento.");

    public static readonly Error ItemNaoPodeSerAlterado = Error.ConflitoDeEstado(
        "Pedido.ItemNaoPodeSerAlterado",
        "Somente item pendente pode ter a quantidade alterada.");

    public static readonly Error TodosOsItensCancelados = Error.ConflitoDeEstado(
        "Pedido.TodosOsItensCancelados",
        "Pedido com todos os itens cancelados nao pode avancar.");

    public static Error NaoAceitaItens(StatusPedido status) => Error.ConflitoDeEstado(
        "Pedido.NaoAceitaItens",
        $"Pedido em status {status} nao aceita novos itens.");

    public static Error TransicaoInvalida(StatusPedido origem, StatusPedido destino) => Error.ConflitoDeEstado(
        "Pedido.TransicaoInvalida",
        $"Nao e possivel mudar o pedido de {origem} para {destino}.");

    public static Error TransicaoDeItemInvalida(StatusItemPedido origem, StatusItemPedido destino) =>
        Error.ConflitoDeEstado(
            "Pedido.TransicaoDeItemInvalida",
            $"Nao e possivel mudar o item de {origem} para {destino}.");
}
