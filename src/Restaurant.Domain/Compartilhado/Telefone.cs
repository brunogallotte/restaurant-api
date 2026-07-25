using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed class Telefone : ValueObject
{
    private Telefone(string digitos) => Digitos = digitos;

    public string Digitos { get; }

    public string Ddd => Digitos[..2];

    public string Numero => Digitos[2..];

    public string Formatado => $"({Ddd}) {Numero[..^4]}-{Numero[^4..]}";

    public static Result<Telefone> Criar(string? entrada)
    {
        var digitos = string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Concat(entrada.Where(char.IsAsciiDigit));

        if (digitos.Length is not (10 or 11))
        {
            return Result.Failure<Telefone>(CompartilhadoErrors.TelefoneFormatoInvalido);
        }

        return new Telefone(digitos);
    }

    public override string ToString() => Formatado;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }
}
