using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Contas.AlterarTaxaDeServico;
using Restaurant.Application.UnitTests.Builders;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.BoundedContexts.Contas;

public sealed class AlterarTaxaDeServicoTests
{
    private readonly IEstabelecimentoRepository _estabelecimentos = Substitute.For<IEstabelecimentoRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Altera_a_taxa_do_estabelecimento_do_tenant()
    {
        var estabelecimento = EstabelecimentoExiste(EstabelecimentoBuilder.Um().ComTaxaDeServico(10m).Construir());

        var resultado = await Executar(new AlterarTaxaDeServicoCommand(12.5m));

        resultado.Sucesso.Should().BeTrue();
        estabelecimento.TaxaDeServico.Valor.Should().Be(12.5m);
        estabelecimento.DomainEvents.Should().ContainItemsAssignableTo<TaxaDeServicoAlteradaDomainEvent>();
    }

    [Fact]
    public async Task Estabelecimento_inexistente_devolve_nao_encontrado()
    {
        _estabelecimentos
            .ObterPorIdAsync(Arg.Any<EstabelecimentoId>(), Arg.Any<CancellationToken>())
            .Returns((Estabelecimento?)null);

        var resultado = await Executar(new AlterarTaxaDeServicoCommand(12.5m));

        resultado.Error.Should().Be(EstabelecimentoErrors.NaoEncontrado);
        resultado.Error.Tipo.Should().Be(ErrorType.NaoEncontrado);
    }

    [Fact]
    public async Task Taxa_fora_da_faixa_e_recusada_antes_de_tocar_no_agregado()
    {
        var estabelecimento = EstabelecimentoExiste(EstabelecimentoBuilder.Um().ComTaxaDeServico(10m).Construir());

        var resultado = await Executar(new AlterarTaxaDeServicoCommand(-1m));

        resultado.Error.Should().Be(Percentual.ForaDaFaixa);
        estabelecimento.TaxaDeServico.Valor.Should().Be(10m);
    }

    [Fact]
    public async Task Recusa_do_agregado_e_propagada_sem_reescrita()
    {
        EstabelecimentoExiste(EstabelecimentoBuilder.Um().ConstruirDesativado());

        var resultado = await Executar(new AlterarTaxaDeServicoCommand(12.5m));

        resultado.Error.Should().Be(EstabelecimentoErrors.Desativado);
    }

    private Estabelecimento EstabelecimentoExiste(Estabelecimento estabelecimento)
    {
        _estabelecimentos
            .ObterPorIdAsync(_tenant.EstabelecimentoId, Arg.Any<CancellationToken>())
            .Returns(estabelecimento);

        return estabelecimento;
    }

    private Task<Result> Executar(AlterarTaxaDeServicoCommand comando) =>
        new AlterarTaxaDeServicoCommandHandler(_estabelecimentos, _tenant)
            .Handle(comando, TestContext.Current.CancellationToken);
}
