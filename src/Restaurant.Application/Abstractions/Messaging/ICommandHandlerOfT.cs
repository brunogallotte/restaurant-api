using MediatR;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
