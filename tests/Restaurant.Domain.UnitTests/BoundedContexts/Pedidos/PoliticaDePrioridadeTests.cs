using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.Policies;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.BoundedContexts.Pedidos;

public sealed class PoliticaDePrioridadeTests
{
    private static readonly DateTimeOffset Abertura = new(2026, 7, 25, 19, 0, 0, TimeSpan.Zero);

    private readonly PoliticaDePrioridade _politica = PoliticaDePrioridade.Padrao;

    [Theory]
    [InlineData(0, "Normal")]
    [InlineData(19, "Normal")]
    [InlineData(20, "Alta")]
    [InlineData(34, "Alta")]
    [InlineData(35, "Urgente")]
    [InlineData(120, "Urgente")]
    public void Calcular_escala_a_prioridade_pelo_tempo_decorrido(int minutos, string esperado)
    {
        var prioridade = _politica.Calcular(
            PrioridadePedido.Normal,
            TimeSpan.FromMinutes(minutos),
            StatusPedido.EmPreparo);

        prioridade.Nome.Should().Be(esperado);
    }

    [Fact]
    public void Calcular_nunca_rebaixa_a_prioridade_manual()
    {
        var prioridade = _politica.Calcular(
            PrioridadePedido.Urgente,
            TimeSpan.FromMinutes(1),
            StatusPedido.Confirmado);

        prioridade.Should().Be(PrioridadePedido.Urgente);
    }

    [Theory]
    [InlineData("Fechado")]
    [InlineData("Cancelado")]
    public void Calcular_nao_escala_pedido_em_status_final(string nome)
    {
        var prioridade = _politica.Calcular(
            PrioridadePedido.Normal,
            TimeSpan.FromHours(3),
            StatusPedido.DeNome(nome));

        prioridade.Should().Be(PrioridadePedido.Normal);
    }

    [Fact]
    public void Decorrido_de_pedido_em_andamento_acompanha_o_relogio()
    {
        var decorrido = PoliticaDePrioridade.Decorrido(
            Abertura,
            fechadoEm: null,
            Abertura.AddMinutes(25));

        decorrido.Should().Be(TimeSpan.FromMinutes(25));
    }

    [Fact]
    public void Decorrido_de_pedido_fechado_congela_no_fechamento()
    {
        var decorrido = PoliticaDePrioridade.Decorrido(
            Abertura,
            Abertura.AddMinutes(40),
            Abertura.AddHours(3));

        decorrido.Should().Be(TimeSpan.FromMinutes(40));
    }

    [Fact]
    public void TempoDecorrido_do_agregado_concorda_com_a_politica()
    {
        var pedido = PedidoBuilder.Um().ComItem().AbertoEm(Abertura).ConstruirFechado();
        var agora = Abertura.AddHours(3);

        pedido.TempoDecorrido(agora).Should().Be(
            PoliticaDePrioridade.Decorrido(pedido.AbertoEm, pedido.FechadoEm, agora));
    }

    [Fact]
    public void Politica_com_limites_invertidos_e_rejeitada_na_construcao()
    {
        var acao = () => new PoliticaDePrioridade(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Prioridade_efetiva_usa_o_relogio_injetado_e_nao_o_do_sistema()
    {
        var relogio = new FakeTimeProvider(Abertura);
        var pedido = PedidoBuilder.Um().ComItem().AbertoEm(Abertura).ConstruirConfirmado();

        relogio.Advance(TimeSpan.FromMinutes(36));

        var prioridadeEfetiva = _politica.Calcular(
            pedido.Prioridade,
            pedido.TempoDecorrido(relogio.GetUtcNow()),
            pedido.Status);

        prioridadeEfetiva.Should().Be(PrioridadePedido.Urgente);
        pedido.Prioridade.Should().Be(PrioridadePedido.Normal);
    }

    [Fact]
    public void ElevarPrioridade_manual_levanta_evento()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirConfirmado();

        var resultado = pedido.ElevarPrioridade(PrioridadePedido.Alta);

        resultado.Sucesso.Should().BeTrue();
        pedido.Prioridade.Should().Be(PrioridadePedido.Alta);
        pedido.DomainEvents.Should().ContainItemsAssignableTo<
            PrioridadeDoPedidoElevadaDomainEvent>();
    }

    [Fact]
    public void ElevarPrioridade_para_valor_menor_e_ignorada_sem_evento()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirConfirmado();
        pedido.ElevarPrioridade(PrioridadePedido.Urgente);
        pedido.ClearDomainEvents();

        var resultado = pedido.ElevarPrioridade(PrioridadePedido.Normal);

        resultado.Sucesso.Should().BeTrue();
        pedido.Prioridade.Should().Be(PrioridadePedido.Urgente);
        pedido.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ElevarPrioridade_de_pedido_fechado_falha()
    {
        var pedido = PedidoBuilder.Um().ComItem().ConstruirFechado();

        var resultado = pedido.ElevarPrioridade(PrioridadePedido.Urgente);

        resultado.Falhou.Should().BeTrue();
    }
}
