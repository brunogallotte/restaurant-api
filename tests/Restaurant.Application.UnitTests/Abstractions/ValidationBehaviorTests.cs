using AwesomeAssertions;
using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Behaviors;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.Abstractions;

public sealed class ValidationBehaviorTests
{
    private static readonly ComandoDeTeste Comando = new(string.Empty);

    [Fact]
    public async Task Requisicao_valida_segue_para_o_handler()
    {
        var chamou = false;
        var behavior = new ValidationBehavior<ComandoDeTeste, Result>([ValidadorExigindoNome()]);

        var resultado = await behavior.Handle(
            new ComandoDeTeste("Picanha"),
            _ =>
            {
                chamou = true;
                return Task.FromResult(Result.Success());
            },
            TestContext.Current.CancellationToken);

        chamou.Should().BeTrue();
        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Sem_validador_registrado_a_requisicao_passa_direto()
    {
        var behavior = new ValidationBehavior<ComandoDeTeste, Result>([]);

        var resultado = await behavior.Handle(
            Comando,
            _ => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Requisicao_invalida_e_recusada_sem_chamar_o_handler()
    {
        var chamou = false;
        var behavior = new ValidationBehavior<ComandoDeTeste, Result>([ValidadorExigindoNome()]);

        var resultado = await behavior.Handle(
            Comando,
            _ =>
            {
                chamou = true;
                return Task.FromResult(Result.Success());
            },
            TestContext.Current.CancellationToken);

        chamou.Should().BeFalse();
        resultado.Falhou.Should().BeTrue();
        resultado.Error.Tipo.Should().Be(ErrorType.Validacao);
        resultado.Error.Codigo.Should().Be("Validacao.Nome");
    }

    [Fact]
    public async Task Recusa_de_comando_com_resposta_devolve_Result_do_tipo_certo()
    {
        var validador = new InlineValidator<ComandoComRespostaDeTeste>();
        validador.RuleFor(comando => comando.Nome).NotEmpty();
        var behavior = new ValidationBehavior<ComandoComRespostaDeTeste, Result<Guid>>([validador]);

        var resultado = await behavior.Handle(
            new ComandoComRespostaDeTeste(string.Empty),
            _ => Task.FromResult(Result.Success(Guid.Empty)),
            TestContext.Current.CancellationToken);

        resultado.Should().BeOfType<Result<Guid>>();
        resultado.Falhou.Should().BeTrue();
    }

    [Fact]
    public async Task Recusa_nao_lanca_excecao()
    {
        var behavior = new ValidationBehavior<ComandoDeTeste, Result>([ValidadorExigindoNome()]);

        var acao = async () => await behavior.Handle(
            Comando,
            _ => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        await acao.Should().NotThrowAsync();
    }

    private static InlineValidator<ComandoDeTeste> ValidadorExigindoNome()
    {
        var validador = new InlineValidator<ComandoDeTeste>();
        validador.RuleFor(comando => comando.Nome).NotEmpty();

        return validador;
    }
}
