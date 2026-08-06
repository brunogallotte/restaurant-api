using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.SharedKernel.Tenancy;

public interface ITenantContext
{
    EstabelecimentoId EstabelecimentoId { get; }

    FuncionarioId FuncionarioId { get; }
}
