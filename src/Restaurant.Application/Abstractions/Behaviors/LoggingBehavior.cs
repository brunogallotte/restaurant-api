using MediatR;
using Microsoft.Extensions.Logging;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.Abstractions.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider relogio) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var inicio = relogio.GetTimestamp();

        var resposta = await next(cancellationToken);

        Registrar(resposta, relogio.GetElapsedTime(inicio));

        return resposta;
    }

    private void Registrar(TResponse resposta, TimeSpan duracao)
    {
        if (resposta.Sucesso)
        {
            logger.LogInformation(
                "{Requisicao} concluida em {DuracaoEmMilissegundos}ms.",
                typeof(TRequest).Name,
                duracao.TotalMilliseconds);

            return;
        }

        logger.LogWarning(
            "{Requisicao} recusada em {DuracaoEmMilissegundos}ms com {CodigoDoErro}.",
            typeof(TRequest).Name,
            duracao.TotalMilliseconds,
            resposta.Error.Codigo);
    }
}
