using System.Reflection;
using AwesomeAssertions;

namespace Restaurant.ArchitectureTests;

public sealed class RegraDeDependenciaTests
{
    private static readonly Assembly Domain = typeof(Domain.BuildingBlocks.Model.Entity<>).Assembly;
    private static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Persistence = typeof(Persistence.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_nao_referencia_nenhuma_outra_camada() =>
        NomesReferenciadosPor(Domain).Should().NotContain(nome => nome.StartsWith("Restaurant.", StringComparison.Ordinal));

    [Fact]
    public void Domain_nao_conhece_EntityFrameworkCore() =>
        NomesReferenciadosPor(Domain).Should().NotContain(nome => nome.Contains("EntityFrameworkCore", StringComparison.Ordinal));

    [Fact]
    public void Domain_nao_conhece_AspNetCore() =>
        NomesReferenciadosPor(Domain).Should().NotContain(nome => nome.Contains("AspNetCore", StringComparison.Ordinal));

    [Fact]
    public void Domain_so_depende_de_MediatR_Contracts()
    {
        var externas = NomesReferenciadosPor(Domain)
            .Where(nome => !nome.StartsWith("System", StringComparison.Ordinal) && nome != "netstandard");

        externas.Should().BeEquivalentTo(["MediatR.Contracts"]);
    }

    [Fact]
    public void Application_nao_conhece_EntityFrameworkCore() =>
        NomesReferenciadosPor(Application).Should().NotContain(nome => nome.Contains("EntityFrameworkCore", StringComparison.Ordinal));

    [Fact]
    public void Application_nao_conhece_Npgsql() =>
        NomesReferenciadosPor(Application).Should().NotContain(nome => nome.Contains("Npgsql", StringComparison.Ordinal));

    [Fact]
    public void Application_nao_referencia_camada_externa() =>
        CamadasReferenciadasPor(Application).Should()
            .NotContain("Restaurant.Persistence")
            .And.NotContain("Restaurant.Infrastructure")
            .And.NotContain("Restaurant.Api");

    [Fact]
    public void Persistence_nao_referencia_Infrastructure() =>
        CamadasReferenciadasPor(Persistence).Should().NotContain("Restaurant.Infrastructure");

    [Fact]
    public void Infrastructure_nao_referencia_Persistence() =>
        CamadasReferenciadasPor(Infrastructure).Should().NotContain("Restaurant.Persistence");

    [Fact]
    public void Infrastructure_nao_conhece_EntityFrameworkCore() =>
        NomesReferenciadosPor(Infrastructure).Should().NotContain(nome => nome.Contains("EntityFrameworkCore", StringComparison.Ordinal));

    [Fact]
    public void Nenhuma_camada_de_dentro_referencia_a_Api() =>
        new[] { Domain, Application, Persistence, Infrastructure }
            .Should().AllSatisfy(camada => CamadasReferenciadasPor(camada).Should().NotContain("Restaurant.Api"));

    private static IEnumerable<string> NomesReferenciadosPor(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(referencia => referencia.Name!);

    private static IEnumerable<string> CamadasReferenciadasPor(Assembly assembly) =>
        NomesReferenciadosPor(assembly).Where(nome => nome.StartsWith("Restaurant.", StringComparison.Ordinal));
}
