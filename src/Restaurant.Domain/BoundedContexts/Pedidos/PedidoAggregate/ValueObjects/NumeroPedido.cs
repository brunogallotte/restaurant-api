using System.Globalization;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;

public sealed class NumeroPedido : ValueObject
{
    public const int SequencialMaximo = 9999;

    public static readonly Error SequencialForaDaFaixa = Error.Validacao(
        "NumeroPedido.SequencialForaDaFaixa",
        $"Sequencial do pedido deve estar entre 1 e {SequencialMaximo}.");

    private NumeroPedido(DateOnly dia, int sequencial)
    {
        Dia = dia;
        Sequencial = sequencial;
    }

    public DateOnly Dia { get; }

    public int Sequencial { get; }

    public string Valor => $"{Dia:yyyyMMdd}-{Sequencial:D4}";

    public static Result<NumeroPedido> Criar(DateOnly dia, int sequencial)
    {
        if (sequencial is < 1 or > SequencialMaximo)
        {
            return Result.Failure<NumeroPedido>(SequencialForaDaFaixa);
        }

        return new NumeroPedido(dia, sequencial);
    }

    public static Result<NumeroPedido> Reconstituir(string valor)
    {
        var partes = valor.Split('-');

        if (partes.Length != 2
            || !DateOnly.TryParseExact(partes[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia)
            || !int.TryParse(partes[1], CultureInfo.InvariantCulture, out var sequencial))
        {
            return Result.Failure<NumeroPedido>(SequencialForaDaFaixa);
        }

        return Criar(dia, sequencial);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Dia;
        yield return Sequencial;
    }
}
