using System.Text.RegularExpressions;
using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed partial class Email : ValueObject
{
    public const int TamanhoMaximo = 254;

    private Email(string valor) => Valor = valor;

    public string Valor { get; }

    public string Dominio => Valor[(Valor.IndexOf('@', StringComparison.Ordinal) + 1)..];

    public static Result<Email> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Failure<Email>(CompartilhadoErrors.EmailVazio);
        }

        var normalizado = entrada.Trim().ToLowerInvariant();

        if (normalizado.Length > TamanhoMaximo || !FormatoDeEmail().IsMatch(normalizado))
        {
            return Result.Failure<Email>(CompartilhadoErrors.EmailFormatoInvalido);
        }

        return new Email(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.ExplicitCapture)]
    private static partial Regex FormatoDeEmail();
}
