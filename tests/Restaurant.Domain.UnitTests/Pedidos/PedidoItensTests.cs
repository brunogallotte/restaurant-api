using AwesomeAssertions;
using Restaurant.Domain.Compartilhado;
using Restaurant.Domain.Pedidos;
using Restaurant.Domain.Pedidos.Events;
using Restaurant.Domain.Pedidos.ValueObjects;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.Pedidos;

public sealed class PedidoItensTests
{
    private static readonly DateTimeOffset Agora = new(2026, 7, 25, 19, 30, 0, TimeSpan.Zero);

    [Fact]
    public void AdicionarItem_a_pedido_fechado_falha_com_NaoAceitaItens()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirFechado();
        var produto = ProdutoDoPedidoBuilder.Um().Construir();

        var resultado = pedido.AdicionarItem(produto, Quantidade.Uma(), observacao: null, Agora);

        resultado.Error.Codigo.Should().Be("Pedido.NaoAceitaItens");
    }

    [Fact]
    public void AdicionarItem_a_pedido_cancelado_falha_com_NaoAceitaItens()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirEmPreparo();
        pedido.Cancelar(MotivoCancelamento.Criar("mesa desistiu").Value, Agora);
        var produto = ProdutoDoPedidoBuilder.Um().Construir();

        var resultado = pedido.AdicionarItem(produto, Quantidade.Uma(), observacao: null, Agora);

        resultado.Error.Codigo.Should().Be("Pedido.NaoAceitaItens");
    }

    [Fact]
    public void AdicionarItem_em_pedido_pronto_retorna_status_para_em_preparo()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirPronto();
        var produto = ProdutoDoPedidoBuilder.Um().Chamado("Pudim").Construir();

        var resultado = pedido.AdicionarItem(produto, Quantidade.Uma(), observacao: null, Agora);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.EmPreparo);
        pedido.ProntoEm.Should().BeNull();
        pedido.DomainEvents.OfType<StatusDoPedidoAlteradoDomainEvent>().Should().ContainSingle()
            .Which.StatusNovo.Should().Be(StatusPedido.EmPreparo);
    }

    [Fact]
    public void AdicionarItem_com_moeda_diferente_da_do_pedido_falha()
    {
        var pedido = PedidoBuilder.Um().Construir();
        var produtoEmDolar = ProdutoDoPedidoBuilder.Um().NaMoeda(Moeda.Dolar).Construir();

        var resultado = pedido.AdicionarItem(produtoEmDolar, Quantidade.Uma(), observacao: null, Agora);

        resultado.Error.Should().Be(CompartilhadoErrors.DinheiroMoedasDiferentes);
    }

    [Fact]
    public void AdicionarItem_levanta_evento_com_o_snapshot_do_produto()
    {
        var pedido = PedidoBuilder.Um().Construir();
        var produto = ProdutoDoPedidoBuilder.Um().Chamado("Moqueca").ComPreco(89.90m).Construir();

        pedido.AdicionarItem(produto, Quantidade.Criar(2).Value, observacao: null, Agora);

        var evento = pedido.DomainEvents.OfType<ItemAdicionadoAoPedidoDomainEvent>().Single();
        evento.NomeDoProduto.Should().Be("Moqueca");
        evento.PrecoUnitario.Should().Be(89.90m);
        evento.Quantidade.Should().Be(2);
    }

    [Fact]
    public void AlterarQuantidadeDoItem_de_item_pendente_funciona()
    {
        var pedido = PedidoBuilder.Um().ComItem(quantidade: 1).Construir();

        var resultado = pedido.AlterarQuantidadeDoItem(pedido.Itens[0].Id, Quantidade.Criar(4).Value);

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].Quantidade.Valor.Should().Be(4);
        pedido.DomainEvents.OfType<QuantidadeDoItemAlteradaDomainEvent>().Single()
            .QuantidadeAnterior.Should().Be(1);
    }

    [Fact]
    public void AlterarQuantidadeDoItem_ja_em_preparo_falha()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirEmPreparo();

        var resultado = pedido.AlterarQuantidadeDoItem(pedido.Itens[0].Id, Quantidade.Criar(3).Value);

        resultado.Error.Should().Be(PedidoErrors.ItemNaoPodeSerAlterado);
    }

    [Fact]
    public void AlterarQuantidadeDoItem_inexistente_falha_com_ItemNaoEncontrado()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();

        var resultado = pedido.AlterarQuantidadeDoItem(ItemPedidoId.Novo(), Quantidade.Criar(2).Value);

        resultado.Error.Should().Be(PedidoErrors.ItemNaoEncontrado);
    }

    [Fact]
    public void CancelarItem_pendente_dispensa_motivo()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();

        var resultado = pedido.CancelarItem(pedido.Itens[0].Id, motivo: null);

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].EstaCancelado.Should().BeTrue();
        pedido.DomainEvents.OfType<ItemDoPedidoCanceladoDomainEvent>().Single()
            .JaEstavaEmProducao.Should().BeFalse();
    }

    [Fact]
    public void CancelarItem_em_producao_sem_motivo_falha()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirEmPreparo();

        var resultado = pedido.CancelarItem(pedido.Itens[0].Id, motivo: null);

        resultado.Error.Should().Be(PedidoErrors.MotivoDeCancelamentoObrigatorio);
        pedido.Itens[0].EstaCancelado.Should().BeFalse();
    }

    [Fact]
    public void CancelarItem_em_producao_com_motivo_registra_a_perda()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirEmPreparo();
        var motivo = MotivoCancelamento.Criar("prato queimou na cozinha").Value;

        var resultado = pedido.CancelarItem(pedido.Itens[0].Id, motivo);

        resultado.Sucesso.Should().BeTrue();
        pedido.Itens[0].MotivoCancelamento.Should().Be(motivo);
        pedido.DomainEvents.OfType<ItemDoPedidoCanceladoDomainEvent>().Single()
            .JaEstavaEmProducao.Should().BeTrue();
    }

    [Fact]
    public void CancelarItem_duas_vezes_falha_com_ItemJaCancelado()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();
        pedido.CancelarItem(pedido.Itens[0].Id, motivo: null);

        var resultado = pedido.CancelarItem(pedido.Itens[0].Id, motivo: null);

        resultado.Error.Should().Be(PedidoErrors.ItemJaCancelado);
    }

    [Fact]
    public void Subtotal_soma_apenas_itens_ativos()
    {
        var pedido = PedidoBuilder.Um()
            .ComItem(precoUnitario: 30m, quantidade: 2)
            .ComItem(precoUnitario: 12m, quantidade: 1, nome: "Farofa")
            .Construir();
        pedido.CancelarItem(pedido.Itens[1].Id, motivo: null);

        pedido.Subtotal.Valor.Should().Be(60m);
    }

    [Fact]
    public void Total_aplica_a_taxa_de_servico_sobre_o_subtotal()
    {
        var pedido = PedidoBuilder.Um()
            .ComItem(precoUnitario: 50m, quantidade: 2)
            .ComTaxaDeServico(10m)
            .Construir();

        pedido.Subtotal.Valor.Should().Be(100m);
        pedido.ValorDaTaxaDeServico.Valor.Should().Be(10m);
        pedido.Total.Valor.Should().Be(110m);
    }

    [Fact]
    public void Total_de_pedido_sem_itens_e_zero()
    {
        var pedido = PedidoBuilder.Um().ComTaxaDeServico(10m).Construir();

        pedido.Total.EstaZerado.Should().BeTrue();
    }

    [Fact]
    public void Itens_nao_expoe_a_colecao_interna_para_modificacao()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();

        pedido.Itens.Should().BeAssignableTo<IReadOnlyList<ItemPedido>>();
        pedido.Itens.Should().NotBeAssignableTo<List<ItemPedido>>();
    }
}
