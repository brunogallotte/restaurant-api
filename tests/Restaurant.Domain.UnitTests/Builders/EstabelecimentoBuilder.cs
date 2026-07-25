using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.UnitTests.Builders;

internal sealed class EstabelecimentoBuilder
{
    public static readonly DateTimeOffset CadastroPadrao = new(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

    private string _nomeFantasia = "Cantina da Esquina";
    private string _cnpj = "11222333000181";
    private string _email = "contato@cantina.com.br";
    private string _telefone = "11987654321";
    private decimal _taxaDeServico;

    public static EstabelecimentoBuilder Um() => new();

    public EstabelecimentoBuilder Chamado(string nome)
    {
        _nomeFantasia = nome;
        return this;
    }

    public EstabelecimentoBuilder ComCnpj(string cnpj)
    {
        _cnpj = cnpj;
        return this;
    }

    public EstabelecimentoBuilder ComEmail(string email)
    {
        _email = email;
        return this;
    }

    public EstabelecimentoBuilder ComTelefone(string telefone)
    {
        _telefone = telefone;
        return this;
    }

    public EstabelecimentoBuilder ComTaxaDeServico(decimal taxa)
    {
        _taxaDeServico = taxa;
        return this;
    }

    public static Endereco EnderecoPadrao() =>
        Endereco.Criar(
            "Rua das Flores",
            "123",
            complemento: null,
            "Centro",
            "Sao Paulo",
            "SP",
            Cep.Criar("01001000").Value).Value;

    public Estabelecimento Construir()
    {
        var estabelecimento = Estabelecimento.Cadastrar(
            NomeFantasia.Criar(_nomeFantasia).Value,
            Cnpj.Criar(_cnpj).Value,
            Email.Criar(_email).Value,
            Telefone.Criar(_telefone).Value,
            EnderecoPadrao(),
            Percentual.Criar(_taxaDeServico).Value,
            CadastroPadrao).Value;

        estabelecimento.ClearDomainEvents();

        return estabelecimento;
    }

    public Estabelecimento ConstruirDesativado()
    {
        var estabelecimento = Construir();
        estabelecimento.Desativar(CadastroPadrao.AddYears(1));
        estabelecimento.ClearDomainEvents();

        return estabelecimento;
    }
}
