using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.BoundedContexts.Cardapio.CriarCategoria;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.BoundedContexts.Cardapio;

public sealed class CriarCategoriaTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly TenantContextFake _tenant = new();

    [Fact]
    public async Task Cria_a_categoria_no_estabelecimento_do_tenant()
    {
        Categoria? adicionada = null;
        _categorias.Adicionar(Arg.Do<Categoria>(argumento => adicionada = argumento));

        var resultado = await Executar(new CriarCategoriaCommand("Sobremesas", 3));

        resultado.Sucesso.Should().BeTrue();
        adicionada!.EstabelecimentoId.Should().Be(_tenant.EstabelecimentoId);
        adicionada.Nome.Valor.Should().Be("Sobremesas");
        adicionada.Ordem.Should().Be(3);
    }

    [Fact]
    public async Task Nome_curto_demais_propaga_o_erro_do_value_object()
    {
        var resultado = await Executar(new CriarCategoriaCommand("X", 1));

        resultado.Error.Should().Be(NomeDeCategoria.MuitoCurto);
        _categorias.DidNotReceive().Adicionar(Arg.Any<Categoria>());
    }

    [Fact]
    public async Task Ordem_negativa_e_recusada_pelo_agregado()
    {
        var resultado = await Executar(new CriarCategoriaCommand("Sobremesas", -1));

        resultado.Error.Should().Be(CategoriaErrors.OrdemNegativa);
        _categorias.DidNotReceive().Adicionar(Arg.Any<Categoria>());
    }

    private Task<Result<Guid>> Executar(CriarCategoriaCommand comando) =>
        new CriarCategoriaCommandHandler(_categorias, _tenant)
            .Handle(comando, TestContext.Current.CancellationToken);
}
