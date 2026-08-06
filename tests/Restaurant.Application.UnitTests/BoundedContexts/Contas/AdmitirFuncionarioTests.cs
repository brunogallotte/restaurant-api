using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Contas.AdmitirFuncionario;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.BoundedContexts.Contas;

public sealed class AdmitirFuncionarioTests
{
    private readonly IFuncionarioRepository _funcionarios = Substitute.For<IFuncionarioRepository>();
    private readonly IEstabelecimentoRepository _estabelecimentos = Substitute.For<IEstabelecimentoRepository>();
    private readonly TenantContextFake _tenant = new();
    private readonly FakeTimeProvider _relogio = new(EstabelecimentoBuilder.CadastroPadrao);

    [Fact]
    public async Task Admite_o_funcionario_no_estabelecimento_do_tenant()
    {
        EstabelecimentoExiste();
        Funcionario? adicionado = null;
        _funcionarios.Adicionar(Arg.Do<Funcionario>(argumento => adicionado = argumento));

        var resultado = await Executar(Comando());

        resultado.Sucesso.Should().BeTrue();
        adicionado!.EstabelecimentoId.Should().Be(_tenant.EstabelecimentoId);
        adicionado.Cargo.Should().Be(Cargo.Garcom);
        adicionado.AdmitidoEm.Should().Be(EstabelecimentoBuilder.CadastroPadrao);
    }

    [Fact]
    public async Task Estabelecimento_inexistente_recusa_a_admissao()
    {
        _estabelecimentos
            .ObterPorIdAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<CancellationToken>())
            .Returns((Estabelecimento?)null);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(EstabelecimentoErrors.NaoEncontrado);
        _funcionarios.DidNotReceive().Adicionar(Arg.Any<Funcionario>());
    }

    [Fact]
    public async Task Email_invalido_propaga_o_erro_do_value_object()
    {
        EstabelecimentoExiste();

        var resultado = await Executar(Comando() with { Email = "sem-arroba" });

        resultado.Error.Should().Be(Email.FormatoInvalido);
        _funcionarios.DidNotReceive().Adicionar(Arg.Any<Funcionario>());
    }

    [Fact]
    public async Task Nome_vazio_propaga_o_erro_do_value_object()
    {
        EstabelecimentoExiste();

        var resultado = await Executar(Comando() with { Nome = "   " });

        resultado.Error.Should().Be(NomePessoa.Vazio);
    }

    [Theory]
    [InlineData("Garcom", true)]
    [InlineData("garcom", true)]
    [InlineData("Cozinha", true)]
    [InlineData("Barista", false)]
    [InlineData("", false)]
    public void Validator_aceita_apenas_cargos_conhecidos(string cargo, bool esperado)
    {
        var validacao = new AdmitirFuncionarioCommandValidator()
            .Validate(Comando() with { Cargo = cargo });

        validacao.IsValid.Should().Be(esperado);
    }

    private void EstabelecimentoExiste() =>
        _estabelecimentos
            .ObterPorIdAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>())
            .Returns(EstabelecimentoBuilder.Um().Construir());

    private static AdmitirFuncionarioCommand Comando() =>
        new("Ana Beatriz Souza", "ana@cantina.com.br", "Garcom");

    private Task<Result<Guid>> Executar(AdmitirFuncionarioCommand comando) =>
        new AdmitirFuncionarioCommandHandler(_funcionarios, _estabelecimentos, _tenant, _relogio)
            .Handle(comando, TestContext.Current.CancellationToken);
}
