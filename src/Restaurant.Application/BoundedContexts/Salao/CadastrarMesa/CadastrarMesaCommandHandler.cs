using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Salao.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Salao.CadastrarMesa;

internal sealed class CadastrarMesaCommandHandler(
    IMesaRepository mesas,
    ITenantContext tenant) : ICommandHandler<CadastrarMesaCommand, Guid>
{
    public Task<Result<Guid>> Handle(CadastrarMesaCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Cadastrar(command));

    private Result<Guid> Cadastrar(CadastrarMesaCommand command)
    {
        var numero = NumeroDaMesa.Criar(command.Numero);

        if (numero.Falhou)
        {
            return Result.Failure<Guid>(numero.Error);
        }

        var cadastro = Mesa.Cadastrar(tenant.EstabelecimentoId, numero.Value, command.Lugares);

        if (cadastro.Falhou)
        {
            return Result.Failure<Guid>(cadastro.Error);
        }

        mesas.Adicionar(cadastro.Value);

        return cadastro.Value.Id.Valor;
    }
}
