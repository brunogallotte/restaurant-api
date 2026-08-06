using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Restaurant.Application.Abstractions.Behaviors;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.Abstractions;

public sealed class LoggingBehaviorTests
{
    private static readonly Error Recusa = Error.ConflitoDeEstado("Teste.Recusa", "Recusado pelo agregado.");

    private readonly LoggerEspiao<LoggingBehavior<ComandoDeTeste, Result>> _logger = new();
    private readonly FakeTimeProvider _relogio = new();

    [Fact]
    public async Task Sucesso_e_registrado_como_informacao()
    {
        var resultado = await Executar(() => Task.FromResult(Result.Success()));

        resultado.Sucesso.Should().BeTrue();
        _logger.Registros.Should().ContainSingle().Which.Nivel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task Recusa_e_registrada_como_aviso_com_o_codigo_do_erro()
    {
        var resultado = await Executar(() => Task.FromResult(Result.Failure(Recusa)));

        resultado.Falhou.Should().BeTrue();
        var registro = _logger.Registros.Should().ContainSingle().Subject;
        registro.Nivel.Should().Be(LogLevel.Warning);
        registro.Mensagem.Should().Contain(Recusa.Codigo);
    }

    [Fact]
    public async Task Duracao_vem_do_relogio_injetado_e_nao_do_sistema()
    {
        await Executar(() =>
        {
            _relogio.Advance(TimeSpan.FromMilliseconds(250));
            return Task.FromResult(Result.Success());
        });

        _logger.Registros.Should().ContainSingle().Which.Mensagem.Should().Contain("250");
    }

    private Task<Result> Executar(Func<Task<Result>> handler) =>
        new LoggingBehavior<ComandoDeTeste, Result>(_logger, _relogio).Handle(
            new ComandoDeTeste("Picanha"),
            _ => handler(),
            TestContext.Current.CancellationToken);
}
