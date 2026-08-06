using AwesomeAssertions;
using Restaurant.Application.Abstractions.Results;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.Abstractions;

public sealed class FailedResultTests
{
    private static readonly Error Recusa = Error.Validacao("Teste.Recusa", "Recusado no pipeline.");

    [Fact]
    public void De_constroi_falha_de_Result_sem_valor()
    {
        var resultado = FailedResult.De<Result>(Recusa);

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Should().Be(Recusa);
    }

    [Fact]
    public void De_constroi_falha_de_Result_com_valor()
    {
        var resultado = FailedResult.De<Result<Guid>>(Recusa);

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Should().Be(Recusa);
    }

    [Fact]
    public void De_preserva_o_tipo_fechado_do_valor()
    {
        var resultado = FailedResult.De<Result<IReadOnlyList<string>>>(Recusa);

        resultado.Should().BeOfType<Result<IReadOnlyList<string>>>();
    }

    [Fact]
    public void Ler_o_valor_de_uma_falha_continua_proibido()
    {
        var resultado = FailedResult.De<Result<Guid>>(Recusa);

        var acao = () => resultado.Value;

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void De_reaproveita_a_fabrica_entre_chamadas_do_mesmo_tipo()
    {
        var primeiro = FailedResult.De<Result<int>>(Recusa);
        var segundo = FailedResult.De<Result<int>>(Recusa);

        primeiro.Error.Should().Be(segundo.Error);
        segundo.Falhou.Should().BeTrue();
    }
}
