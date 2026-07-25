using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.Ports;

public interface ICategoriaRepository : IRepository<Categoria, CategoriaId>
{
    Task<bool> ExisteAtivaAsync(CategoriaId categoriaId, CancellationToken cancellationToken = default);
}
