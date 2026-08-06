using FluentValidation;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;

namespace Restaurant.Application.BoundedContexts.Pedidos.ElevarPrioridadeDoPedido;

internal sealed class ElevarPrioridadeDoPedidoCommandValidator
    : AbstractValidator<ElevarPrioridadeDoPedidoCommand>
{
    public ElevarPrioridadeDoPedidoCommandValidator() =>
        RuleFor(comando => comando.Prioridade)
            .Must(EhPrioridadeConhecida)
            .WithMessage(comando => $"Prioridade '{comando.Prioridade}' nao existe.");

    private static bool EhPrioridadeConhecida(string? prioridade) =>
        PrioridadePedido.Todos.Any(conhecida =>
            string.Equals(conhecida.Nome, prioridade, StringComparison.OrdinalIgnoreCase));
}
