using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.ArchitectureTests;

public sealed class ConvencaoDePastasTests
{
    private const string Raiz = "Restaurant.Domain";
    private const string BuildingBlocks = $"{Raiz}.BuildingBlocks";
    private const string SharedKernel = $"{Raiz}.SharedKernel";
    private const string BoundedContexts = $"{Raiz}.BoundedContexts";

    private static readonly Assembly Domain = typeof(Entity<>).Assembly;

    [Fact]
    public void BuildingBlocks_nao_conhece_negocio()
    {
        var contaminados = TodosOsTipos()
            .Where(tipo => EstaEm(tipo, BuildingBlocks))
            .Where(tipo => TiposReferenciadosPor(tipo).Any(referencia =>
                EstaEm(referencia, SharedKernel) || EstaEm(referencia, BoundedContexts)))
            .Select(tipo => tipo.FullName);

        contaminados.Should().BeEmpty();
    }

    [Fact]
    public void Contexto_nao_referencia_outro_contexto()
    {
        var vazamentos = TodosOsTipos()
            .Where(tipo => EstaEm(tipo, BoundedContexts))
            .SelectMany(tipo => TiposReferenciadosPor(tipo)
                .Where(referencia => EstaEm(referencia, BoundedContexts))
                .Where(referencia => NomeDoContexto(referencia) != NomeDoContexto(tipo))
                .Select(referencia => $"{tipo.Name} -> {referencia.FullName}"));

        vazamentos.Should().BeEmpty();
    }

    [Fact]
    public void Pasta_Events_contem_exatamente_os_domain_events()
    {
        var naPasta = TiposDoDominio().Where(tipo => TerminaEm(tipo, "Events")).ToList();
        var domainEvents = TiposDoDominio()
            .Where(tipo => typeof(IDomainEvent).IsAssignableFrom(tipo) && !tipo.IsInterface)
            .ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().BeEquivalentTo(domainEvents);
    }

    [Fact]
    public void Pasta_ValueObjects_contem_exatamente_os_value_objects()
    {
        var naPasta = TiposDoDominio().Where(tipo => TerminaEm(tipo, "ValueObjects")).ToList();
        var valueObjects = TiposDoDominio()
            .Where(tipo => tipo.IsSubclassOf(typeof(ValueObject)))
            .ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().BeEquivalentTo(valueObjects);
    }

    [Fact]
    public void Pasta_Identifiers_contem_exatamente_os_ids_fortemente_tipados()
    {
        var naPasta = TiposDoDominio().Where(tipo => TerminaEm(tipo, "Identifiers")).ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().AllSatisfy(tipo => EhReadonlyRecordStruct(tipo).Should().BeTrue());

        TiposDoDominio()
            .Where(EhReadonlyRecordStruct)
            .Should().BeEquivalentTo(naPasta);
    }

    [Fact]
    public void Pasta_Enumerations_contem_exatamente_os_smart_enums()
    {
        var naPasta = TiposDoDominio().Where(tipo => TerminaEm(tipo, "Enumerations")).ToList();
        var smartEnums = TiposDoDominio().Where(HerdaDeSmartEnum).ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().BeEquivalentTo(smartEnums);
    }

    [Fact]
    public void Pasta_Ports_so_tem_interface()
    {
        var naPasta = TodosOsTipos().Where(tipo => TerminaEm(tipo, "Ports")).ToList();

        naPasta.Should().NotBeEmpty();
        naPasta.Should().AllSatisfy(tipo => tipo.IsInterface.Should().BeTrue());
    }

    private static IEnumerable<Type> TodosOsTipos() => Domain.GetTypes().Where(EscritoPorHumano);

    private static IEnumerable<Type> TiposDoDominio() =>
        TodosOsTipos()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false } or { IsValueType: true, IsEnum: false });

    private static bool EscritoPorHumano(Type tipo) =>
        !tipo.IsNested
        && !tipo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        && !tipo.Name.Contains('<', StringComparison.Ordinal);

    private static bool EstaEm(Type tipo, string prefixo) =>
        tipo.Namespace?.StartsWith(prefixo, StringComparison.Ordinal) == true;

    private static bool TerminaEm(Type tipo, string segmento) =>
        tipo.Namespace?.EndsWith($".{segmento}", StringComparison.Ordinal) == true;

    private static string NomeDoContexto(Type tipo) =>
        tipo.Namespace!.Split('.').ElementAtOrDefault(3) ?? string.Empty;

    private static bool EhReadonlyRecordStruct(Type tipo) =>
        tipo is { IsValueType: true, IsEnum: false }
        && tipo.GetCustomAttributesData().Any(atributo => atributo.AttributeType.Name == "IsReadOnlyAttribute")
        && tipo.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static bool HerdaDeSmartEnum(Type tipo)
    {
        for (var atual = tipo.BaseType; atual is not null; atual = atual.BaseType)
        {
            if (atual.IsGenericType && atual.GetGenericTypeDefinition() == typeof(SmartEnum<>))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<Type> TiposReferenciadosPor(Type tipo)
    {
        const BindingFlags Todos = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var deBase = tipo.BaseType is null ? [] : new[] { tipo.BaseType };
        var deInterfaces = tipo.GetInterfaces();
        var deCampos = tipo.GetFields(Todos).Select(campo => campo.FieldType);
        var dePropriedades = tipo.GetProperties(Todos).Select(propriedade => propriedade.PropertyType);
        var deMetodos = tipo.GetMethods(Todos)
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType));

        return deBase
            .Concat(deInterfaces)
            .Concat(deCampos)
            .Concat(dePropriedades)
            .Concat(deMetodos)
            .SelectMany(Desembrulhar)
            .Where(referencia => referencia.Assembly == Domain)
            .Distinct();
    }

    private static IEnumerable<Type> Desembrulhar(Type tipo)
    {
        yield return tipo;

        if (tipo.IsGenericType)
        {
            foreach (var argumento in tipo.GetGenericArguments())
            {
                yield return argumento;
            }
        }
    }
}
