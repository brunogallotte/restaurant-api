using System.Collections.Concurrent;
using System.Reflection;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.Abstractions.Results;

public static class FailedResult
{
    private static readonly ConcurrentDictionary<Type, Func<Error, Result>> Fabricas = new();

    public static TResponse De<TResponse>(Error error)
        where TResponse : Result =>
        (TResponse)Fabricas.GetOrAdd(typeof(TResponse), CriarFabrica)(error);

    private static Func<Error, Result> CriarFabrica(Type tipo)
    {
        if (tipo == typeof(Result))
        {
            return Result.Failure;
        }

        return typeof(FailedResult)
            .GetMethod(nameof(ComValor), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(tipo.GenericTypeArguments[0])
            .CreateDelegate<Func<Error, Result>>();
    }

    private static Result<TValue> ComValor<TValue>(Error error) => Result.Failure<TValue>(error);
}
