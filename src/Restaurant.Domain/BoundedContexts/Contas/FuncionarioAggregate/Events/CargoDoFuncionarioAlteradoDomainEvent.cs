using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Events;

public sealed record CargoDoFuncionarioAlteradoDomainEvent(
    FuncionarioId FuncionarioId,
    string CargoAnterior,
    string CargoNovo) : IDomainEvent;
