using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Events;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.Tenancy;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;

public sealed class Funcionario : AggregateRoot<FuncionarioId>, ITenantScoped
{
    private Funcionario(
        FuncionarioId id,
        EstabelecimentoId estabelecimentoId,
        NomePessoa nome,
        Email email,
        Cargo cargo,
        DateTimeOffset admitidoEm) : base(id)
    {
        EstabelecimentoId = estabelecimentoId;
        Nome = nome;
        Email = email;
        Cargo = cargo;
        AdmitidoEm = admitidoEm;
        Ativo = true;
    }

    private Funcionario()
    {
        Nome = null!;
        Email = null!;
        Cargo = null!;
    }

    public EstabelecimentoId EstabelecimentoId { get; private set; }

    public NomePessoa Nome { get; private set; }

    public Email Email { get; private set; }

    public Cargo Cargo { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset AdmitidoEm { get; private set; }

    public DateTimeOffset? DesligadoEm { get; private set; }

    public static Result<Funcionario> Admitir(
        EstabelecimentoId estabelecimentoId,
        NomePessoa nome,
        Email email,
        Cargo cargo,
        DateTimeOffset admitidoEm)
    {
        var funcionario = new Funcionario(
            FuncionarioId.Novo(),
            estabelecimentoId,
            nome,
            email,
            cargo,
            admitidoEm);

        funcionario.Raise(new FuncionarioAdmitidoDomainEvent(
            funcionario.Id,
            estabelecimentoId,
            nome.Valor,
            email.Valor,
            cargo.Nome,
            admitidoEm));

        return funcionario;
    }

    public Result AlterarCargo(Cargo novoCargo)
    {
        if (!Ativo)
        {
            return Result.Failure(FuncionarioErrors.Desligado);
        }

        if (novoCargo == Cargo)
        {
            return Result.Success();
        }

        var cargoAnterior = Cargo;
        Cargo = novoCargo;

        Raise(new CargoDoFuncionarioAlteradoDomainEvent(Id, cargoAnterior.Nome, novoCargo.Nome));

        return Result.Success();
    }

    public Result AtualizarContato(Email email)
    {
        if (!Ativo)
        {
            return Result.Failure(FuncionarioErrors.Desligado);
        }

        Email = email;

        return Result.Success();
    }

    public Result Desligar(DateTimeOffset desligadoEm)
    {
        if (!Ativo)
        {
            return Result.Failure(FuncionarioErrors.JaDesligado);
        }

        if (desligadoEm < AdmitidoEm)
        {
            return Result.Failure(FuncionarioErrors.DesligamentoAntesDaAdmissao);
        }

        Ativo = false;
        DesligadoEm = desligadoEm;

        Raise(new FuncionarioDesligadoDomainEvent(Id, EstabelecimentoId, Cargo.Nome, desligadoEm));

        return Result.Success();
    }
}
