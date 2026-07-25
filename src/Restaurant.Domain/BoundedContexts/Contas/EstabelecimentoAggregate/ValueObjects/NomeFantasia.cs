using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.ValueObjects;

public sealed class NomeFantasia : ValueObject
{
    public const int TamanhoMinimo = 2;
    public const int TamanhoMaximo = 120;

    public static readonly Error MuitoCurto = Error.Validacao(
        "NomeFantasia.MuitoCurto",
        $"Nome fantasia exige ao menos {TamanhoMinimo} caracteres.");

    public static readonly Error MuitoLongo = Error.Validacao(
        "NomeFantasia.MuitoLongo",
        $"Nome fantasia nao pode exceder {TamanhoMaximo} caracteres.");

    private NomeFantasia(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<NomeFantasia> Criar(string? entrada)
    {
        var normalizado = string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Join(' ', entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (normalizado.Length < TamanhoMinimo)
        {
            return Result.Failure<NomeFantasia>(MuitoCurto);
        }

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NomeFantasia>(MuitoLongo);
        }

        return new NomeFantasia(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
