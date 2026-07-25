using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;

public sealed class MotivoCancelamento : ValueObject
{
    public const int TamanhoMinimo = 5;
    public const int TamanhoMaximo = 280;

    public static readonly Error MuitoCurto = Error.Validacao(
        "MotivoCancelamento.MuitoCurto",
        $"Motivo de cancelamento exige ao menos {TamanhoMinimo} caracteres.");

    public static readonly Error MuitoLongo = Error.Validacao(
        "MotivoCancelamento.MuitoLongo",
        $"Motivo de cancelamento nao pode exceder {TamanhoMaximo} caracteres.");

    private MotivoCancelamento(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<MotivoCancelamento> Criar(string? entrada)
    {
        var normalizado = entrada?.Trim() ?? string.Empty;

        if (normalizado.Length < TamanhoMinimo)
        {
            return Result.Failure<MotivoCancelamento>(MuitoCurto);
        }

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<MotivoCancelamento>(MuitoLongo);
        }

        return new MotivoCancelamento(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
