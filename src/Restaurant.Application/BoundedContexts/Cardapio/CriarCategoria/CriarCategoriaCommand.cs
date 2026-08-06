using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Cardapio.CriarCategoria;

public sealed record CriarCategoriaCommand(string Nome, int Ordem) : ICommand<Guid>;
