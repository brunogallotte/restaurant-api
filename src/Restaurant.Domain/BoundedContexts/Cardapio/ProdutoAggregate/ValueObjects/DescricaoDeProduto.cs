using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;

public sealed class DescricaoDeProduto : ValueObject
{
    public const int TamanhoMaximo = 500;

    public static readonly Error MuitoLonga = Error.Validacao(
        "DescricaoDeProduto.MuitoLonga",
        $"Descricao nao pode exceder {TamanhoMaximo} caracteres.");

    private DescricaoDeProduto(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<DescricaoDeProduto?> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Success<DescricaoDeProduto?>(null);
        }

        var normalizado = entrada.Trim();

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<DescricaoDeProduto?>(MuitoLonga);
        }

        return Result.Success<DescricaoDeProduto?>(new DescricaoDeProduto(normalizado));
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
