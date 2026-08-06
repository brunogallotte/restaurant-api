using MediatR;
using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.Abstractions.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommandBase
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var resposta = await next(cancellationToken);

        if (resposta.Falhou)
        {
            return resposta;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return resposta;
    }
}
