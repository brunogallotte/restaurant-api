using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.UnitTests.Builders;

internal sealed class EstabelecimentoBuilder
{
    public const string CnpjValido = "11222333000181";

    public static readonly DateTimeOffset CadastroPadrao = new(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

    private decimal _taxaDeServico = 10m;

    public static EstabelecimentoBuilder Um() => new();

    public static Endereco EnderecoPadrao() =>
        Endereco.Criar(
            "Rua das Flores",
            "123",
            complemento: null,
            "Centro",
            "Sao Paulo",
            "SP",
            Cep.Criar("01001000").Value).Value;

    public EstabelecimentoBuilder ComTaxaDeServico(decimal taxa)
    {
        _taxaDeServico = taxa;
        return this;
    }

    public Estabelecimento Construir()
    {
        var estabelecimento = Estabelecimento.Cadastrar(
            NomeFantasia.Criar("Cantina da Esquina").Value,
            Cnpj.Criar(CnpjValido).Value,
            Email.Criar("contato@cantina.com.br").Value,
            Telefone.Criar("11987654321").Value,
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
