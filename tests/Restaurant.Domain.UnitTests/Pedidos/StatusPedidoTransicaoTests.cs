using AwesomeAssertions;
using Restaurant.Domain.Pedidos;

namespace Restaurant.Domain.UnitTests.Pedidos;

public sealed class StatusPedidoTransicaoTests
{
    [Theory]
    [InlineData("Aberto", "Confirmado")]
    [InlineData("Aberto", "Cancelado")]
    [InlineData("Confirmado", "EmPreparo")]
    [InlineData("Confirmado", "Cancelado")]
    [InlineData("EmPreparo", "Pronto")]
    [InlineData("EmPreparo", "Cancelado")]
    [InlineData("Pronto", "Entregue")]
    [InlineData("Pronto", "EmPreparo")]
    [InlineData("Entregue", "Fechado")]
    public void Transicoes_permitidas(string origem, string destino) =>
        StatusPedido.DeNome(origem).PodeTransicionarPara(StatusPedido.DeNome(destino)).Should().BeTrue();

    [Theory]
    [InlineData("Aberto", "EmPreparo")]
    [InlineData("Aberto", "Pronto")]
    [InlineData("Aberto", "Entregue")]
    [InlineData("Aberto", "Fechado")]
    [InlineData("Confirmado", "Pronto")]
    [InlineData("Confirmado", "Aberto")]
    [InlineData("EmPreparo", "Confirmado")]
    [InlineData("EmPreparo", "Entregue")]
    [InlineData("Pronto", "Fechado")]
    [InlineData("Pronto", "Cancelado")]
    [InlineData("Entregue", "Cancelado")]
    [InlineData("Entregue", "EmPreparo")]
    [InlineData("Fechado", "Aberto")]
    [InlineData("Fechado", "Cancelado")]
    [InlineData("Cancelado", "Aberto")]
    [InlineData("Cancelado", "Confirmado")]
    public void Transicoes_proibidas(string origem, string destino) =>
        StatusPedido.DeNome(origem).PodeTransicionarPara(StatusPedido.DeNome(destino)).Should().BeFalse();

    [Theory]
    [InlineData("Fechado")]
    [InlineData("Cancelado")]
    public void Status_finais_nao_tem_saida(string nome)
    {
        var status = StatusPedido.DeNome(nome);

        status.EhFinal.Should().BeTrue();
        StatusPedido.Todos.Should().OnlyContain(destino => !status.PodeTransicionarPara(destino));
    }

    [Theory]
    [InlineData("Aberto", true)]
    [InlineData("Confirmado", true)]
    [InlineData("EmPreparo", true)]
    [InlineData("Pronto", true)]
    [InlineData("Entregue", false)]
    [InlineData("Fechado", false)]
    [InlineData("Cancelado", false)]
    public void AceitaNovosItens_reflete_o_ciclo_de_vida(string nome, bool esperado) =>
        StatusPedido.DeNome(nome).AceitaNovosItens.Should().Be(esperado);

    [Fact]
    public void Todos_expoe_os_sete_status() => StatusPedido.Todos.Should().HaveCount(7);

    [Fact]
    public void DeValor_desconhecido_lanca_DomainException()
    {
        var acao = () => StatusPedido.DeValor(99);

        acao.Should().Throw<Restaurant.Domain.Abstractions.DomainException>();
    }

    [Theory]
    [InlineData("Pendente", "EmPreparo")]
    [InlineData("Pendente", "Cancelado")]
    [InlineData("EmPreparo", "Pronto")]
    [InlineData("EmPreparo", "Cancelado")]
    [InlineData("Pronto", "Entregue")]
    [InlineData("Pronto", "Cancelado")]
    public void Transicoes_de_item_permitidas(string origem, string destino) =>
        StatusItemPedido.DeNome(origem).PodeTransicionarPara(StatusItemPedido.DeNome(destino)).Should().BeTrue();

    [Theory]
    [InlineData("Pendente", "Pronto")]
    [InlineData("Pendente", "Entregue")]
    [InlineData("EmPreparo", "Pendente")]
    [InlineData("EmPreparo", "Entregue")]
    [InlineData("Entregue", "Pronto")]
    [InlineData("Cancelado", "Pendente")]
    public void Transicoes_de_item_proibidas(string origem, string destino) =>
        StatusItemPedido.DeNome(origem).PodeTransicionarPara(StatusItemPedido.DeNome(destino)).Should().BeFalse();

    [Theory]
    [InlineData("Pendente", false)]
    [InlineData("EmPreparo", true)]
    [InlineData("Pronto", true)]
    [InlineData("Entregue", true)]
    [InlineData("Cancelado", false)]
    public void JaEntrouEmProducao_define_a_exigencia_de_motivo(string nome, bool esperado) =>
        StatusItemPedido.DeNome(nome).JaEntrouEmProducao.Should().Be(esperado);
}
