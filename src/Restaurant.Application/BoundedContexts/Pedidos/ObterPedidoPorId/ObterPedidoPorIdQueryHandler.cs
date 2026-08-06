using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.BoundedContexts.Pedidos.Contracts;
using Restaurant.Application.BoundedContexts.Pedidos.Ports;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Policies;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPedidoPorId;

internal sealed class ObterPedidoPorIdQueryHandler(
    IPedidoQueries pedidos,
    PoliticaDePrioridade politica,
    ITenantContext tenant,
    TimeProvider relogio) : IQueryHandler<ObterPedidoPorIdQuery, PedidoDetalhado>
{
    public async Task<Result<PedidoDetalhado>> Handle(
        ObterPedidoPorIdQuery query,
        CancellationToken cancellationToken)
    {
        var lido = await pedidos.ObterPorIdAsync(
            tenant.EstabelecimentoId,
            new PedidoId(query.PedidoId),
            cancellationToken);

        if (lido is null)
        {
            return Result.Failure<PedidoDetalhado>(PedidoErrors.NaoEncontrado);
        }

        return Projetar(lido, relogio.GetUtcNow());
    }

    private PedidoDetalhado Projetar(PedidoLido lido, DateTimeOffset agora)
    {
        var decorrido = PoliticaDePrioridade.Decorrido(lido.AbertoEm, lido.FechadoEm, agora);

        var prioridadeEfetiva = politica.Calcular(
            PrioridadePedido.DeNome(lido.PrioridadeManual),
            decorrido,
            StatusPedido.DeNome(lido.Status));

        var itens = lido.Itens.Select(Projetar).ToList();
        var subtotal = SomarAtivos(lido.Itens);
        var taxaDeServico = Percentual.Criar(lido.TaxaDeServico).Value;
        var valorDaTaxa = subtotal.AplicarPercentual(taxaDeServico);

        return new PedidoDetalhado(
            lido.PedidoId,
            lido.Numero,
            lido.MesaId,
            lido.NumeroDaMesa,
            lido.NomeCliente,
            lido.Observacao,
            lido.Status,
            prioridadeEfetiva.Nome,
            (int)decorrido.TotalMinutes,
            taxaDeServico.Valor,
            subtotal.Valor,
            valorDaTaxa.Valor,
            subtotal.Somar(valorDaTaxa).Value.Valor,
            lido.AbertoEm,
            lido.FechadoEm,
            itens);
    }

    private static ItemDoPedidoDetalhado Projetar(ItemLido item) =>
        new(
            item.ItemId,
            item.ProdutoId,
            item.Nome,
            item.Quantidade,
            item.PrecoUnitario,
            TotalDoItem(item).Valor,
            item.Status,
            item.Observacao,
            item.MotivoCancelamento);

    private static Dinheiro SomarAtivos(IReadOnlyList<ItemLido> itens) =>
        itens
            .Where(item => item.Status != StatusItemPedido.Cancelado.Nome)
            .Aggregate(
                Dinheiro.Zero(Moeda.Real),
                (acumulado, item) => acumulado.Somar(TotalDoItem(item)).Value);

    private static Dinheiro TotalDoItem(ItemLido item) =>
        Dinheiro.Criar(item.PrecoUnitario, Moeda.Real).Value.MultiplicarPor(item.Quantidade);
}
