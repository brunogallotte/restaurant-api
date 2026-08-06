using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Restaurant.Application.Abstractions.Behaviors;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.Abstractions;

public sealed class PipelineDeBehaviorsTests
{
    [Fact]
    public void Behaviors_sao_registrados_na_ordem_do_pipeline()
    {
        var services = new ServiceCollection().AddApplication();

        var behaviors = services
            .Where(descritor => descritor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descritor => descritor.ImplementationType)
            .ToList();

        behaviors.Should().Equal(
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(UnitOfWorkBehavior<,>));
    }

    [Fact]
    public void Comando_recebe_o_behavior_de_unidade_de_trabalho()
    {
        var behaviors = ResolverBehaviorsDe<ComandoDeTeste, Result>();

        behaviors.Should().Contain(typeof(UnitOfWorkBehavior<,>));
    }

    [Fact]
    public void Query_nao_recebe_o_behavior_de_unidade_de_trabalho()
    {
        var behaviors = ResolverBehaviorsDe<QueryDeTeste, Result<int>>();

        behaviors.Should().NotContain(typeof(UnitOfWorkBehavior<,>));
    }

    [Fact]
    public void Query_ainda_recebe_log_e_validacao()
    {
        var behaviors = ResolverBehaviorsDe<QueryDeTeste, Result<int>>();

        behaviors.Should().Equal(typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static List<Type> ResolverBehaviorsDe<TRequest, TResponse>()
        where TRequest : IRequest<TResponse> =>
        [.. ConstruirProvedor()
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())];

    private static ServiceProvider ConstruirProvedor() =>
        new ServiceCollection()
            .AddApplication()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(TimeProvider.System)
            .AddSingleton(Substitute.For<IUnitOfWork>())
            .BuildServiceProvider();
}
