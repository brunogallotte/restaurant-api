using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.UnitTests.Builders;

internal sealed class PedidoBuilder
{
    private static readonly DateTimeOffset AberturaPadrao = new(2026, 7, 25, 19, 0, 0, TimeSpan.Zero);

    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private MesaId _mesaId = MesaId.Novo();
    private FuncionarioId _abertoPor = FuncionarioId.Novo();
    private NumeroPedido _numero = NumeroPedido.Criar(new DateOnly(2026, 7, 25), 1).Value;
    private NomeCliente? _nomeCliente;
    private Observacao? _observacao;
    private Percentual _taxaDeServico = Percentual.Zero();
    private DateTimeOffset _abertoEm = AberturaPadrao;
    private readonly List<(ProdutoDoPedido Produto, Quantidade Quantidade)> _itens = [];

    public static PedidoBuilder Um() => new();

    public PedidoBuilder NaMesa(MesaId mesaId)
    {
        _mesaId = mesaId;
        return this;
    }

    public PedidoBuilder DoEstabelecimento(EstabelecimentoId estabelecimentoId)
    {
        _estabelecimentoId = estabelecimentoId;
        return this;
    }

    public PedidoBuilder AbertoPor(FuncionarioId funcionarioId)
    {
        _abertoPor = funcionarioId;
        return this;
    }

    public PedidoBuilder ComNumero(int sequencial)
    {
        _numero = NumeroPedido.Criar(new DateOnly(2026, 7, 25), sequencial).Value;
        return this;
    }

    public PedidoBuilder DoCliente(string nome)
    {
        _nomeCliente = NomeCliente.Criar(nome).Value;
        return this;
    }

    public PedidoBuilder ComObservacao(string observacao)
    {
        _observacao = Observacao.Criar(observacao).Value;
        return this;
    }

    public PedidoBuilder ComTaxaDeServico(decimal percentual)
    {
        _taxaDeServico = Percentual.Criar(percentual).Value;
        return this;
    }

    public PedidoBuilder AbertoEm(DateTimeOffset abertoEm)
    {
        _abertoEm = abertoEm;
        return this;
    }

    public PedidoBuilder ComItem(decimal precoUnitario = 25m, int quantidade = 1, string nome = "Picanha")
    {
        var produto = ProdutoDoPedidoBuilder.Um().Chamado(nome).ComPreco(precoUnitario).Construir();
        _itens.Add((produto, Quantidade.Criar(quantidade).Value));
        return this;
    }

    public Pedido Construir()
    {
        var pedido = Pedido.Abrir(
            _estabelecimentoId,
            _mesaId,
            _abertoPor,
            _numero,
            _nomeCliente,
            _observacao,
            _taxaDeServico,
            _abertoEm).Value;

        foreach (var (produto, quantidade) in _itens)
        {
            pedido.AdicionarItem(produto, quantidade, observacao: null, _abertoEm);
        }

        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirConfirmado()
    {
        var pedido = Construir();
        pedido.Confirmar(_abertoEm.AddMinutes(1));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirEmPreparo()
    {
        var pedido = ConstruirConfirmado();
        pedido.IniciarPreparo(_abertoEm.AddMinutes(2));
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

        pedido.MarcarComoPronto(_abertoEm.AddMinutes(10));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirEntregue()
    {
        var pedido = ConstruirPronto();
        pedido.Entregar(_abertoEm.AddMinutes(12));
        pedido.ClearDomainEvents();

        return pedido;
    }

    public Pedido ConstruirFechado()
    {
        var pedido = ConstruirEntregue();
        pedido.Fechar(_abertoEm.AddMinutes(40));
        pedido.ClearDomainEvents();

        return pedido;
    }
}
