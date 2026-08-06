using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Salao.CadastrarMesa;

public sealed record CadastrarMesaCommand(string Numero, int Lugares) : ICommand<Guid>;
