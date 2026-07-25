using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed class NomePessoa : ValueObject
{
    public const int TamanhoMaximo = 120;

    private NomePessoa(string valor) => Valor = valor;

    public string Valor { get; }

    public string PrimeiroNome => Valor.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    public static Result<NomePessoa> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Failure<NomePessoa>(CompartilhadoErrors.NomePessoaVazio);
        }

        var normalizado = NormalizarEspacos(entrada);

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NomePessoa>(CompartilhadoErrors.NomePessoaMuitoLongo);
        }

        return new NomePessoa(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    private static string NormalizarEspacos(string entrada) =>
        string.Join(' ', entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
