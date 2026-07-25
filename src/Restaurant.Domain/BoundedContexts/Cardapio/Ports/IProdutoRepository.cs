using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.Ports;

public interface IProdutoRepository : IRepository<Produto, ProdutoId>
{
    Task<IReadOnlyList<Produto>> ListarPorCategoriaAsync(
        CategoriaId categoriaId,
        CancellationToken cancellationToken = default);
}
