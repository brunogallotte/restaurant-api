using Microsoft.Extensions.Logging;

namespace Restaurant.Application.UnitTests.TestDoubles;

public sealed class LoggerEspiao<TCategoria> : ILogger<TCategoria>
{
    private readonly List<(LogLevel Nivel, string Mensagem)> _registros = [];

    public IReadOnlyList<(LogLevel Nivel, string Mensagem)> Registros => _registros.AsReadOnly();

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _registros.Add((logLevel, formatter(state, exception)));
}
