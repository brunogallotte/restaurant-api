using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.SharedKernel.ValueObjects;

public sealed class Percentual : ValueObject
{
    public static readonly Error ForaDaFaixa = Error.Validacao(
        "Percentual.ForaDaFaixa",
        "Percentual deve estar entre 0 e 100.");

    private Percentual(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public decimal ComoFracao => Valor / 100m;

    public bool EstaZerado => Valor == 0m;

    public static Percentual Zero() => new(0m);

    public static Result<Percentual> Criar(decimal valor)
    {
        if (valor is < 0m or > 100m)
        {
            return Result.Failure<Percentual>(ForaDaFaixa);
        }

        return new Percentual(Math.Round(valor, 2, MidpointRounding.ToEven));
    }

    public override string ToString() => $"{Valor:N2}%";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
