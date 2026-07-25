using Restaurant.Domain.Abstractions;
using Restaurant.Domain.Compartilhado;

namespace Restaurant.Domain.Pedidos.ValueObjects;

public sealed class ProdutoDoPedido : ValueObject
{
    public const int TamanhoMaximoDoNome = 120;

    public static readonly Error NomeVazio = Error.Validacao(
        "ProdutoDoPedido.NomeVazio",
        "Nome do produto e obrigatorio no item do pedido.");

    public static readonly Error NomeMuitoLongo = Error.Validacao(
        "ProdutoDoPedido.NomeMuitoLongo",
        $"Nome do produto nao pode exceder {TamanhoMaximoDoNome} caracteres.");

    private ProdutoDoPedido(ProdutoId produtoId, string nome, Dinheiro precoUnitario)
    {
        ProdutoId = produtoId;
        Nome = nome;
        PrecoUnitario = precoUnitario;
    }

    public ProdutoId ProdutoId { get; }

    public string Nome { get; }

    public Dinheiro PrecoUnitario { get; }

    public static Result<ProdutoDoPedido> Criar(ProdutoId produtoId, string? nome, Dinheiro precoUnitario)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Result.Failure<ProdutoDoPedido>(NomeVazio);
        }

        var normalizado = nome.Trim();

        if (normalizado.Length > TamanhoMaximoDoNome)
        {
            return Result.Failure<ProdutoDoPedido>(NomeMuitoLongo);
        }

        return new ProdutoDoPedido(produtoId, normalizado, precoUnitario);
    }

    public override string ToString() => $"{Nome} ({PrecoUnitario})";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ProdutoId;
        yield return Nome;
        yield return PrecoUnitario;
    }
}
