using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;

public sealed class NumeroDaMesa : ValueObject
{
    public const int TamanhoMaximo = 10;

    public static readonly Error Vazio = Error.Validacao(
        "NumeroDaMesa.Vazio",
        "Numero da mesa e obrigatorio.");

    public static readonly Error MuitoLongo = Error.Validacao(
        "NumeroDaMesa.MuitoLongo",
        $"Numero da mesa nao pode exceder {TamanhoMaximo} caracteres.");

    private NumeroDaMesa(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<NumeroDaMesa> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Failure<NumeroDaMesa>(Vazio);
        }

        var normalizado = entrada.Trim().ToUpperInvariant();

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NumeroDaMesa>(MuitoLongo);
        }

        return new NumeroDaMesa(normalizado);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
