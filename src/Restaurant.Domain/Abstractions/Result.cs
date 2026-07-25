namespace Restaurant.Domain.Abstractions;

public class Result
{
    protected Result(bool sucesso, Error error)
    {
        if (sucesso && error != Error.Nenhum)
        {
            throw new DomainException("Result de sucesso nao pode carregar erro.");
        }

        if (!sucesso && error == Error.Nenhum)
        {
            throw new DomainException("Result de falha exige um erro.");
        }

        Sucesso = sucesso;
        Error = error;
    }

    public bool Sucesso { get; }

    public bool Falhou => !Sucesso;

    public Error Error { get; }

    public static Result Success() => new(true, Error.Nenhum);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.Nenhum);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Result PrimeiraFalha(params ReadOnlySpan<Result> resultados)
    {
        foreach (var resultado in resultados)
        {
            if (resultado.Falhou)
            {
                return Failure(resultado.Error);
            }
        }

        return Success();
    }
}
