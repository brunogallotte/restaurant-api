using AwesomeAssertions;
using Restaurant.Domain.Compartilhado;
using Restaurant.Domain.Pedidos.ValueObjects;

namespace Restaurant.Domain.UnitTests.Compartilhado;

public sealed class ValueObjectsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Percentual_aceita_a_faixa_de_zero_a_cem(decimal valor) =>
        Percentual.Criar(valor).Sucesso.Should().BeTrue();

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Percentual_rejeita_fora_da_faixa(decimal valor) =>
        Percentual.Criar(valor).Error.Should().Be(CompartilhadoErrors.PercentualForaDaFaixa);

    [Fact]
    public void Percentual_ComoFracao_divide_por_cem() =>
        Percentual.Criar(12.5m).Value.ComoFracao.Should().Be(0.125m);

    [Theory]
    [InlineData("garcom@restaurante.com.br")]
    [InlineData("  GARCOM@Restaurante.COM  ")]
    public void Email_normaliza_para_minusculo_e_sem_espacos(string entrada)
    {
        var email = Email.Criar(entrada).Value;

        email.Valor.Should().Be(email.Valor.ToLowerInvariant().Trim());
        email.Valor.Should().NotContain(" ");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Email_vazio_e_rejeitado(string? entrada) =>
        Email.Criar(entrada).Error.Should().Be(CompartilhadoErrors.EmailVazio);

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("sem@dominio")]
    [InlineData("@semlocal.com")]
    [InlineData("dois@@arrobas.com")]
    [InlineData("com espaco@dominio.com")]
    public void Email_com_formato_invalido_e_rejeitado(string entrada) =>
        Email.Criar(entrada).Error.Should().Be(CompartilhadoErrors.EmailFormatoInvalido);

    [Fact]
    public void Email_expoe_o_dominio() =>
        Email.Criar("garcom@restaurante.com.br").Value.Dominio.Should().Be("restaurante.com.br");

    [Theory]
    [InlineData("11987654321", "(11) 98765-4321")]
    [InlineData("(11) 3456-7890", "(11) 3456-7890")]
    public void Telefone_formata_com_ddd(string entrada, string esperado) =>
        Telefone.Criar(entrada).Value.Formatado.Should().Be(esperado);

    [Theory]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    [InlineData(null)]
    public void Telefone_rejeita_quantidade_de_digitos_invalida(string? entrada) =>
        Telefone.Criar(entrada).Error.Should().Be(CompartilhadoErrors.TelefoneFormatoInvalido);

    [Fact]
    public void NomePessoa_colapsa_espacos_repetidos() =>
        NomePessoa.Criar("  Maria   da   Silva  ").Value.Valor.Should().Be("Maria da Silva");

    [Fact]
    public void NomePessoa_expoe_o_primeiro_nome() =>
        NomePessoa.Criar("Maria da Silva").Value.PrimeiroNome.Should().Be("Maria");

    [Fact]
    public void NomePessoa_acima_do_limite_e_rejeitado() =>
        NomePessoa.Criar(new string('a', NomePessoa.TamanhoMaximo + 1))
            .Error.Should().Be(CompartilhadoErrors.NomePessoaMuitoLongo);

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void Quantidade_aceita_a_faixa_valida(int valor) =>
        Quantidade.Criar(valor).Sucesso.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Quantidade_rejeita_fora_da_faixa(int valor) =>
        Quantidade.Criar(valor).Error.Should().Be(Quantidade.ForaDaFaixa);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Observacao_vazia_produz_nulo_sem_erro(string? entrada)
    {
        var resultado = Observacao.Criar(entrada);

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.Should().BeNull();
    }

    [Fact]
    public void Observacao_acima_do_limite_e_rejeitada() =>
        Observacao.Criar(new string('a', Observacao.TamanhoMaximo + 1))
            .Error.Should().Be(Observacao.MuitoLonga);

    [Fact]
    public void MotivoCancelamento_muito_curto_e_rejeitado() =>
        MotivoCancelamento.Criar("abc").Error.Should().Be(MotivoCancelamento.MuitoCurto);

    [Fact]
    public void MotivoCancelamento_valido_e_normalizado() =>
        MotivoCancelamento.Criar("  prato queimou  ").Value.Valor.Should().Be("prato queimou");

    [Fact]
    public void NumeroPedido_formata_dia_e_sequencial() =>
        NumeroPedido.Criar(new DateOnly(2026, 7, 25), 42).Value.Valor.Should().Be("20260725-0042");

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void NumeroPedido_rejeita_sequencial_fora_da_faixa(int sequencial) =>
        NumeroPedido.Criar(new DateOnly(2026, 7, 25), sequencial)
            .Error.Should().Be(NumeroPedido.SequencialForaDaFaixa);

    [Fact]
    public void NumeroPedido_faz_ida_e_volta_pela_string()
    {
        var original = NumeroPedido.Criar(new DateOnly(2026, 7, 25), 42).Value;

        var reconstituido = NumeroPedido.Reconstituir(original.Valor).Value;

        reconstituido.Should().Be(original);
    }

    [Fact]
    public void NomeCliente_vazio_produz_nulo_sem_erro() =>
        NomeCliente.Criar("  ").Value.Should().BeNull();

    [Fact]
    public void ProdutoDoPedido_sem_nome_e_rejeitado() =>
        ProdutoDoPedido.Criar(ProdutoId.Novo(), "  ", Dinheiro.ZeroEmReal())
            .Error.Should().Be(ProdutoDoPedido.NomeVazio);
}
