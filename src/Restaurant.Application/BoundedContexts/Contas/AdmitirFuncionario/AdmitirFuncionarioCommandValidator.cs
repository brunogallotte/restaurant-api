using FluentValidation;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;

namespace Restaurant.Application.BoundedContexts.Contas.AdmitirFuncionario;

internal sealed class AdmitirFuncionarioCommandValidator : AbstractValidator<AdmitirFuncionarioCommand>
{
    public AdmitirFuncionarioCommandValidator() =>
        RuleFor(comando => comando.Cargo)
            .Must(EhCargoConhecido)
            .WithMessage(comando => $"Cargo '{comando.Cargo}' nao existe.");

    private static bool EhCargoConhecido(string? cargo) =>
        Cargo.Todos.Any(conhecido => string.Equals(conhecido.Nome, cargo, StringComparison.OrdinalIgnoreCase));
}
