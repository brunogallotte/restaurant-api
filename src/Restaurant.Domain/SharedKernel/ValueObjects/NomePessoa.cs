using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.SharedKernel.ValueObjects;

public sealed class NomePessoa : ValueObject
{
    public const int TamanhoMaximo = 120;

    public static readonly Error Vazio = Error.Validacao(
        "NomePessoa.Vazio",
        "Nome e obrigatorio.");

    public static readonly Error MuitoLongo = Error.Validacao(
        "NomePessoa.MuitoLongo",
        $"Nome nao pode exceder {TamanhoMaximo} caracteres.");

    private NomePessoa(string valor) => Valor = valor;

    public string Valor { get; }

    public string PrimeiroNome => Valor.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    public static Result<NomePessoa> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Failure<NomePessoa>(Vazio);
        }

        var normalizado = NormalizarEspacos(entrada);

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NomePessoa>(MuitoLongo);
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
