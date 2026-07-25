using System.Reflection;
using AwesomeAssertions;
using Restaurant.Domain.BuildingBlocks.Events;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.SharedKernel.Tenancy;

namespace Restaurant.ArchitectureTests;

public sealed class ConvencoesDoDominioTests
{
    private static readonly Assembly Domain = typeof(Entity<>).Assembly;

    [Fact]
    public void Agregados_nao_expoem_setter_publico()
    {
        var setterPublicos = TiposDoDominio()
            .Where(tipo => typeof(IAggregateRoot).IsAssignableFrom(tipo))
            .SelectMany(tipo => tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(propriedade => propriedade.SetMethod?.IsPublic == true)
            .Select(propriedade => $"{propriedade.DeclaringType!.Name}.{propriedade.Name}");

        setterPublicos.Should().BeEmpty();
    }

    [Fact]
    public void Entidades_nao_expoem_setter_publico()
    {
        var setterPublicos = TiposDoDominio()
            .Where(EhEntidade)
            .SelectMany(tipo => tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(propriedade => propriedade.SetMethod?.IsPublic == true)
            .Select(propriedade => $"{propriedade.DeclaringType!.Name}.{propriedade.Name}");

        setterPublicos.Should().BeEmpty();
    }

    [Fact]
    public void Agregados_nao_expoem_colecao_mutavel()
    {
        var colecoesMutaveis = TiposDoDominio()
            .Where(tipo => typeof(IAggregateRoot).IsAssignableFrom(tipo))
            .SelectMany(tipo => tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(propriedade => EhColecaoMutavel(propriedade.PropertyType))
            .Select(propriedade => $"{propriedade.DeclaringType!.Name}.{propriedade.Name}");

        colecoesMutaveis.Should().BeEmpty();
    }

    [Fact]
    public void Value_objects_nao_tem_construtor_publico()
    {
        var construtoresPublicos = TiposDoDominio()
            .Where(tipo => tipo.IsSubclassOf(typeof(ValueObject)))
            .Where(tipo => tipo.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(tipo => tipo.Name);

        construtoresPublicos.Should().BeEmpty();
    }

    [Fact]
    public void Agregados_e_entidades_nao_tem_construtor_publico()
    {
        var construtoresPublicos = TiposDoDominio()
            .Where(tipo => EhEntidade(tipo) || typeof(IAggregateRoot).IsAssignableFrom(tipo))
            .Where(tipo => tipo.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(tipo => tipo.Name);

        construtoresPublicos.Should().BeEmpty();
    }

    [Fact]
    public void Domain_events_sao_records_seladas()
    {
        var eventos = TiposDoDominio().Where(tipo => typeof(IDomainEvent).IsAssignableFrom(tipo)).ToList();

        eventos.Should().NotBeEmpty();
        eventos.Should().AllSatisfy(evento =>
        {
            evento.IsSealed.Should().BeTrue();
            EhRecord(evento).Should().BeTrue();
        });
    }

    [Fact]
    public void Domain_events_terminam_com_DomainEvent() =>
        TiposDoDominio()
            .Where(tipo => typeof(IDomainEvent).IsAssignableFrom(tipo))
            .Should().AllSatisfy(evento => evento.Name.Should().EndWith("DomainEvent"));

    [Fact]
    public void Domain_events_nao_carregam_agregado_nem_entidade()
    {
        var propriedadesSuspeitas = TiposDoDominio()
            .Where(tipo => typeof(IDomainEvent).IsAssignableFrom(tipo))
            .SelectMany(tipo => tipo.GetProperties())
            .Where(propriedade =>
                typeof(IAggregateRoot).IsAssignableFrom(propriedade.PropertyType) || EhEntidade(propriedade.PropertyType))
            .Select(propriedade => $"{propriedade.DeclaringType!.Name}.{propriedade.Name}");

        propriedadesSuspeitas.Should().BeEmpty();
    }

    [Fact]
    public void Todo_agregado_e_escopado_por_tenant_ou_e_o_proprio_tenant() =>
        TiposDoDominio()
            .Where(tipo => typeof(IAggregateRoot).IsAssignableFrom(tipo))
            .Should().AllSatisfy(agregado =>
                (typeof(ITenantScoped).IsAssignableFrom(agregado) || typeof(ITenantRoot).IsAssignableFrom(agregado))
                    .Should().BeTrue());

    [Fact]
    public void Existe_um_unico_tenant_root() =>
        TiposDoDominio()
            .Where(tipo => typeof(ITenantRoot).IsAssignableFrom(tipo))
            .Should().HaveCount(1);

    [Fact]
    public void Repositorios_sao_declarados_no_dominio() =>
        InterfacesDoDominio()
            .Where(tipo => tipo.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Should().NotBeEmpty();

    private static IEnumerable<Type> TiposDoDominio() =>
        Domain.GetTypes().Where(tipo => tipo is { IsClass: true, IsAbstract: false });

    private static IEnumerable<Type> InterfacesDoDominio() => Domain.GetTypes().Where(tipo => tipo.IsInterface);

    private static bool EhEntidade(Type tipo)
    {
        for (var atual = tipo.BaseType; atual is not null; atual = atual.BaseType)
        {
            if (atual.IsGenericType && atual.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EhColecaoMutavel(Type tipo) =>
        tipo.IsGenericType
        && (tipo.GetGenericTypeDefinition() == typeof(List<>)
            || tipo.GetGenericTypeDefinition() == typeof(ICollection<>)
            || tipo.GetGenericTypeDefinition() == typeof(IList<>)
            || tipo.GetGenericTypeDefinition() == typeof(HashSet<>));

    private static bool EhRecord(Type tipo) =>
        tipo.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
