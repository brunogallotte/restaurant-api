using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;

public sealed record ProdutoCadastradoDomainEvent(
    ProdutoId ProdutoId,
    EstabelecimentoId EstabelecimentoId,
    CategoriaId CategoriaId,
    string Nome,
    decimal Preco) : IDomainEvent;
