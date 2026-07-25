using AwesomeAssertions;
using Restaurant.Domain.Compartilhado;

namespace Restaurant.Domain.UnitTests.Compartilhado;

public sealed class DinheiroTests
{
    [Fact]
    public void Criar_com_valor_negativo_falha()
    {
        var resultado = Dinheiro.CriarEmReal(-0.01m);

        resultado.Error.Should().Be(CompartilhadoErrors.DinheiroNegativo);
    }

    [Fact]
    public void Criar_com_zero_e_valido()
    {
        var resultado = Dinheiro.CriarEmReal(0m);

        resultado.Sucesso.Should().BeTrue();
        resultado.Value.EstaZerado.Should().BeTrue();
    }

    [Fact]
    public void Criar_arredondia_para_duas_casas()
    {
        var dinheiro = Dinheiro.CriarEmReal(10.005m).Value;

        dinheiro.Valor.Should().Be(10.00m);
    }

    [Fact]
    public void Somar_moedas_diferentes_falha()
    {
        var real = Dinheiro.CriarEmReal(10m).Value;
        var dolar = Dinheiro.Criar(10m, Moeda.Dolar).Value;

        var resultado = real.Somar(dolar);

        resultado.Error.Should().Be(CompartilhadoErrors.DinheiroMoedasDiferentes);
    }

    [Fact]
    public void Somar_mesma_moeda_acumula()
    {
        var resultado = Dinheiro.CriarEmReal(10.50m).Value.Somar(Dinheiro.CriarEmReal(4.50m).Value);

        resultado.Value.Valor.Should().Be(15m);
    }

    [Fact]
    public void Subtrair_abaixo_de_zero_falha()
    {
        var resultado = Dinheiro.CriarEmReal(5m).Value.Subtrair(Dinheiro.CriarEmReal(10m).Value);

        resultado.Error.Should().Be(CompartilhadoErrors.DinheiroNegativo);
    }

    [Fact]
    public void MultiplicarPor_escala_o_valor()
    {
        var total = Dinheiro.CriarEmReal(12.35m).Value.MultiplicarPor(3);

        total.Valor.Should().Be(37.05m);
    }

    [Fact]
    public void AplicarPercentual_calcula_a_fracao()
    {
        var taxa = Dinheiro.CriarEmReal(200m).Value.AplicarPercentual(Percentual.Criar(12.5m).Value);

        taxa.Valor.Should().Be(25m);
    }

    [Fact]
    public void Igualdade_e_estrutural_nao_por_referencia()
    {
        var primeiro = Dinheiro.CriarEmReal(10m).Value;
        var segundo = Dinheiro.CriarEmReal(10m).Value;

        primeiro.Should().Be(segundo);
        (primeiro == segundo).Should().BeTrue();
        primeiro.GetHashCode().Should().Be(segundo.GetHashCode());
        ReferenceEquals(primeiro, segundo).Should().BeFalse();
    }

    [Fact]
    public void Valores_iguais_em_moedas_diferentes_nao_sao_iguais()
    {
        var real = Dinheiro.CriarEmReal(10m).Value;
        var dolar = Dinheiro.Criar(10m, Moeda.Dolar).Value;

        real.Should().NotBe(dolar);
    }
}
