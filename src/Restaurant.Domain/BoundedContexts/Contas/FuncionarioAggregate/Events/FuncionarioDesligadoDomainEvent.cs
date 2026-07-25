using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Events;

public sealed record FuncionarioDesligadoDomainEvent(
    FuncionarioId FuncionarioId,
    EstabelecimentoId EstabelecimentoId,
    string Cargo,
    DateTimeOffset DesligadoEm) : IDomainEvent;
