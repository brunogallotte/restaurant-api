using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.Ports;

public interface IVerificadorDeNomeUnicoDeProduto
{
    Task<bool> EhUnicoAsync(
        EstabelecimentoId estabelecimentoId,
        NomeDeProduto nome,
        ProdutoId? ignorando = null,
        CancellationToken cancellationToken = default);
}
