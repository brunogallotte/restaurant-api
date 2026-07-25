using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Pedidos.ValueObjects;

public sealed class Quantidade : ValueObject
{
    public const int Maxima = 99;

    public static readonly Error ForaDaFaixa = Error.Validacao(
        "Quantidade.ForaDaFaixa",
        $"Quantidade deve estar entre 1 e {Maxima}.");

    private Quantidade(int valor) => Valor = valor;

    public int Valor { get; }

    public static Quantidade Uma() => new(1);

    public static Result<Quantidade> Criar(int valor)
    {
        if (valor is < 1 or > Maxima)
        {
            return Result.Failure<Quantidade>(ForaDaFaixa);
        }

        return new Quantidade(valor);
    }

    public override string ToString() => Valor.ToString(provider: null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
