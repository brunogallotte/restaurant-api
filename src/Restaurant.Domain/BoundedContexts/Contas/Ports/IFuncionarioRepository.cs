using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Contas.Ports;

public interface IFuncionarioRepository : IRepository<Funcionario, FuncionarioId>
{
    Task<Funcionario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default);
}
