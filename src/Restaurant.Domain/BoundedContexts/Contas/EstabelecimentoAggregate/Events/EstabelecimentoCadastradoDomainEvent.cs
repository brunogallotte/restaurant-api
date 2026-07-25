using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.Events;

public sealed record EstabelecimentoCadastradoDomainEvent(
    EstabelecimentoId EstabelecimentoId,
    string NomeFantasia,
    string Cnpj,
    string Email,
    DateTimeOffset CadastradoEm) : IDomainEvent;
