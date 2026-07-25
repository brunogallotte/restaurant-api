using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;

public sealed class NomeDeProduto : ValueObject
{
    public const int TamanhoMinimo = 2;
    public const int TamanhoMaximo = 120;

    public static readonly Error MuitoCurto = Error.Validacao(
        "NomeDeProduto.MuitoCurto",
        $"Nome do produto exige ao menos {TamanhoMinimo} caracteres.");

    public static readonly Error MuitoLongo = Error.Validacao(
        "NomeDeProduto.MuitoLongo",
        $"Nome do produto nao pode exceder {TamanhoMaximo} caracteres.");

    private NomeDeProduto(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<NomeDeProduto> Criar(string? entrada)
    {
        var normalizado = string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Join(' ', entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (normalizado.Length < TamanhoMinimo)
        {
            return Result.Failure<NomeDeProduto>(MuitoCurto);
        }

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NomeDeProduto>(MuitoLongo);
        }

        return new NomeDeProduto(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
