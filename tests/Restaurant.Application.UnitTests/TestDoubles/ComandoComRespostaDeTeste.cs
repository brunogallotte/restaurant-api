using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.UnitTests.TestDoubles;

public sealed record ComandoComRespostaDeTeste(string Nome) : ICommand<Guid>;
