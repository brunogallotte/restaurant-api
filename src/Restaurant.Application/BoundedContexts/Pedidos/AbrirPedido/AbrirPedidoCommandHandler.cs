using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Pedidos.Ports;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;

namespace Restaurant.Application.BoundedContexts.Pedidos.AbrirPedido;

internal sealed class AbrirPedidoCommandHandler(
    IPedidoRepository pedidos,
    IMesaRepository mesas,
    IEstabelecimentoRepository estabelecimentos,
    IFuncionarioRepository funcionarios,
    IGeradorDeNumeroDePedido geradorDeNumero,
    ITenantContext tenant,
    TimeProvider relogio) : ICommandHandler<AbrirPedidoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AbrirPedidoCommand command, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var mesaId = new MesaId(command.MesaId);

        var estabelecimento = await estabelecimentos.ObterPorIdAsync(tenant.EstabelecimentoId, cancellationToken);

        if (estabelecimento is null)
        {
            return Result.Failure<Guid>(EstabelecimentoErrors.NaoEncontrado);
        }

        var funcionario = await funcionarios.ObterPorIdAsync(tenant.FuncionarioId, cancellationToken);

        if (funcionario is null || funcionario.EstabelecimentoId != tenant.EstabelecimentoId || !funcionario.Ativo)
        {
            return Result.Failure<Guid>(FuncionarioErrors.NaoEncontrado);
        }

        var mesa = await mesas.ObterPorIdAsync(mesaId, cancellationToken);

        if (mesa is null || mesa.EstabelecimentoId != tenant.EstabelecimentoId)
        {
            return Result.Failure<Guid>(MesaErrors.NaoEncontrada);
        }

        if (await pedidos.ExisteAbertoParaMesaAsync(mesaId, cancellationToken))
        {
            return Result.Failure<Guid>(PedidoErrors.MesaJaTemPedidoAberto);
        }

        var nomeCliente = NomeCliente.Criar(command.NomeCliente);
        var observacao = Observacao.Criar(command.Observacao);

        var entradas = Result.PrimeiraFalha(nomeCliente, observacao);

        if (entradas.Falhou)
        {
            return Result.Failure<Guid>(entradas.Error);
        }

        var numero = await geradorDeNumero.GerarAsync(
            tenant.EstabelecimentoId,
            DateOnly.FromDateTime(agora.UtcDateTime),
            cancellationToken);

        if (numero.Falhou)
        {
            return Result.Failure<Guid>(numero.Error);
        }

        var abertura = Pedido.Abrir(
            tenant.EstabelecimentoId,
            mesaId,
            tenant.FuncionarioId,
            numero.Value,
            nomeCliente.Value,
            observacao.Value,
            estabelecimento.TaxaDeServico,
            agora);

        if (abertura.Falhou)
        {
            return Result.Failure<Guid>(abertura.Error);
        }

        pedidos.Adicionar(abertura.Value);

        return abertura.Value.Id.Valor;
    }
}
