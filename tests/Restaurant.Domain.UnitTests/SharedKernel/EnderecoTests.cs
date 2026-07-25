using AwesomeAssertions;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.UnitTests.SharedKernel;

public sealed class CepTests
{
    [Theory]
    [InlineData("01001000")]
    [InlineData("01001-000")]
    [InlineData(" 01001 000 ")]
    public void Criar_aceita_cep_em_qualquer_formatacao(string entrada) =>
        Cep.Criar(entrada).Value.Digitos.Should().Be("01001000");

    [Fact]
    public void Formatado_aplica_a_mascara() =>
        Cep.Criar("01001000").Value.Formatado.Should().Be("01001-000");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0100100")]
    [InlineData("010010000")]
    public void Criar_rejeita_quantidade_de_digitos_invalida(string? entrada) =>
        Cep.Criar(entrada).Error.Should().Be(Cep.FormatoInvalido);

    [Fact]
    public void Igualdade_ignora_a_formatacao() =>
        Cep.Criar("01001-000").Value.Should().Be(Cep.Criar("01001000").Value);
}

public sealed class EnderecoTests
{
    private static Result<Endereco> Criar(
        string? logradouro = "Rua das Flores",
        string? numero = "123",
        string? complemento = null,
        string? bairro = "Centro",
        string? cidade = "Sao Paulo",
        string? uf = "SP") =>
        Endereco.Criar(logradouro, numero, complemento, bairro, cidade, uf, Cep.Criar("01001000").Value);

    [Fact]
    public void Criar_com_todos_os_campos_obrigatorios_funciona()
    {
        var endereco = Criar().Value;

        endereco.Logradouro.Should().Be("Rua das Flores");
        endereco.Uf.Should().Be("SP");
        endereco.Complemento.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Logradouro_obrigatorio(string? valor) =>
        Criar(logradouro: valor).Error.Should().Be(Endereco.LogradouroObrigatorio);

    [Fact]
    public void Numero_obrigatorio() =>
        Criar(numero: "  ").Error.Should().Be(Endereco.NumeroObrigatorio);

    [Fact]
    public void Bairro_obrigatorio() =>
        Criar(bairro: null).Error.Should().Be(Endereco.BairroObrigatorio);

    [Fact]
    public void Cidade_obrigatoria() =>
        Criar(cidade: null).Error.Should().Be(Endereco.CidadeObrigatoria);

    [Theory]
    [InlineData("XX")]
    [InlineData("SPP")]
    [InlineData("")]
    [InlineData(null)]
    public void Uf_precisa_ser_uma_das_27(string? uf) =>
        Criar(uf: uf).Error.Should().Be(Endereco.UfInvalida);

    [Theory]
    [InlineData("sp")]
    [InlineData(" Sp ")]
    public void Uf_e_normalizada_para_maiusculo(string uf) =>
        Criar(uf: uf).Value.Uf.Should().Be("SP");

    [Fact]
    public void Campo_acima_do_limite_e_rejeitado() =>
        Criar(logradouro: new string('a', Endereco.TamanhoMaximoDeTexto + 1))
            .Error.Should().Be(Endereco.TextoMuitoLongo);

    [Fact]
    public void Complemento_em_branco_vira_nulo() =>
        Criar(complemento: "   ").Value.Complemento.Should().BeNull();

    [Fact]
    public void Igualdade_e_estrutural_sobre_todos_os_componentes()
    {
        Criar().Value.Should().Be(Criar().Value);
        Criar(numero: "123").Value.Should().NotBe(Criar(numero: "456").Value);
    }
}
