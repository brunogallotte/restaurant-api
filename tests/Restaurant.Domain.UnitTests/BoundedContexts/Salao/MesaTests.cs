using AwesomeAssertions;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Events;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.BoundedContexts.Salao;

public sealed class MesaTests
{
    private static readonly DateTimeOffset Agora = MesaBuilder.Agora;

    [Fact]
    public void Cadastrar_nasce_livre()
    {
        var mesa = MesaBuilder.Uma().Construir();

        mesa.Status.Should().Be(StatusMesa.Livre);
        mesa.EstaLivre.Should().BeTrue();
        mesa.OcupadaEm.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Cadastrar_sem_lugares_falha(int lugares)
    {
        var resultado = Mesa.Cadastrar(
            EstabelecimentoId.Novo(),
            NumeroDaMesa.Criar("7").Value,
            lugares);

        resultado.Error.Should().Be(MesaErrors.LugaresInvalidos);
    }

    [Fact]
    public void Ocupar_mesa_livre_registra_o_instante()
    {
        var mesa = MesaBuilder.Uma().Construir();

        var resultado = mesa.Ocupar(Agora);

        resultado.Sucesso.Should().BeTrue();
        mesa.Status.Should().Be(StatusMesa.Ocupada);
        mesa.OcupadaEm.Should().Be(Agora);
        mesa.DomainEvents.Should().ContainItemsAssignableTo<MesaOcupadaDomainEvent>();
    }

    [Fact]
    public void Ocupar_mesa_ja_ocupada_falha()
    {
        var mesa = MesaBuilder.Uma().ConstruirOcupada();

        var resultado = mesa.Ocupar(Agora.AddMinutes(5));

        resultado.Error.Codigo.Should().Be("Mesa.TransicaoInvalida");
    }

    [Fact]
    public void Ocupar_mesa_reservada_e_permitido_e_limpa_a_reserva()
    {
        var mesa = MesaBuilder.Uma().ConstruirReservada();

        var resultado = mesa.Ocupar(Agora);

        resultado.Sucesso.Should().BeTrue();
        mesa.Status.Should().Be(StatusMesa.Ocupada);
        mesa.ReservadaAte.Should().BeNull();
    }

    [Fact]
    public void Liberar_mesa_livre_falha()
    {
        var mesa = MesaBuilder.Uma().Construir();

        mesa.Liberar(Agora).Error.Codigo.Should().Be("Mesa.TransicaoInvalida");
    }

    [Fact]
    public void Liberar_mesa_ocupada_zera_o_estado()
    {
        var mesa = MesaBuilder.Uma().ConstruirOcupada();

        var resultado = mesa.Liberar(Agora.AddHours(1));

        resultado.Sucesso.Should().BeTrue();
        mesa.Status.Should().Be(StatusMesa.Livre);
        mesa.OcupadaEm.Should().BeNull();
        mesa.DomainEvents.Should().ContainItemsAssignableTo<MesaLiberadaDomainEvent>();
    }

    [Fact]
    public void Reservar_com_data_no_passado_falha()
    {
        var mesa = MesaBuilder.Uma().Construir();

        var resultado = mesa.Reservar(Agora.AddMinutes(-1), Agora);

        resultado.Error.Should().Be(MesaErrors.ReservaNoPassado);
        mesa.Status.Should().Be(StatusMesa.Livre);
    }

    [Fact]
    public void Reservar_mesa_ocupada_falha()
    {
        var mesa = MesaBuilder.Uma().ConstruirOcupada();

        var resultado = mesa.Reservar(Agora.AddHours(3), Agora);

        resultado.Error.Codigo.Should().Be("Mesa.TransicaoInvalida");
    }

    [Fact]
    public void CancelarReserva_de_mesa_nao_reservada_falha()
    {
        var mesa = MesaBuilder.Uma().Construir();

        mesa.CancelarReserva(Agora).Error.Should().Be(MesaErrors.NaoEstaReservada);
    }

    [Fact]
    public void CancelarReserva_devolve_a_mesa_para_livre()
    {
        var mesa = MesaBuilder.Uma().ConstruirReservada();

        var resultado = mesa.CancelarReserva(Agora.AddMinutes(30));

        resultado.Sucesso.Should().BeTrue();
        mesa.Status.Should().Be(StatusMesa.Livre);
        mesa.ReservadaAte.Should().BeNull();
    }

    [Fact]
    public void AlterarLugares_para_zero_falha()
    {
        var mesa = MesaBuilder.Uma().Construir();

        mesa.AlterarLugares(0).Error.Should().Be(MesaErrors.LugaresInvalidos);
    }

    [Fact]
    public void NumeroDaMesa_normaliza_para_maiusculo() =>
        NumeroDaMesa.Criar(" varanda-a ").Value.Valor.Should().Be("VARANDA-A");

    [Fact]
    public void NumeroDaMesa_vazio_e_rejeitado() =>
        NumeroDaMesa.Criar("  ").Error.Should().Be(NumeroDaMesa.Vazio);

    [Fact]
    public void Mesa_nao_referencia_PedidoId_a_relacao_e_unidirecional()
    {
        var propriedades = typeof(Mesa).GetProperties().Select(propriedade => propriedade.PropertyType.Name);

        propriedades.Should().NotContain("PedidoId");
    }
}

public sealed class StatusMesaTransicaoTests
{
    [Theory]
    [InlineData("Livre", "Reservada")]
    [InlineData("Livre", "Ocupada")]
    [InlineData("Reservada", "Ocupada")]
    [InlineData("Reservada", "Livre")]
    [InlineData("Ocupada", "Livre")]
    public void Transicoes_permitidas(string origem, string destino) =>
        StatusMesa.DeNome(origem).PodeTransicionarPara(StatusMesa.DeNome(destino)).Should().BeTrue();

    [Theory]
    [InlineData("Livre", "Livre")]
    [InlineData("Ocupada", "Reservada")]
    [InlineData("Ocupada", "Ocupada")]
    [InlineData("Reservada", "Reservada")]
    public void Transicoes_proibidas(string origem, string destino) =>
        StatusMesa.DeNome(origem).PodeTransicionarPara(StatusMesa.DeNome(destino)).Should().BeFalse();

    [Theory]
    [InlineData("Livre", true)]
    [InlineData("Reservada", true)]
    [InlineData("Ocupada", false)]
    public void AceitaNovoPedido_reflete_a_ocupacao(string nome, bool esperado) =>
        StatusMesa.DeNome(nome).AceitaNovoPedido.Should().Be(esperado);

    [Fact]
    public void Todos_expoe_os_tres_status() => StatusMesa.Todos.Should().HaveCount(3);
}
