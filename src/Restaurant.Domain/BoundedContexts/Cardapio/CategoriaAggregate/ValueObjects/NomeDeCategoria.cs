using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;

public sealed class NomeDeCategoria : ValueObject
{
    public const int TamanhoMinimo = 2;
    public const int TamanhoMaximo = 60;

    public static readonly Error MuitoCurto = Error.Validacao(
        "NomeDeCategoria.MuitoCurto",
        $"Nome da categoria exige ao menos {TamanhoMinimo} caracteres.");

    public static readonly Error MuitoLongo = Error.Validacao(
        "NomeDeCategoria.MuitoLongo",
        $"Nome da categoria nao pode exceder {TamanhoMaximo} caracteres.");

    private NomeDeCategoria(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<NomeDeCategoria> Criar(string? entrada)
    {
        var normalizado = string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Join(' ', entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (normalizado.Length < TamanhoMinimo)
        {
            return Result.Failure<NomeDeCategoria>(MuitoCurto);
        }

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NomeDeCategoria>(MuitoLongo);
        }

        return new NomeDeCategoria(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
