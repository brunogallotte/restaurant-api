using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Restaurant.Application.Abstractions.Results;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.Abstractions.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validadores)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var falha = await PrimeiraFalhaAsync(request, cancellationToken);

        if (falha is null)
        {
            return await next(cancellationToken);
        }

        return FailedResult.De<TResponse>(
            Error.Validacao($"Validacao.{falha.PropertyName}", falha.ErrorMessage));
    }

    private async Task<ValidationFailure?> PrimeiraFalhaAsync(TRequest request, CancellationToken cancellationToken)
    {
        var contexto = new ValidationContext<TRequest>(request);

        foreach (var validador in validadores)
        {
            var validacao = await validador.ValidateAsync(contexto, cancellationToken);

            if (!validacao.IsValid)
            {
                return validacao.Errors[0];
            }
        }

        return null;
    }
}
