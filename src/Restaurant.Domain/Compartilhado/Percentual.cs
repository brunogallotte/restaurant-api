using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed class Percentual : ValueObject
{
    private Percentual(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public decimal ComoFracao => Valor / 100m;

    public bool EstaZerado => Valor == 0m;

    public static Percentual Zero() => new(0m);

    public static Result<Percentual> Criar(decimal valor)
    {
        if (valor is < 0m or > 100m)
        {
            return Result.Failure<Percentual>(CompartilhadoErrors.PercentualForaDaFaixa);
        }

        return new Percentual(Math.Round(valor, 2, MidpointRounding.ToEven));
    }

    public override string ToString() => $"{Valor:N2}%";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
