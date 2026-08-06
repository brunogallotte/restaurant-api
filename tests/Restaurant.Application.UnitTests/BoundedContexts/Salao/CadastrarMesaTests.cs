using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Salao.CadastrarMesa;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.BoundedContexts.Salao;

public sealed class CadastrarMesaTests
{
    private readonly IMesaRepository _mesas = Substitute.For<IMesaRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Cadastra_a_mesa_livre_no_estabelecimento_do_tenant()
    {
        Mesa? adicionada = null;
        _mesas.Adicionar(Arg.Do<Mesa>(argumento => adicionada = argumento));

        var resultado = await Executar(new CadastrarMesaCommand("12", 4));

        resultado.Sucesso.Should().BeTrue();
        adicionada!.EstabelecimentoId.Should().Be(_tenant.EstabelecimentoId);
        adicionada.Numero.Valor.Should().Be("12");
        adicionada.Status.Should().Be(StatusMesa.Livre);
    }

    [Fact]
    public async Task Mesa_sem_lugares_e_recusada_pelo_agregado()
    {
        var resultado = await Executar(new CadastrarMesaCommand("12", 0));

        resultado.Error.Should().Be(MesaErrors.LugaresInvalidos);
        _mesas.DidNotReceive().Adicionar(Arg.Any<Mesa>());
    }

    [Fact]
    public async Task Numero_vazio_propaga_o_erro_do_value_object()
    {
        var resultado = await Executar(new CadastrarMesaCommand("   ", 4));

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Codigo.Should().StartWith("NumeroDaMesa.");
        _mesas.DidNotReceive().Adicionar(Arg.Any<Mesa>());
    }

    private Task<Result<Guid>> Executar(CadastrarMesaCommand comando) =>
        new CadastrarMesaCommandHandler(_mesas, _tenant)
            .Handle(comando, TestContext.Current.CancellationToken);
}
