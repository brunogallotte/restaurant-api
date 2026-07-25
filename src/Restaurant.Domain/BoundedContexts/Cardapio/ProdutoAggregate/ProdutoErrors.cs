using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;

public static class ProdutoErrors
{
    public static readonly Error Descontinuado = Error.ConflitoDeEstado(
        "Produto.Descontinuado",
        "Produto descontinuado nao aceita alteracoes.");

    public static readonly Error JaDescontinuado = Error.ConflitoDeEstado(
        "Produto.JaDescontinuado",
        "Produto ja esta descontinuado.");

    public static readonly Error JaEsgotado = Error.ConflitoDeEstado(
        "Produto.JaEsgotado",
        "Produto ja esta marcado como esgotado.");

    public static readonly Error JaDisponivel = Error.ConflitoDeEstado(
        "Produto.JaDisponivel",
        "Produto ja esta disponivel.");

    public static readonly Error PrecoZerado = Error.Validacao(
        "Produto.PrecoZerado",
        "Preco do produto deve ser maior que zero.");

    public static readonly Error NomeJaUtilizado = Error.ConflitoDeEstado(
        "Produto.NomeJaUtilizado",
        "Ja existe um produto com este nome neste estabelecimento.");
}
