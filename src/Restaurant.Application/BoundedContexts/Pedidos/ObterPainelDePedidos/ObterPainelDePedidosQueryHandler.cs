using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.BoundedContexts.Pedidos.Contracts;
using Restaurant.Application.BoundedContexts.Pedidos.Ports;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.Policies;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Pedidos.ObterPainelDePedidos;

internal sealed class ObterPainelDePedidosQueryHandler(
    IPedidoQueries pedidos,
    PoliticaDePrioridade politica,
    ITenantContext tenant,
    TimeProvider relogio) : IQueryHandler<ObterPainelDePedidosQuery, IReadOnlyList<PedidoNoPainel>>
{
    public async Task<Result<IReadOnlyList<PedidoNoPainel>>> Handle(
        ObterPainelDePedidosQuery query,
        CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var emAndamento = await pedidos.ListarEmAndamentoAsync(tenant.EstabelecimentoId, cancellationToken);

        IReadOnlyList<PedidoNoPainel> painel =
        [
            .. emAndamento
                .Select(linha => Projetar(linha, agora))
                .OrderByDescending(pedido => PrioridadePedido.DeNome(pedido.PrioridadeEfetiva).Valor)
                .ThenBy(pedido => pedido.AbertoEm)
        ];

        return Result.Success(painel);
    }

    private PedidoNoPainel Projetar(PedidoEmAndamento linha, DateTimeOffset agora)
    {
        var decorrido = PoliticaDePrioridade.Decorrido(linha.AbertoEm, linha.FechadoEm, agora);

        var prioridadeEfetiva = politica.Calcular(
            PrioridadePedido.DeNome(linha.PrioridadeManual),
            decorrido,
            StatusPedido.DeNome(linha.Status));

        return new PedidoNoPainel(
            linha.PedidoId,
            linha.Numero,
            linha.NumeroDaMesa,
            linha.NomeCliente,
            linha.Status,
            prioridadeEfetiva.Nome,
            (int)decorrido.TotalMinutes,
            linha.QuantidadeDeItensAtivos,
            linha.AbertoEm);
    }
}
