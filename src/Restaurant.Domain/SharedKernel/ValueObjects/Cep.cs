using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.SharedKernel.ValueObjects;

public sealed class Cep : ValueObject
{
    public const int QuantidadeDeDigitos = 8;

    public static readonly Error FormatoInvalido = Error.Validacao(
        "Cep.FormatoInvalido",
        $"CEP deve conter {QuantidadeDeDigitos} digitos.");

    private Cep(string digitos) => Digitos = digitos;

    public string Digitos { get; }

    public string Formatado => $"{Digitos[..5]}-{Digitos[5..]}";

    public static Result<Cep> Criar(string? entrada)
    {
        var digitos = string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Concat(entrada.Where(char.IsAsciiDigit));

        if (digitos.Length != QuantidadeDeDigitos)
        {
            return Result.Failure<Cep>(FormatoInvalido);
        }

        return new Cep(digitos);
    }

    public override string ToString() => Formatado;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }
}
