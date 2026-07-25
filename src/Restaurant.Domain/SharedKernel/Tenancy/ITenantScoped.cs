using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.SharedKernel.Tenancy;

public interface ITenantScoped
{
    EstabelecimentoId EstabelecimentoId { get; }
}
