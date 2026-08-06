using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.BoundedContexts.Contas.AdmitirFuncionario;

internal sealed class AdmitirFuncionarioCommandHandler(
    IFuncionarioRepository funcionarios,
    IEstabelecimentoRepository estabelecimentos,
    ITenantContext tenant,
    TimeProvider relogio) : ICommandHandler<AdmitirFuncionarioCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AdmitirFuncionarioCommand command, CancellationToken cancellationToken)
    {
        var estabelecimento = await estabelecimentos.ObterPorIdAsync(tenant.EstabelecimentoId, cancellationToken);

        if (estabelecimento is null)
        {
            return Result.Failure<Guid>(EstabelecimentoErrors.NaoEncontrado);
        }

        var nome = NomePessoa.Criar(command.Nome);
        var email = Email.Criar(command.Email);

        var entradas = Result.PrimeiraFalha(nome, email);

        if (entradas.Falhou)
        {
            return Result.Failure<Guid>(entradas.Error);
        }

        var admissao = Funcionario.Admitir(
            tenant.EstabelecimentoId,
            nome.Value,
            email.Value,
            Cargo.DeNome(command.Cargo),
            relogio.GetUtcNow());

        if (admissao.Falhou)
        {
            return Result.Failure<Guid>(admissao.Error);
        }

        funcionarios.Adicionar(admissao.Value);

        return admissao.Value.Id.Valor;
    }
}
