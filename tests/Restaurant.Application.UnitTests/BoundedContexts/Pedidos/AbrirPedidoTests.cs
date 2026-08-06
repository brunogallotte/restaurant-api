using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Pedidos.AbrirPedido;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.BoundedContexts.Pedidos;

public sealed class AbrirPedidoTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly IMesaRepository _mesas = Substitute.For<IMesaRepository>();
    private readonly IEstabelecimentoRepository _estabelecimentos = Substitute.For<IEstabelecimentoRepository>();
    private readonly IFuncionarioRepository _funcionarios = Substitute.For<IFuncionarioRepository>();
    private readonly IGeradorDeNumeroDePedido _geradorDeNumero = Substitute.For<IGeradorDeNumeroDePedido>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(PedidoBuilder.AberturaPadrao);
    private readonly MesaId _mesaId = MesaId.Novo();

    public AbrirPedidoTests()
    {
        EstabelecimentoDoTenant(taxaDeServico: 10m);
        FuncionarioDoTenant(ativo: true);
        MesaDoTenant();
        NumeroGerado(1);
    }

    [Fact]
    public async Task Abre_o_pedido_com_a_taxa_do_estabelecimento_e_o_relogio_injetado()
    {
        var pedido = await CapturarPedidoAdicionado(Comando());

        pedido.EstabelecimentoId.Should().Be(_tenant.EstabelecimentoId);
        pedido.AbertoPor.Should().Be(_tenant.FuncionarioId);
        pedido.MesaId.Should().Be(_mesaId);
        pedido.TaxaDeServico.Valor.Should().Be(10m);
        pedido.AbertoEm.Should().Be(PedidoBuilder.AberturaPadrao);
    }

    [Fact]
    public async Task Mesa_com_pedido_aberto_e_recusada()
    {
        _pedidos.ExisteAbertoParaMesaAsync(_mesaId, Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(PedidoErrors.MesaJaTemPedidoAberto);
        _pedidos.DidNotReceive().Adicionar(Arg.Any<Pedido>());
    }

    [Fact]
    public async Task Mesa_de_outro_tenant_devolve_nao_encontrada()
    {
        _mesas
            .ObterPorIdAsync(_mesaId, Arg.Any<CancellationToken>())
            .Returns(MesaBuilder.Uma().DoEstabelecimento(EstabelecimentoId.Novo()).Construir());

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(MesaErrors.NaoEncontrada);
    }

    [Fact]
    public async Task Funcionario_desligado_nao_abre_pedido()
    {
        FuncionarioDoTenant(ativo: false);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(FuncionarioErrors.NaoEncontrado);
        _pedidos.DidNotReceive().Adicionar(Arg.Any<Pedido>());
    }

    [Fact]
    public async Task Estabelecimento_inexistente_devolve_nao_encontrado()
    {
        _estabelecimentos
            .ObterPorIdAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<CancellationToken>())
            .Returns((Estabelecimento?)null);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(EstabelecimentoErrors.NaoEncontrado);
    }

    [Fact]
    public async Task Nome_de_cliente_longo_demais_e_recusado()
    {
        var resultado = await Executar(Comando() with { NomeCliente = new string('a', 200) });

        resultado.Error.Should().Be(NomeCliente.MuitoLongo);
    }

    [Fact]
    public async Task Numero_so_e_gerado_depois_das_guardas()
    {
        _pedidos.ExisteAbertoParaMesaAsync(_mesaId, Arg.Any<CancellationToken>()).Returns(true);

        await Executar(Comando());

        await _geradorDeNumero
            .DidNotReceive()
            .GerarAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Numero_do_pedido_usa_o_dia_do_relogio_injetado()
    {
        await Executar(Comando());

        await _geradorDeNumero.Received(1).GerarAsync(
            _tenant.EstabelecimentoId,
            new DateOnly(2026, 7, 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pedido_aberto_levanta_o_evento_que_vai_ocupar_a_mesa()
    {
        var pedido = await CapturarPedidoAdicionado(Comando());

        pedido.DomainEvents.Should().ContainItemsAssignableTo<
            Domain.BoundedContexts.Pedidos.PedidoAggregate.Events.PedidoAbertoDomainEvent>();
    }

    private void EstabelecimentoDoTenant(decimal taxaDeServico) =>
        _estabelecimentos
            .ObterPorIdAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>())
            .Returns(EstabelecimentoBuilder.Um().ComTaxaDeServico(taxaDeServico).Construir());

    private void FuncionarioDoTenant(bool ativo)
    {
        var funcionario = Funcionario.Admitir(
            _tenant.EstabelecimentoId,
            NomePessoa.Criar("Ana Souza").Value,
            Email.Criar("ana@cantina.com.br").Value,
            Cargo.Garcom,
            PedidoBuilder.AberturaPadrao.AddYears(-1)).Value;

        if (!ativo)
        {
            funcionario.Desligar(PedidoBuilder.AberturaPadrao.AddDays(-1));
        }

        _funcionarios
            .ObterPorIdAsync(_tenant.FuncionarioId, Arg.Any<CancellationToken>())
            .Returns(funcionario);
    }

    private void MesaDoTenant() =>
        _mesas
            .ObterPorIdAsync(_mesaId, Arg.Any<CancellationToken>())
            .Returns(MesaBuilder.Uma().DoEstabelecimento(_tenant.EstabelecimentoId).Construir());

    private void NumeroGerado(int sequencial) =>
        _geradorDeNumero
            .GerarAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(NumeroPedido.Criar(new DateOnly(2026, 7, 25), sequencial));

    private AbrirPedidoCommand Comando() => new(_mesaId.Valor, "Bruno", null);

    private async Task<Pedido> CapturarPedidoAdicionado(AbrirPedidoCommand comando)
    {
        Pedido? adicionado = null;
        _pedidos.Adicionar(Arg.Do<Pedido>(argumento => adicionado = argumento));

        var resultado = await Executar(comando);

        resultado.Sucesso.Should().BeTrue();

        return adicionado!;
    }

    private Task<Result<Guid>> Executar(AbrirPedidoCommand comando) =>
        new AbrirPedidoCommandHandler(
            _pedidos,
            _mesas,
            _estabelecimentos,
            _funcionarios,
            _geradorDeNumero,
            _tenant,
            _relogio).Handle(comando, TestContext.Current.CancellationToken);
}
