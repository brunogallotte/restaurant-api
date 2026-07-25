using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Events;

public sealed record FuncionarioAdmitidoDomainEvent(
    FuncionarioId FuncionarioId,
    EstabelecimentoId EstabelecimentoId,
    string Nome,
    string Email,
    string Cargo,
    DateTimeOffset AdmitidoEm) : IDomainEvent;
