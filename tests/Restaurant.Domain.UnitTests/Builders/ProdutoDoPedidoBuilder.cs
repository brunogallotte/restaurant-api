using Restaurant.Domain.Compartilhado;
using Restaurant.Domain.Pedidos.ValueObjects;

namespace Restaurant.Domain.UnitTests.Builders;

internal sealed class ProdutoDoPedidoBuilder
{
    private ProdutoId _produtoId = ProdutoId.Novo();
    private string _nome = "Picanha";
    private decimal _preco = 25m;
    private Moeda _moeda = Moeda.Real;

    public static ProdutoDoPedidoBuilder Um() => new();

    public ProdutoDoPedidoBuilder DoProduto(ProdutoId produtoId)
    {
        _produtoId = produtoId;
        return this;
    }

    public ProdutoDoPedidoBuilder Chamado(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoDoPedidoBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    public ProdutoDoPedidoBuilder NaMoeda(Moeda moeda)
    {
        _moeda = moeda;
        return this;
    }

    public ProdutoDoPedido Construir() =>
        ProdutoDoPedido.Criar(_produtoId, _nome, Dinheiro.Criar(_preco, _moeda).Value).Value;
}
