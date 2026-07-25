using AwesomeAssertions;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.UnitTests.SharedKernel;

public sealed class CnpjTests
{
    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11.222.333/0001-81")]
    [InlineData(" 11222333000181 ")]
    public void Criar_aceita_cnpj_valido_em_qualquer_formatacao(string entrada)
    {
        var resultado = Cnpj.Criar(entrada);

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.Digitos.Should().Be("11222333000181");
    }

    [Fact]
    public void Formatado_aplica_a_mascara_brasileira()
    {
        var cnpj = Cnpj.Criar("11222333000181").Value;

        cnpj.Formatado.Should().Be("11.222.333/0001-81");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("112223330001")]
    [InlineData("112223330001812")]
    [InlineData("abcdefghijklmn")]
    public void Criar_rejeita_quantidade_de_digitos_invalida(string? entrada)
    {
        var resultado = Cnpj.Criar(entrada);

        resultado.Error.Should().Be(Cnpj.FormatoInvalido);
    }

    [Theory]
    [InlineData("11222333000180")]
    [InlineData("11222333000191")]
    [InlineData("12345678901234")]
    public void Criar_rejeita_digito_verificador_invalido(string entrada)
    {
        var resultado = Cnpj.Criar(entrada);

        resultado.Error.Should().Be(Cnpj.DigitoVerificadorInvalido);
    }

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("99999999999999")]
    public void Criar_rejeita_todos_os_digitos_iguais(string entrada)
    {
        var resultado = Cnpj.Criar(entrada);

        resultado.Error.Should().Be(Cnpj.DigitoVerificadorInvalido);
    }

    [Fact]
    public void Igualdade_ignora_a_formatacao_da_entrada()
    {
        var comMascara = Cnpj.Criar("11.222.333/0001-81").Value;
        var semMascara = Cnpj.Criar("11222333000181").Value;

        comMascara.Should().Be(semMascara);
    }
}
