using AwesomeAssertions;
using NSubstitute;
using Restaurant.Application.Abstractions.Behaviors;
using Restaurant.Application.UnitTests.TestDoubles;
using Restaurant.Domain.BuildingBlocks.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.UnitTests.Abstractions;

public sealed class UnitOfWorkBehaviorTests
{
    private static readonly Error Recusa = Error.ConflitoDeEstado("Teste.Recusa", "Recusado pelo agregado.");

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Comando_bem_sucedido_commita()
    {
        var behavior = new UnitOfWorkBehavior<ComandoDeTeste, Result>(_unitOfWork);

        var resultado = await behavior.Handle(
            new ComandoDeTeste("Picanha"),
            _ => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        resultado.Sucesso.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comando_recusado_nao_commita()
    {
        var behavior = new UnitOfWorkBehavior<ComandoDeTeste, Result>(_unitOfWork);

        var resultado = await behavior.Handle(
            new ComandoDeTeste("Picanha"),
            _ => Task.FromResult(Result.Failure(Recusa)),
            TestContext.Current.CancellationToken);

        resultado.Error.Should().Be(Recusa);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comando_com_resposta_commita_e_preserva_o_valor()
    {
        var identificador = Guid.CreateVersion7();
        var behavior = new UnitOfWorkBehavior<ComandoComRespostaDeTeste, Result<Guid>>(_unitOfWork);

        var resultado = await behavior.Handle(
            new ComandoComRespostaDeTeste("Picanha"),
            _ => Task.FromResult(Result.Success(identificador)),
            TestContext.Current.CancellationToken);

        resultado.Value.Should().Be(identificador);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
