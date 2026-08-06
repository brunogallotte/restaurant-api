using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.UnitTests.TestDoubles;

public sealed class TenantContextFake : ITenantContext
{
    public EstabelecimentoId EstabelecimentoId { get; init; } = EstabelecimentoId.Novo();

    public FuncionarioId FuncionarioId { get; init; } = FuncionarioId.Novo();
}
