using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.SharedKernel.ValueObjects;

public sealed class Telefone : ValueObject
{
    public static readonly Error FormatoInvalido = Error.Validacao(
        "Telefone.FormatoInvalido",
        "Telefone deve conter 10 ou 11 digitos com DDD.");

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
            return Result.Failure<Telefone>(FormatoInvalido);
        }

        return new Telefone(digitos);
    }

    public override string ToString() => Formatado;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }
}
