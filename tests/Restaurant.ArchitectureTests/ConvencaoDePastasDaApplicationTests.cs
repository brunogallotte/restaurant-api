using System.Reflection;
using AwesomeAssertions;
using MediatR;
using Restaurant.Application;
using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.ArchitectureTests;

public sealed class ConvencaoDePastasDaApplicationTests
{
    private const string Raiz = "Restaurant.Application";
    private const string Abstractions = $"{Raiz}.Abstractions";
    private const string BoundedContexts = $"{Raiz}.BoundedContexts";

    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;

    [Fact]
    public void Abstractions_nao_conhece_negocio()
    {
        var contaminados = TiposDaApplication()
            .Where(tipo => Reflexao.EstaEm(tipo, Abstractions))
            .Where(tipo => Reflexao.TiposReferenciadosPor(tipo, Application)
                .Any(referencia => Reflexao.EstaEm(referencia, BoundedContexts)))
            .Select(tipo => tipo.FullName);

        contaminados.Should().BeEmpty();
    }

    [Fact]
    public void Pasta_Behaviors_contem_exatamente_os_pipeline_behaviors()
    {
        var naPasta = TiposDaApplication().Where(tipo => Reflexao.TerminaEm(tipo, "Behaviors")).ToList();
        var behaviors = TiposDaApplication()
            .Where(tipo => Reflexao.Implementa(tipo, typeof(IPipelineBehavior<,>)))
            .ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().BeEquivalentTo(behaviors);
    }

    [Fact]
    public void Pasta_Ports_so_tem_interface()
    {
        var naPasta = Reflexao.TiposEscritosPorHumano(Application)
            .Where(tipo => Reflexao.TerminaEm(tipo, "Ports"))
            .ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().AllSatisfy(tipo => tipo.IsInterface.Should().BeTrue());
    }

    [Fact]
    public void Pasta_Contracts_so_tem_record_sem_comportamento()
    {
        var naPasta = Reflexao.TiposEscritosPorHumano(Application)
            .Where(tipo => Reflexao.TerminaEm(tipo, "Contracts"))
            .ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().AllSatisfy(tipo =>
        {
            EhRecord(tipo).Should().BeTrue($"{tipo.Name} deve ser record");
            tipo.IsSealed.Should().BeTrue($"{tipo.Name} deve ser sealed");
            MetodosProprios(tipo).Should().BeEmpty($"{tipo.Name} e dado, nao comportamento");
        });
    }

    [Fact]
    public void Todo_caso_de_uso_mora_dentro_de_um_contexto()
    {
        var forasteiros = Requests()
            .Where(tipo => !Reflexao.EstaEm(tipo, BoundedContexts))
            .Select(tipo => tipo.FullName);

        forasteiros.Should().BeEmpty();
    }

    [Fact]
    public void Pasta_de_caso_de_uso_tem_exatamente_um_request_e_um_handler()
    {
        var pastas = Requests()
            .Select(request => request.Namespace!)
            .Distinct()
            .ToList();

        pastas.Should().NotBeEmpty();
        pastas.Should().AllSatisfy(pasta =>
        {
            TiposDaApplication()
                .Count(tipo => tipo.Namespace == pasta && EhRequest(tipo))
                .Should().Be(1, $"{pasta} deve declarar um unico caso de uso");

            TiposDaApplication()
                .Count(tipo => tipo.Namespace == pasta && Reflexao.Implementa(tipo, typeof(IRequestHandler<,>)))
                .Should().Be(1, $"{pasta} deve declarar um unico handler");
        });
    }

    [Fact]
    public void Nome_da_pasta_e_o_nome_do_caso_de_uso()
    {
        Requests().Should().AllSatisfy(request =>
            Reflexao.UltimoSegmentoDoNamespace(request)
                .Should().Be(
                    SemSufixo(request.Name),
                    $"{request.Name} deve morar numa pasta com o nome do caso de uso"));
    }

    private static IEnumerable<Type> TiposDaApplication() =>
        Reflexao.TiposEscritosPorHumano(Application).Where(tipo => tipo is { IsClass: true, IsAbstract: false });

    private static List<Type> Requests() => [.. TiposDaApplication().Where(EhRequest)];

    private static bool EhRequest(Type tipo) =>
        typeof(ICommandBase).IsAssignableFrom(tipo) || Reflexao.Implementa(tipo, typeof(IQuery<>));

    private static bool EhRecord(Type tipo) =>
        tipo.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static IEnumerable<string> MetodosProprios(Type tipo) =>
        tipo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(metodo => !metodo.IsSpecialName)
            .Where(metodo => !metodo.Name.Contains('<', StringComparison.Ordinal))
            .Where(metodo => metodo.Name is not ("Equals" or "GetHashCode" or "ToString" or "Deconstruct"))
            .Select(metodo => metodo.Name);

    private static string SemSufixo(string nome) =>
        nome.Replace("Command", string.Empty, StringComparison.Ordinal)
            .Replace("Query", string.Empty, StringComparison.Ordinal);
}
