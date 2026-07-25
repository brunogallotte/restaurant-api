using System.Collections.Frozen;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.SharedKernel.ValueObjects;

public sealed class Endereco : ValueObject
{
    public const int TamanhoMaximoDeTexto = 120;

    public static readonly Error LogradouroObrigatorio = Error.Validacao(
        "Endereco.LogradouroObrigatorio",
        "Logradouro e obrigatorio.");

    public static readonly Error NumeroObrigatorio = Error.Validacao(
        "Endereco.NumeroObrigatorio",
        "Numero e obrigatorio. Use 'S/N' quando nao houver.");

    public static readonly Error BairroObrigatorio = Error.Validacao(
        "Endereco.BairroObrigatorio",
        "Bairro e obrigatorio.");

    public static readonly Error CidadeObrigatoria = Error.Validacao(
        "Endereco.CidadeObrigatoria",
        "Cidade e obrigatoria.");

    public static readonly Error UfInvalida = Error.Validacao(
        "Endereco.UfInvalida",
        "UF deve ser uma das 27 unidades federativas brasileiras.");

    public static readonly Error TextoMuitoLongo = Error.Validacao(
        "Endereco.TextoMuitoLongo",
        $"Campos de endereco nao podem exceder {TamanhoMaximoDeTexto} caracteres.");

    private static readonly FrozenSet<string> UnidadesFederativas = new[]
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO",
        "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private Endereco(
        string logradouro,
        string numero,
        string? complemento,
        string bairro,
        string cidade,
        string uf,
        Cep cep)
    {
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cidade = cidade;
        Uf = uf;
        Cep = cep;
    }

    public string Logradouro { get; }

    public string Numero { get; }

    public string? Complemento { get; }

    public string Bairro { get; }

    public string Cidade { get; }

    public string Uf { get; }

    public Cep Cep { get; }

    public static Result<Endereco> Criar(
        string? logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        string? cidade,
        string? uf,
        Cep cep)
    {
        var obrigatorios = Result.PrimeiraFalha(
            ExigirPreenchido(logradouro, LogradouroObrigatorio),
            ExigirPreenchido(numero, NumeroObrigatorio),
            ExigirPreenchido(bairro, BairroObrigatorio),
            ExigirPreenchido(cidade, CidadeObrigatoria));

        if (obrigatorios.Falhou)
        {
            return Result.Failure<Endereco>(obrigatorios.Error);
        }

        var ufNormalizada = uf?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!UnidadesFederativas.Contains(ufNormalizada))
        {
            return Result.Failure<Endereco>(UfInvalida);
        }

        var complementoNormalizado = NormalizarOpcional(complemento);
        var campos = new[]
        {
            Normalizar(logradouro),
            Normalizar(numero),
            complementoNormalizado ?? string.Empty,
            Normalizar(bairro),
            Normalizar(cidade),
        };

        if (campos.Any(campo => campo.Length > TamanhoMaximoDeTexto))
        {
            return Result.Failure<Endereco>(TextoMuitoLongo);
        }

        return new Endereco(
            campos[0],
            campos[1],
            complementoNormalizado,
            campos[3],
            campos[4],
            ufNormalizada,
            cep);
    }

    public override string ToString() =>
        $"{Logradouro}, {Numero}{(Complemento is null ? string.Empty : $" - {Complemento}")}, " +
        $"{Bairro}, {Cidade}/{Uf}, {Cep}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Logradouro;
        yield return Numero;
        yield return Complemento;
        yield return Bairro;
        yield return Cidade;
        yield return Uf;
        yield return Cep;
    }

    private static Result ExigirPreenchido(string? valor, Error erro) =>
        string.IsNullOrWhiteSpace(valor) ? Result.Failure(erro) : Result.Success();

    private static string Normalizar(string? entrada) =>
        string.IsNullOrWhiteSpace(entrada)
            ? string.Empty
            : string.Join(' ', entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? NormalizarOpcional(string? entrada) =>
        Normalizar(entrada) is { Length: > 0 } texto ? texto : null;
}
