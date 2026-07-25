using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed class Cnpj : ValueObject
{
    public const int QuantidadeDeDigitos = 14;

    private static readonly int[] PesosPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Cnpj(string digitos) => Digitos = digitos;

    public string Digitos { get; }

    public string Formatado =>
        $"{Digitos[..2]}.{Digitos[2..5]}.{Digitos[5..8]}/{Digitos[8..12]}-{Digitos[12..]}";

    public static Result<Cnpj> Criar(string? entrada)
    {
        var digitos = SomenteDigitos(entrada);

        if (digitos.Length != QuantidadeDeDigitos)
        {
            return Result.Failure<Cnpj>(CompartilhadoErrors.CnpjFormatoInvalido);
        }

        if (TodosOsDigitosIguais(digitos) || !DigitosVerificadoresConferem(digitos))
        {
            return Result.Failure<Cnpj>(CompartilhadoErrors.CnpjDigitoVerificadorInvalido);
        }

        return new Cnpj(digitos);
    }

    public override string ToString() => Formatado;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }

    private static string SomenteDigitos(string? entrada) =>
        string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Concat(entrada.Where(char.IsAsciiDigit));

    private static bool TodosOsDigitosIguais(string digitos) =>
        digitos.All(digito => digito == digitos[0]);

    private static bool DigitosVerificadoresConferem(string digitos) =>
        CalcularDigito(digitos, PesosPrimeiroDigito) == digitos[12] - '0'
        && CalcularDigito(digitos, PesosSegundoDigito) == digitos[13] - '0';

    private static int CalcularDigito(string digitos, int[] pesos)
    {
        var soma = 0;

        for (var posicao = 0; posicao < pesos.Length; posicao++)
        {
            soma += (digitos[posicao] - '0') * pesos[posicao];
        }

        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }
}
