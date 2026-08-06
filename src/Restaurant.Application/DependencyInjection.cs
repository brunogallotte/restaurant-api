using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application.Abstractions.Behaviors;
using Restaurant.Domain.BoundedContexts.Pedidos.Policies;

namespace Restaurant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(AssemblyReference.Assembly, includeInternalTypes: true);

        services.AddSingleton(PoliticaDePrioridade.Padrao);

        return services;
    }
}
