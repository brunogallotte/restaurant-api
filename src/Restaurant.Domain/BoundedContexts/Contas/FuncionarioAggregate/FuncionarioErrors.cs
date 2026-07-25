using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;

public static class FuncionarioErrors
{
    public static readonly Error Desligado = Error.ConflitoDeEstado(
        "Funcionario.Desligado",
        "Funcionario desligado nao aceita alteracoes.");

    public static readonly Error JaDesligado = Error.ConflitoDeEstado(
        "Funcionario.JaDesligado",
        "Funcionario ja esta desligado.");

    public static readonly Error DesligamentoAntesDaAdmissao = Error.Validacao(
        "Funcionario.DesligamentoAntesDaAdmissao",
        "Data de desligamento nao pode ser anterior a admissao.");
}
