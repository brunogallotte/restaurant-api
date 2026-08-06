using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Contas.CadastrarEstabelecimento;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.BoundedContexts.Contas;

public sealed class CadastrarEstabelecimentoTests
{
    private readonly IEstabelecimentoRepository _estabelecimentos = Substitute.For<IEstabelecimentoRepository>();
    private readonly IVerificadorDeCnpjUnico _verificadorDeCnpj = Substitute.For<IVerificadorDeCnpjUnico>();
    private readonly FakeTimeProvider _relogio = new(EstabelecimentoBuilder.CadastroPadrao);

    [Fact]
    public async Task Cadastra_o_estabelecimento_e_devolve_o_identificador()
    {
        var resultado = await Executar(Comando());

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.Should().NotBe(Guid.Empty);
        _estabelecimentos.Received(1).Adicionar(Arg.Any<Estabelecimento>());
    }

    [Fact]
    public async Task Estabelecimento_cadastrado_usa_o_relogio_injetado()
    {
        Estabelecimento? adicionado = null;
        _estabelecimentos.Adicionar(Arg.Do<Estabelecimento>(argumento => adicionado = argumento));

        await Executar(Comando());

        adicionado!.CadastradoEm.Should().Be(EstabelecimentoBuilder.CadastroPadrao);
    }

    [Fact]
    public async Task Cnpj_ja_cadastrado_e_recusado_sem_adicionar()
    {
        _verificadorDeCnpj
            .JaCadastradoAsync(Arg.Any<Cnpj>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var resultado = await Executar(Comando());

        resultado.Error.Should().Be(EstabelecimentoErrors.CnpjJaCadastrado);
        _estabelecimentos.DidNotReceive().Adicionar(Arg.Any<Estabelecimento>());
    }

    [Fact]
    public async Task Cnpj_invalido_propaga_o_erro_do_value_object()
    {
        var resultado = await Executar(Comando() with { Cnpj = "11111111111111" });

        resultado.Error.Should().Be(Cnpj.DigitoVerificadorInvalido);
        _estabelecimentos.DidNotReceive().Adicionar(Arg.Any<Estabelecimento>());
    }

    [Fact]
    public async Task Cep_invalido_impede_a_construcao_do_endereco()
    {
        var resultado = await Executar(Comando() with { Cep = "123" });

        resultado.Error.Should().Be(Cep.FormatoInvalido);
    }

    [Fact]
    public async Task Taxa_de_servico_fora_da_faixa_e_recusada()
    {
        var resultado = await Executar(Comando() with { TaxaDeServico = 150m });

        resultado.Error.Should().Be(Percentual.ForaDaFaixa);
    }

    [Fact]
    public async Task Unicidade_do_cnpj_so_e_consultada_com_as_entradas_validas()
    {
        await Executar(Comando() with { Cnpj = "11111111111111" });

        await _verificadorDeCnpj
            .DidNotReceive()
            .JaCadastradoAsync(Arg.Any<Cnpj>(), Arg.Any<CancellationToken>());
    }

    private static CadastrarEstabelecimentoCommand Comando() =>
        new(
            "Cantina da Esquina",
            EstabelecimentoBuilder.CnpjValido,
            "contato@cantina.com.br",
            "11987654321",
            "Rua das Flores",
            "123",
            null,
            "Centro",
            "Sao Paulo",
            "SP",
            "01001000",
            10m);

    private Task<Result<Guid>> Executar(CadastrarEstabelecimentoCommand comando) =>
        new CadastrarEstabelecimentoCommandHandler(_estabelecimentos, _verificadorDeCnpj, _relogio)
            .Handle(comando, TestContext.Current.CancellationToken);
}
