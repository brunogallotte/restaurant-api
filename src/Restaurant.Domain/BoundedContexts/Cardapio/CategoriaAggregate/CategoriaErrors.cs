using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;

public static class CategoriaErrors
{
    public static readonly Error Desativada = Error.ConflitoDeEstado(
        "Categoria.Desativada",
        "Categoria desativada nao aceita alteracoes.");

    public static readonly Error JaDesativada = Error.ConflitoDeEstado(
        "Categoria.JaDesativada",
        "Categoria ja esta desativada.");

    public static readonly Error OrdemNegativa = Error.Validacao(
        "Categoria.OrdemNegativa",
        "Ordem de exibicao nao pode ser negativa.");
}
