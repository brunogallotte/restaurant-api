using System.Reflection;
using AwesomeAssertions;
using FluentValidation;
using MediatR;
using Restaurant.Application;
using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.ArchitectureTests;

public sealed class ConvencoesDaApplicationTests
{
    private const string EventHandlers = "EventHandlers";

    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;

    [Fact]
    public void Comandos_e_queries_sao_records_selados_publicos()
    {
        var requests = Requests();

        requests.Should().NotBeEmpty();
        requests.Should().AllSatisfy(tipo =>
        {
            tipo.IsSealed.Should().BeTrue($"{tipo.Name} deve ser sealed");
            tipo.IsPublic.Should().BeTrue($"{tipo.Name} e contrato com a Api");
            EhRecord(tipo).Should().BeTrue($"{tipo.Name} deve ser record");
        });
    }

    [Fact]
    public void Handlers_sao_internos_e_selados()
    {
        var handlers = Handlers();

        handlers.Should().NotBeEmpty();
        handlers.Should().AllSatisfy(tipo =>
        {
            tipo.IsSealed.Should().BeTrue($"{tipo.Name} deve ser sealed");
            tipo.IsPublic.Should().BeFalse($"{tipo.Name} nao e referenciado fora da Application");
        });
    }

    [Fact]
    public void Todo_request_tem_exatamente_um_handler()
    {
        var handlers = Handlers();

        Requests().Should().AllSatisfy(request =>
            handlers.Count(handler => TrataRequest(handler, request))
                .Should().Be(1, $"{request.Name} precisa de um unico handler"));
    }

    [Fact]
    public void Handlers_usam_as_abstracoes_da_Application()
    {
        var crus = Handlers()
            .Where(handler => !Reflexao.Implementa(handler, typeof(ICommandHandler<>))
                && !Reflexao.Implementa(handler, typeof(ICommandHandler<,>))
                && !Reflexao.Implementa(handler, typeof(IQueryHandler<,>))
                && !Reflexao.Implementa(handler, typeof(INotificationHandler<>)))
            .Select(handler => handler.FullName);

        crus.Should().BeEmpty();
    }

    [Fact]
    public void Nomenclatura_de_comando_query_e_handler()
    {
        Comandos().Should().AllSatisfy(tipo => tipo.Name.Should().EndWith("Command"));
        Queries().Should().AllSatisfy(tipo => tipo.Name.Should().EndWith("Query"));
        Handlers()
            .Where(handler => !Reflexao.Implementa(handler, typeof(INotificationHandler<>)))
            .Should().AllSatisfy(tipo => tipo.Name.Should().EndWith("Handler"));
        Validators().Should().AllSatisfy(tipo => tipo.Name.Should().EndWith("Validator"));
    }

    [Fact]
    public void Validators_validam_o_proprio_comando()
    {
        Validators().Should().AllSatisfy(validator =>
        {
            var validado = Reflexao.InterfacesFechadas(validator, typeof(IValidator<>))[0].GenericTypeArguments[0];

            validado.Namespace.Should().Be(
                validator.Namespace,
                $"{validator.Name} deve morar na pasta do caso de uso que valida");
        });
    }

    [Fact]
    public void Cada_request_tem_no_maximo_um_validator()
    {
        var validators = Validators();

        Requests().Should().AllSatisfy(request =>
            validators.Count(validator => Valida(validator, request))
                .Should().BeLessThanOrEqualTo(1, $"{request.Name} nao pode ter dois validators"));
    }

    [Fact]
    public void Handler_de_comando_nao_conhece_IUnitOfWork()
    {
        var infratores = Handlers()
            .Where(handler => Reflexao.UltimoSegmentoDoNamespace(handler) != EventHandlers)
            .Where(handler => Reflexao.TiposDeParametrosDeConstrutor(handler).Contains(typeof(IUnitOfWork)))
            .Select(handler => handler.FullName);

        infratores.Should().BeEmpty("commit e responsabilidade do UnitOfWorkBehavior");
    }

    [Fact]
    public void Application_nao_declara_catalogo_de_Error()
    {
        const BindingFlags Estaticos = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        var catalogos = TiposDaApplication()
            .Where(tipo => !Reflexao.EstaEm(tipo, "Restaurant.Application.Abstractions"))
            .Where(tipo => tipo.GetFields(Estaticos).Any(campo => campo.FieldType == typeof(Error))
                || tipo.GetProperties(Estaticos).Any(propriedade => propriedade.PropertyType == typeof(Error)))
            .Select(tipo => tipo.FullName);

        catalogos.Should().BeEmpty("o vocabulario de erro e do dominio");
    }

    [Fact]
    public void Application_nao_conhece_AspNetCore()
    {
        var referencias = Application.GetReferencedAssemblies().Select(referencia => referencia.Name);

        referencias.Should().NotContain(nome =>
            nome!.Contains("AspNetCore", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> TiposDaApplication() =>
        Reflexao.TiposEscritosPorHumano(Application).Where(tipo => tipo is { IsClass: true, IsAbstract: false });

    private static List<Type> Requests() => [.. Comandos().Concat(Queries())];

    private static List<Type> Comandos() =>
        [.. TiposDaApplication().Where(tipo => typeof(ICommandBase).IsAssignableFrom(tipo))];

    private static List<Type> Queries() =>
        [.. TiposDaApplication().Where(tipo => Reflexao.Implementa(tipo, typeof(IQuery<>)))];

    private static List<Type> Handlers() =>
        [.. TiposDaApplication().Where(tipo =>
            Reflexao.Implementa(tipo, typeof(IRequestHandler<,>))
            || Reflexao.Implementa(tipo, typeof(INotificationHandler<>)))];

    private static List<Type> Validators() =>
        [.. TiposDaApplication().Where(tipo => Reflexao.Implementa(tipo, typeof(IValidator<>)))];

    private static bool TrataRequest(Type handler, Type request) =>
        Reflexao.InterfacesFechadas(handler, typeof(IRequestHandler<,>))
            .Any(contrato => contrato.GenericTypeArguments[0] == request);

    private static bool Valida(Type validator, Type request) =>
        Reflexao.InterfacesFechadas(validator, typeof(IValidator<>))
            .Any(contrato => contrato.GenericTypeArguments[0] == request);

    private static bool EhRecord(Type tipo) =>
        tipo.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
