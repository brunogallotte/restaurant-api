using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.Builders;

internal sealed class PedidoBuilder
{
    public static readonly DateTimeOffset AberturaPadrao = new(2026, 7, 25, 19, 0, 0, TimeSpan.Zero);

    private readonly List<(string Nome, decimal Preco, int Quantidade)> _itens = [];

    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private MesaId _mesaId = MesaId.Novo();
    private decimal _taxaDeServico;

    public static PedidoBuilder Um() => new();

    public PedidoBuilder DoEstabelecimento(EstabelecimentoId estabelecimentoId)
    {
        _estabelecimentoId = estabelecimentoId;
        return this;
    }

    public PedidoBuilder NaMesa(MesaId mesaId)
    {
        _mesaId = mesaId;
        return this;
    }

    public PedidoBuilder ComTaxaDeServico(decimal taxa)
    {
        _taxaDeServico = taxa;
        return this;
    }

    public PedidoBuilder ComItem(string nome = "Picanha", decimal preco = 89.90m, int quantidade = 1)
    {
        _itens.Add((nome, preco, quantidade));
        return this;
    }

    public Pedido Construir()
    {
        var pedido = Pedido.Abrir(
            _estabelecimentoId,
            _mesaId,
            FuncionarioId.Novo(),
            NumeroPedido.Criar(new DateOnly(2026, 7, 25), 1).Value,
            nomeCliente: null,
            observacao: null,
            Percentual.Criar(_taxaDeServico).Value,
            AberturaPadrao).Value;

        foreach (var (nome, preco, quantidade) in _itens)
        {
            pedido.AdicionarItem(
                ProdutoDoPedido.Criar(ProdutoId.Novo(), nome, Dinheiro.Criar(preco, Moeda.Real).Value).Value,
                Quantidade.Criar(quantidade).Value,
                observacao: null,
                AberturaPadrao);
        }

        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirConfirmado()
    {
        var pedido = Construir();
        pedido.Confirmar(AberturaPadrao.AddMinutes(1));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirEmPreparo()
    {
        var pedido = ConstruirConfirmado();
        pedido.IniciarPreparo(AberturaPadrao.AddMinutes(2));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirPronto()
    {
        var pedido = ConstruirEmPreparo();

        foreach (var item in pedido.ItensAtivos.ToList())
        {
            pedido.MarcarItemComoPronto(item.Id);
        }

        pedido.MarcarComoPronto(AberturaPadrao.AddMinutes(10));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirEntregue()
    {
        var pedido = ConstruirPronto();
        pedido.Entregar(AberturaPadrao.AddMinutes(12));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirFechado()
    {
        var pedido = ConstruirEntregue();
        pedido.Fechar(AberturaPadrao.AddMinutes(40));
        pedido.ClearDomainEvents();

        return pedido;
    }
}
