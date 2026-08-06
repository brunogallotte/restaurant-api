using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.UnitTests.TestDoubles;

public sealed record QueryDeTeste(string Nome) : IQuery<int>;
