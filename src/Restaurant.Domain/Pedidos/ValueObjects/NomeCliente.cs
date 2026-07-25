using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Pedidos.ValueObjects;

public sealed class NomeCliente : ValueObject
{
    public const int TamanhoMaximo = 80;

    public static readonly Error MuitoLongo = Error.Validacao(
        "NomeCliente.MuitoLongo",
        $"Nome do cliente nao pode exceder {TamanhoMaximo} caracteres.");

    private NomeCliente(string valor) => Valor = valor;

    public string Valor { get; }

    public static Result<NomeCliente?> Criar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Result.Success<NomeCliente?>(null);
        }

        var normalizado = string.Join(
            ' ',
            entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (normalizado.Length > TamanhoMaximo)
        {
            return Result.Failure<NomeCliente?>(MuitoLongo);
        }

        return Result.Success<NomeCliente?>(new NomeCliente(normalizado));
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
