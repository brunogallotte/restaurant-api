using AwesomeAssertions;
using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.UnitTests.Abstractions;

public sealed class ResultTests
{
    private static readonly Error ErroQualquer = Error.Validacao("Teste.Erro", "Falhou.");

    [Fact]
    public void Success_nao_carrega_erro()
    {
        var resultado = Result.Success();

        resultado.Sucesso.Should().BeTrue();
        resultado.Falhou.Should().BeFalse();
        resultado.Error.Should().Be(Error.Nenhum);
    }

    [Fact]
    public void Failure_carrega_o_erro()
    {
        var resultado = Result.Failure(ErroQualquer);

        resultado.Falhou.Should().BeTrue();
        resultado.Error.Should().Be(ErroQualquer);
    }

    [Fact]
    public void Ler_Value_de_Result_que_falhou_lanca_DomainException()
    {
        var resultado = Result.Failure<int>(ErroQualquer);

        var acao = () => resultado.Value;

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Conversao_implicita_produz_sucesso()
    {
        Result<int> resultado = 42;

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.Should().Be(42);
    }

    [Fact]
    public void Map_projeta_apenas_quando_ha_sucesso()
    {
        Result.Success(10).Map(valor => valor * 2).Value.Should().Be(20);
        Result.Failure<int>(ErroQualquer).Map(valor => valor * 2).Error.Should().Be(ErroQualquer);
    }

    [Fact]
    public void PrimeiraFalha_retorna_o_primeiro_erro_encontrado()
    {
        var outroErro = Error.Validacao("Teste.Outro", "Outro.");

        var resultado = Result.PrimeiraFalha(
            Result.Success(),
            Result.Failure(ErroQualquer),
            Result.Failure(outroErro));

        resultado.Error.Should().Be(ErroQualquer);
    }

    [Fact]
    public void PrimeiraFalha_sem_falhas_retorna_sucesso() =>
        Result.PrimeiraFalha(Result.Success(), Result.Success()).Sucesso.Should().BeTrue();

    [Theory]
    [InlineData(ErrorType.Validacao)]
    [InlineData(ErrorType.ConflitoDeEstado)]
    [InlineData(ErrorType.NaoEncontrado)]
    public void Error_preserva_o_tipo_para_o_mapeamento_http(ErrorType tipo)
    {
        var error = new Error("Teste.Codigo", "Mensagem.", tipo);

        error.Tipo.Should().Be(tipo);
        error.ToString().Should().Be("Teste.Codigo: Mensagem.");
    }
}
