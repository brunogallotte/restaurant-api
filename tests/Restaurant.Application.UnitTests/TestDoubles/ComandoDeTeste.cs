using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.UnitTests.TestDoubles;

public sealed record ComandoDeTeste(string Nome) : ICommand;
