using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.UnitTests.Builders;

internal sealed class FuncionarioBuilder
{
    public static readonly DateTimeOffset AdmissaoPadrao = new(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);

    private EstabelecimentoId _estabelecimentoId = EstabelecimentoId.Novo();
    private string _nome = "Maria da Silva";
    private string _email = "maria@cantina.com.br";
    private Cargo _cargo = Cargo.Garcom;

    public static FuncionarioBuilder Um() => new();

    public FuncionarioBuilder DoEstabelecimento(EstabelecimentoId id)
    {
        _estabelecimentoId = id;
        return this;
    }

    public FuncionarioBuilder Chamado(string nome)
    {
        _nome = nome;
        return this;
    }

    public FuncionarioBuilder ComEmail(string email)
    {
        _email = email;
        return this;
    }

    public FuncionarioBuilder ComCargo(Cargo cargo)
    {
        _cargo = cargo;
        return this;
    }

    public Funcionario Construir()
    {
        var funcionario = Funcionario.Admitir(
            _estabelecimentoId,
            NomePessoa.Criar(_nome).Value,
            Email.Criar(_email).Value,
            _cargo,
            AdmissaoPadrao).Value;

        funcionario.ClearDomainEvents();

        return funcionario;
    }

    public Funcionario ConstruirDesligado()
    {
        var funcionario = Construir();
        funcionario.Desligar(AdmissaoPadrao.AddMonths(6));
        funcionario.ClearDomainEvents();

        return funcionario;
    }
}
