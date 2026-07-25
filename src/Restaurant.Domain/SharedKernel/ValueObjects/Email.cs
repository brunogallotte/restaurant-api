using System.Text.RegularExpressions;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.SharedKernel.ValueObjects;

public sealed partial class Email : ValueObject
{
    public const int TamanhoMaximo = 254;

    public static readonly Error Vazio = Error.Validacao(
        "Email.Vazio",
        "E-mail e obrigatorio.");

    public static readonly Error FormatoInvalido = Error.Validacao(
        "Email.FormatoInvalido",
        "E-mail em formato invalido.");

    private Email(string valor) => Valor = valor;

    public string Valor { get; }

    public string Dominio => Valor[(Valor.IndexOf('@', StringComparison.Ordinal) + 1)..];

    public static Result<Email> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Failure<Email>(Vazio);
        }

        var normalizado = entrada.Trim().ToLowerInvariant();

        if (normalizado.Length > TamanhoMaximo || !FormatoDeEmail().IsMatch(normalizado))
        {
            return Result.Failure<Email>(FormatoInvalido);
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
