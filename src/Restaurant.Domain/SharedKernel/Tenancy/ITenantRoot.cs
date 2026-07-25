using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.SharedKernel.Tenancy;

public interface ITenantRoot
{
    EstabelecimentoId Id { get; }
}
