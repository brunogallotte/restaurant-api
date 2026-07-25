using AwesomeAssertions;
using Restaurant.Application;

namespace Restaurant.Application.UnitTests;

public sealed class AbstracoesDaApplicationTests
{
    [Fact]
    public void AssemblyReference_aponta_para_o_assembly_da_Application() =>
        AssemblyReference.Assembly.GetName().Name.Should().Be("Restaurant.Application");

    [Fact]
    public void Application_nao_carrega_EntityFrameworkCore() =>
        AssemblyReference.Assembly.GetReferencedAssemblies()
            .Should().NotContain(referencia =>
                referencia.Name!.Contains("EntityFrameworkCore", StringComparison.Ordinal));
}
