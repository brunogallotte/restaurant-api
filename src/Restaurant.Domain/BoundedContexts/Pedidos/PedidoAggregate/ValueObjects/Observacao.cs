using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;

public sealed class Observacao : ValueObject
{
    public const int TamanhoMaximo = 280;

    public static readonly Error MuitoLonga = Error.Validacao(
        "Observacao.MuitoLonga",
        $"Observacao nao pode exceder {TamanhoMaximo} caracteres.");

    private Observacao(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<Observacao?> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Success<Observacao?>(null);
        }

        var normalizado = entrada.Trim();

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<Observacao?>(MuitoLonga);
        }

        return Result.Success<Observacao?>(new Observacao(normalizado));
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
