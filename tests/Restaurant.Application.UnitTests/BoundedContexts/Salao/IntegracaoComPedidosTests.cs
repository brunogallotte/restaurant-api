using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Salao.EventHandlers;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.BoundedContexts.Salao;

public sealed class IntegracaoComPedidosTests
{
    private static readonly DateTimeOffset Abertura = PedidoBuilder.AberturaPadrao;
    private static readonly PedidoId Pedido = PedidoId.Novo();

    private readonly IMesaRepository _mesas = Substitute.For<IMesaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MesaId _mesaId = MesaId.Novo();

    [Fact]
    public async Task Pedido_aberto_ocupa_a_mesa_e_commita()
    {
        var mesa = MesaExistente(MesaBuilder.Uma().Construir());

        await Ocupar();

        mesa.Status.Should().Be(StatusMesa.Ocupada);
        mesa.OcupadaEm.Should().Be(Abertura);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pedido_fechado_libera_a_mesa_e_commita()
    {
        var mesa = MesaExistente(MesaBuilder.Uma().ConstruirOcupada(Abertura));

        await Liberar(fechado: true);

        mesa.Status.Should().Be(StatusMesa.Livre);
        mesa.OcupadaEm.Should().BeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pedido_cancelado_tambem_libera_a_mesa()
    {
        var mesa = MesaExistente(MesaBuilder.Uma().ConstruirOcupada(Abertura));

        await Liberar(fechado: false);

        mesa.Status.Should().Be(StatusMesa.Livre);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mesa_ja_ocupada_e_no_op_sem_excecao_e_sem_commit()
    {
        MesaExistente(MesaBuilder.Uma().ConstruirOcupada(Abertura));

        var acao = Ocupar;

        await acao.Should().NotThrowAsync();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mesa_inexistente_e_no_op_sem_excecao_e_sem_commit()
    {
        _mesas.ObterPorIdAsync(Arg.Any<MesaId>(), Arg.Any<CancellationToken>()).Returns((Mesa?)null);

        var acao = Ocupar;

        await acao.Should().NotThrowAsync();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mesa_ja_livre_nao_e_liberada_de_novo()
    {
        MesaExistente(MesaBuilder.Uma().Construir());

        await Liberar(fechado: true);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Mesa MesaExistente(Mesa mesa)
    {
        _mesas.ObterPorIdAsync(_mesaId, Arg.Any<CancellationToken>()).Returns(mesa);

        return mesa;
    }

    private Task Ocupar() =>
        new OcuparMesaQuandoPedidoAberto(_mesas, _unitOfWork, new LoggerEspiao<OcuparMesaQuandoPedidoAberto>())
            .Handle(
                new PedidoAbertoDomainEvent(
                    Pedido,
                    EstabelecimentoId.Novo(),
                    _mesaId,
                    FuncionarioId.Novo(),
                    "20260725-0001",
                    Abertura),
                TestContext.Current.CancellationToken);

    private Task Liberar(bool fechado)
    {
        var quando = Abertura.AddMinutes(40);

        if (fechado)
        {
            return new LiberarMesaQuandoPedidoFechado(
                _mesas,
                _unitOfWork,
                new LoggerEspiao<LiberarMesaQuandoPedidoFechado>()).Handle(
                new PedidoFechadoDomainEvent(
                    Pedido,
                    EstabelecimentoId.Novo(),
                    _mesaId,
                    Subtotal: 200m,
                    TaxaDeServico: 20m,
                    Total: 220m,
                    quando),
                TestContext.Current.CancellationToken);
        }

        return new LiberarMesaQuandoPedidoCancelado(
            _mesas,
            _unitOfWork,
            new LoggerEspiao<LiberarMesaQuandoPedidoCancelado>()).Handle(
            new PedidoCanceladoDomainEvent(Pedido, _mesaId, "Cliente foi embora sem esperar", quando),
            TestContext.Current.CancellationToken);
    }
}
