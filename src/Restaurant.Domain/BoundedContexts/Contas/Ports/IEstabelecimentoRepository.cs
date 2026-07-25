using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Contas.Ports;

public interface IEstabelecimentoRepository : IRepository<Estabelecimento, EstabelecimentoId>
{
}
