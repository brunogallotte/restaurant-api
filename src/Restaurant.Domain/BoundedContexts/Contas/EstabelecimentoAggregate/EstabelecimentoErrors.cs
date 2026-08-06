using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;

public static class EstabelecimentoErrors
{
    public static readonly Error NaoEncontrado = Error.NaoEncontrado(
        "Estabelecimento.NaoEncontrado",
        "Estabelecimento nao encontrado.");

    public static readonly Error Desativado = Error.ConflitoDeEstado(
        "Estabelecimento.Desativado",
        "Estabelecimento desativado nao aceita alteracoes.");

    public static readonly Error JaDesativado = Error.ConflitoDeEstado(
        "Estabelecimento.JaDesativado",
        "Estabelecimento ja esta desativado.");

    public static readonly Error CnpjJaCadastrado = Error.ConflitoDeEstado(
        "Estabelecimento.CnpjJaCadastrado",
        "Ja existe um estabelecimento cadastrado com este CNPJ.");
}
