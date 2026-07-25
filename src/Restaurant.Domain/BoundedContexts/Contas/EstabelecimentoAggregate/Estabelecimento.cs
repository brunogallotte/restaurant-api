using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.Tenancy;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;

public sealed class Estabelecimento : AggregateRoot<EstabelecimentoId>, ITenantRoot
{
    private Estabelecimento(
        EstabelecimentoId id,
        NomeFantasia nomeFantasia,
        Cnpj cnpj,
        Email email,
        Telefone telefone,
        Endereco endereco,
        Percentual taxaDeServico,
        DateTimeOffset cadastradoEm) : base(id)
    {
        NomeFantasia = nomeFantasia;
        Cnpj = cnpj;
        Email = email;
        Telefone = telefone;
        Endereco = endereco;
        TaxaDeServico = taxaDeServico;
        CadastradoEm = cadastradoEm;
        Ativo = true;
    }

    private Estabelecimento()
    {
        NomeFantasia = null!;
        Cnpj = null!;
        Email = null!;
        Telefone = null!;
        Endereco = null!;
        TaxaDeServico = null!;
    }

    public NomeFantasia NomeFantasia { get; private set; }

    public Cnpj Cnpj { get; private set; }

    public Email Email { get; private set; }

    public Telefone Telefone { get; private set; }

    public Endereco Endereco { get; private set; }

    public Percentual TaxaDeServico { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CadastradoEm { get; private set; }

    public DateTimeOffset? DesativadoEm { get; private set; }

    public static Result<Estabelecimento> Cadastrar(
        NomeFantasia nomeFantasia,
        Cnpj cnpj,
        Email email,
        Telefone telefone,
        Endereco endereco,
        Percentual taxaDeServico,
        DateTimeOffset cadastradoEm)
    {
        var estabelecimento = new Estabelecimento(
            EstabelecimentoId.Novo(),
            nomeFantasia,
            cnpj,
            email,
            telefone,
            endereco,
            taxaDeServico,
            cadastradoEm);

        estabelecimento.Raise(new EstabelecimentoCadastradoDomainEvent(
            estabelecimento.Id,
            nomeFantasia.Valor,
            cnpj.Digitos,
            email.Valor,
            cadastradoEm));

        return estabelecimento;
    }

    public Result AlterarTaxaDeServico(Percentual novaTaxa)
    {
        if (!Ativo)
        {
            return Result.Failure(EstabelecimentoErrors.Desativado);
        }

        if (novaTaxa == TaxaDeServico)
        {
            return Result.Success();
        }

        var taxaAnterior = TaxaDeServico;
        TaxaDeServico = novaTaxa;

        Raise(new TaxaDeServicoAlteradaDomainEvent(Id, taxaAnterior.Valor, novaTaxa.Valor));

        return Result.Success();
    }

    public Result AtualizarContato(Email email, Telefone telefone, Endereco endereco)
    {
        if (!Ativo)
        {
            return Result.Failure(EstabelecimentoErrors.Desativado);
        }

        Email = email;
        Telefone = telefone;
        Endereco = endereco;

        return Result.Success();
    }

    public Result Renomear(NomeFantasia novoNome)
    {
        if (!Ativo)
        {
            return Result.Failure(EstabelecimentoErrors.Desativado);
        }

        NomeFantasia = novoNome;

        return Result.Success();
    }

    public Result Desativar(DateTimeOffset desativadoEm)
    {
        if (!Ativo)
        {
            return Result.Failure(EstabelecimentoErrors.JaDesativado);
        }

        Ativo = false;
        DesativadoEm = desativadoEm;

        Raise(new EstabelecimentoDesativadoDomainEvent(Id, desativadoEm));

        return Result.Success();
    }
}
