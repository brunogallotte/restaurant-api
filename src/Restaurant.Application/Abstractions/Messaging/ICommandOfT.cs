using MediatR;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.Abstractions.Messaging;

public interface ICommand<TResponse> : ICommandBase, IRequest<Result<TResponse>>
{
}
