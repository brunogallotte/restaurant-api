using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.BoundedContexts.Contas.AlterarTaxaDeServico;

internal sealed class AlterarTaxaDeServicoCommandHandler(
    IEstabelecimentoRepository estabelecimentos,
    ITenantContext tenant) : ICommandHandler<AlterarTaxaDeServicoCommand>
{
    public async Task<Result> Handle(AlterarTaxaDeServicoCommand command, CancellationToken cancellationToken)
    {
        var estabelecimento = await estabelecimentos.ObterPorIdAsync(tenant.EstabelecimentoId, cancellationToken);

        if (estabelecimento is null)
        {
            return Result.Failure(EstabelecimentoErrors.NaoEncontrado);
        }

        var novaTaxa = Percentual.Criar(command.TaxaDeServico);

        if (novaTaxa.Falhou)
        {
            return Result.Failure(novaTaxa.Error);
        }

        return estabelecimento.AlterarTaxaDeServico(novaTaxa.Value);
    }
}
