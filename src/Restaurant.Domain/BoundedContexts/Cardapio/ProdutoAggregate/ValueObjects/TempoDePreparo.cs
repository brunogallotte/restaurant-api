using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;

public sealed class TempoDePreparo : ValueObject
{
    public const int MinutosMinimo = 1;
    public const int MinutosMaximo = 240;

    public static readonly Error ForaDaFaixa = Error.Validacao(
        "TempoDePreparo.ForaDaFaixa",
        $"Tempo de preparo deve estar entre {MinutosMinimo} e {MinutosMaximo} minutos.");

    private TempoDePreparo(int minutos) => Minutos = minutos;

    public int Minutos { get; }

    public TimeSpan Duracao => TimeSpan.FromMinutes(Minutos);

    public static Result<TempoDePreparo> DeMinutos(int minutos)
    {
        if (minutos is < MinutosMinimo or > MinutosMaximo)
        {
            return Result.Failure<TempoDePreparo>(ForaDaFaixa);
        }

        return new TempoDePreparo(minutos);
    }

    public override string ToString() => $"{Minutos} min";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Minutos;
    }
}
