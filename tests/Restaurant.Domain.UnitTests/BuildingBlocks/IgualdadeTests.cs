using AwesomeAssertions;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.BuildingBlocks;

public sealed class IgualdadeTests
{
    [Fact]
    public void Entity_com_mesmo_id_e_igual_mesmo_com_estado_diferente()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();
        var item = pedido.Itens[0];
        var mesmoItem = pedido.Itens[0];

        item.Should().Be(mesmoItem);
        item.GetHashCode().Should().Be(mesmoItem.GetHashCode());
    }

    [Fact]
    public void Entity_com_ids_diferentes_nao_e_igual()
    {
        var pedido = PedidoBuilder.Um().ComItem().ComItem(nome: "Farofa").Construir();

        pedido.Itens[0].Should().NotBe(pedido.Itens[1]);
    }

    [Fact]
    public void Dois_pedidos_com_os_mesmos_dados_sao_entidades_distintas()
    {
        var mesa = MesaId.Novo();
        var primeiro = PedidoBuilder.Um().NaMesa(mesa).ComNumero(1).Construir();
        var segundo = PedidoBuilder.Um().NaMesa(mesa).ComNumero(1).Construir();

        primeiro.Should().NotBe(segundo);
        primeiro.Id.Should().NotBe(segundo.Id);
    }

    [Fact]
    public void ValueObject_com_os_mesmos_componentes_e_igual()
    {
        var produtoId = ProdutoId.Novo();
        var preco = Dinheiro.CriarEmReal(30m).Value;
        var primeiro = ProdutoDoPedidoBuilder.Um().DoProduto(produtoId).ComPreco(30m).Construir();
        var segundo = ProdutoDoPedidoBuilder.Um().DoProduto(produtoId).ComPreco(30m).Construir();

        primeiro.Should().Be(segundo);
        primeiro.PrecoUnitario.Should().Be(preco);
    }

    [Fact]
    public void ValueObject_com_um_componente_diferente_nao_e_igual()
    {
        var produtoId = ProdutoId.Novo();
        var trintaReais = ProdutoDoPedidoBuilder.Um().DoProduto(produtoId).ComPreco(30m).Construir();
        var quarentaReais = ProdutoDoPedidoBuilder.Um().DoProduto(produtoId).ComPreco(40m).Construir();

        trintaReais.Should().NotBe(quarentaReais);
    }

    [Fact]
    public void Id_fortemente_tipado_impede_confundir_agregados()
    {
        var pedidoId = PedidoId.Novo();
        var itemId = new ItemPedidoId(pedidoId.Valor);

        pedidoId.Valor.Should().Be(itemId.Valor);
        pedidoId.GetType().Should().NotBe(itemId.GetType());
    }

    [Fact]
    public void Ids_sao_guid_versao_7()
    {
        PedidoId.Novo().Valor.Version.Should().Be(7);
        ItemPedidoId.Novo().Valor.Version.Should().Be(7);
        MesaId.Novo().Valor.Version.Should().Be(7);
    }

    [Fact]
    public void Prefixo_de_timestamp_do_guid_v7_nunca_retrocede()
    {
        var primeiro = PedidoId.Novo();
        var segundo = PedidoId.Novo();

        TimestampBigEndian(segundo.Valor).Should().BeGreaterThanOrEqualTo(TimestampBigEndian(primeiro.Valor));
    }

    private static long TimestampBigEndian(Guid id)
    {
        var bytes = id.ToByteArray(bigEndian: true);

        return ((long)bytes[0] << 40)
            | ((long)bytes[1] << 32)
            | ((long)bytes[2] << 24)
            | ((long)bytes[3] << 16)
            | ((long)bytes[4] << 8)
            | bytes[5];
    }

    [Fact]
    public void SmartEnum_e_igual_por_valor_e_singleton_por_instancia()
    {
        StatusPedido.DeNome("Aberto").Should().BeSameAs(StatusPedido.Aberto);
        StatusPedido.DeValor(1).Should().Be(StatusPedido.Aberto);
        StatusPedido.Aberto.Should().NotBe(StatusPedido.Confirmado);
    }
}
