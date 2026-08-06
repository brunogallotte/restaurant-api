using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.BoundedContexts.Cardapio.Contracts;

namespace Restaurant.Application.BoundedContexts.Cardapio.ObterCardapio;

public sealed record ObterCardapioQuery(bool ApenasDisponiveis) : IQuery<IReadOnlyList<ItemDoCardapio>>;
