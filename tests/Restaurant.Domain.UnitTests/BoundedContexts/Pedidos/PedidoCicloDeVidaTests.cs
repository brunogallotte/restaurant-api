using AwesomeAssertions;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.BoundedContexts.Pedidos;

public sealed class PedidoCicloDeVidaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 7, 25, 19, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Abrir_nasce_aberto_com_prioridade_normal_e_sem_itens()
    {
        var pedido = PedidoBuilder.Um().Construir();

        pedido.Status.Should().Be(StatusPedido.Aberto);
        pedido.Prioridade.Should().Be(PrioridadePedido.Normal);
        pedido.Itens.Should().BeEmpty();
        pedido.ConfirmadoEm.Should().BeNull();
    }

    [Fact]
    public void Abrir_levanta_PedidoAbertoDomainEvent()
    {
        var pedido = Pedido.Abrir(
            EstabelecimentoId.Novo(),
            MesaId.Novo(),
            FuncionarioId.Novo(),
            NumeroPedido.Criar(new DateOnly(2026, 7, 25), 7).Value,
            nomeCliente: null,
            observacao: null,
            Percentual.Zero(),
            Agora).Value;

        pedido.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PedidoAbertoDomainEvent>()
            .Which.NumeroPedido.Should().Be("20260725-0007");
    }

    [Fact]
    public void Confirmar_sem_itens_falha_com_SemItens()
    {
        var pedido = PedidoBuilder.Um().Construir();

        var resultado = pedido.Confirmar(Agora);

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Should().Be(PedidoErrors.SemItens);
        pedido.Status.Should().Be(StatusPedido.Aberto);
    }

    [Fact]
    public void Confirmar_com_item_avanca_status_e_registra_o_instante()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();

        var resultado = pedido.Confirmar(Agora);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Confirmado);
        pedido.ConfirmadoEm.Should().Be(Agora);
        pedido.DomainEvents.Should().ContainItemsAssignableTo<PedidoConfirmadoDomainEvent>();
    }

    [Fact]
    public void Confirmar_com_todos_os_itens_cancelados_falha_com_SemItens()
    {
        var pedido = PedidoBuilder.Um().ComItem().Construir();
        pedido.CancelarItem(pedido.Itens[0].Id, motivo: null);

        var resultado = pedido.Confirmar(Agora);

        resultado.Error.Should().Be(PedidoErrors.SemItens);
    }

    [Fact]
    public void MarcarComoPronto_com_item_ainda_em_preparo_falha_com_ItensPendentes()
    {
        var pedido = PedidoBuilder.Um().ComItem().ComItem(nome: "Farofa").ConstruirEmPreparo();
        pedido.MarcarItemComoPronto(pedido.Itens[0].Id);

        var resultado = pedido.MarcarComoPronto(Agora);

        resultado.Error.Should().Be(PedidoErrors.ItensPendentes);
        pedido.Status.Should().Be(StatusPedido.EmPreparo);
    }

    [Fact]
    public void MarcarComoPronto_com_todos_os_itens_prontos_avanca()
    {
        var pedido = PedidoBuilder.Um().ComItem().ComItem(nome: "Farofa").ConstruirEmPreparo();

        foreach (var item in pedido.ItensAtivos.ToList())
        {
            pedido.MarcarItemComoPronto(item.Id);
        }

        var resultado = pedido.MarcarComoPronto(Agora);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Pronto);
        pedido.ProntoEm.Should().Be(Agora);
    }

    [Fact]
    public void MarcarComoPronto_ignora_item_cancelado_na_verificacao()
    {
        var pedido = PedidoBuilder.Um().ComItem().ComItem(nome: "Farofa").ConstruirEmPreparo();
        var motivo = MotivoCancelamento.Criar("cliente desistiu").Value;
        pedido.CancelarItem(pedido.Itens[1].Id, motivo);
        pedido.MarcarItemComoPronto(pedido.Itens[0].Id);

        var resultado = pedido.MarcarComoPronto(Agora);

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public void Entregar_marca_todos_os_itens_ativos_como_entregues()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirPronto();

        var resultado = pedido.Entregar(Agora);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Entregue);
        pedido.ItensAtivos.Should().OnlyContain(item => item.Status == StatusItemPedido.Entregue);
    }

    [Fact]
    public void Fechar_exige_pedido_entregue()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirPronto();

        var resultado = pedido.Fechar(Agora);

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Codigo.Should().Be("Pedido.TransicaoInvalida");
    }

    [Fact]
    public void Fechar_congela_o_total_no_evento()
    {
        var pedido = PedidoBuilder.Um().ComItem(precoUnitario: 100m).ComTaxaDeServico(10m).ConstruirEntregue();

        pedido.Fechar(Agora);

        var evento = pedido.DomainEvents.OfType<PedidoFechadoDomainEvent>().Single();
        evento.Subtotal.Should().Be(100m);
        evento.TaxaDeServico.Should().Be(10m);
        evento.Total.Should().Be(110m);
    }

    [Theory]
    [InlineData("Entregue")]
    [InlineData("Fechado")]
    public void Cancelar_e_proibido_apos_entrega(string statusNome)
    {
        var pedido = statusNome == "Entregue"
            ? PedidoBuilder.Um().ComItem().ConstruirEntregue()
            : PedidoBuilder.Um().ComItem().ConstruirFechado();
        var motivo = MotivoCancelamento.Criar("erro de lancamento").Value;

        var resultado = pedido.Cancelar(motivo, Agora);

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Codigo.Should().Be("Pedido.TransicaoInvalida");
    }

    [Fact]
    public void Cancelar_pedido_cancela_todos_os_itens_ativos()
    {
        var pedido = PedidoBuilder.Um().ComItem().ComItem(nome: "Farofa").ConstruirEmPreparo();
        var motivo = MotivoCancelamento.Criar("mesa desistiu do pedido").Value;

        var resultado = pedido.Cancelar(motivo, Agora);

        resultado.Sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Cancelado);
        pedido.Itens.Should().OnlyContain(item => item.EstaCancelado);
        pedido.ItensAtivos.Should().BeEmpty();
    }

    [Fact]
    public void TempoDecorrido_congela_no_fechamento()
    {
        var abertura = Agora;
        var pedido = PedidoBuilder.Um().ComItem().AbertoEm(abertura).ConstruirFechado();

        var decorridoMuitoDepois = pedido.TempoDecorrido(abertura.AddHours(5));

        decorridoMuitoDepois.Should().Be(TimeSpan.FromMinutes(40));
    }

    [Fact]
    public void TempoDecorrido_de_pedido_em_andamento_acompanha_o_relogio()
    {
        var pedido = PedidoBuilder.Um().ComItem().AbertoEm(Agora).ConstruirConfirmado();

        pedido.TempoDecorrido(Agora.AddMinutes(17)).Should().Be(TimeSpan.FromMinutes(17));
    }
}
