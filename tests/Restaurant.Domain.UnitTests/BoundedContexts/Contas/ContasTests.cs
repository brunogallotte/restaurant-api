using AwesomeAssertions;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Events;
using Restaurant.Domain.SharedKernel.Tenancy;
using Restaurant.Domain.SharedKernel.ValueObjects;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.BoundedContexts.Contas;

public sealed class EstabelecimentoTests
{
    [Fact]
    public void Cadastrar_nasce_ativo_e_levanta_evento()
    {
        var estabelecimento = Estabelecimento.Cadastrar(
            Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.ValueObjects.NomeFantasia
                .Criar("Cantina da Esquina").Value,
            Cnpj.Criar("11222333000181").Value,
            Email.Criar("contato@cantina.com.br").Value,
            Telefone.Criar("11987654321").Value,
            EstabelecimentoBuilder.EnderecoPadrao(),
            Percentual.Criar(10m).Value,
            EstabelecimentoBuilder.CadastroPadrao).Value;

        estabelecimento.Ativo.Should().BeTrue();
        estabelecimento.DesativadoEm.Should().BeNull();
        estabelecimento.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EstabelecimentoCadastradoDomainEvent>()
            .Which.Cnpj.Should().Be("11222333000181");
    }

    [Fact]
    public void Estabelecimento_e_o_proprio_tenant_e_nao_e_tenant_scoped()
    {
        var estabelecimento = EstabelecimentoBuilder.Um().Construir();

        estabelecimento.Should().BeAssignableTo<ITenantRoot>();
        estabelecimento.Should().NotBeAssignableTo<ITenantScoped>();
    }

    [Fact]
    public void AlterarTaxaDeServico_registra_valor_anterior_e_novo()
    {
        var estabelecimento = EstabelecimentoBuilder.Um().ComTaxaDeServico(10m).Construir();

        var resultado = estabelecimento.AlterarTaxaDeServico(Percentual.Criar(12m).Value);

        resultado.Sucesso.Should().BeTrue();
        var evento = estabelecimento.DomainEvents.OfType<TaxaDeServicoAlteradaDomainEvent>().Single();
        evento.TaxaAnterior.Should().Be(10m);
        evento.TaxaNova.Should().Be(12m);
    }

    [Fact]
    public void AlterarTaxaDeServico_para_o_mesmo_valor_nao_levanta_evento()
    {
        var estabelecimento = EstabelecimentoBuilder.Um().ComTaxaDeServico(10m).Construir();

        var resultado = estabelecimento.AlterarTaxaDeServico(Percentual.Criar(10m).Value);

        resultado.Sucesso.Should().BeTrue();
        estabelecimento.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Estabelecimento_desativado_nao_altera_taxa()
    {
        var estabelecimento = EstabelecimentoBuilder.Um().ConstruirDesativado();

        var resultado = estabelecimento.AlterarTaxaDeServico(Percentual.Criar(15m).Value);

        resultado.Error.Should().Be(EstabelecimentoErrors.Desativado);
    }

    [Fact]
    public void Estabelecimento_desativado_nao_atualiza_contato()
    {
        var estabelecimento = EstabelecimentoBuilder.Um().ConstruirDesativado();

        var resultado = estabelecimento.AtualizarContato(
            Email.Criar("novo@cantina.com.br").Value,
            Telefone.Criar("11912345678").Value,
            EstabelecimentoBuilder.EnderecoPadrao());

        resultado.Error.Should().Be(EstabelecimentoErrors.Desativado);
    }

    [Fact]
    public void Desativar_duas_vezes_falha()
    {
        var estabelecimento = EstabelecimentoBuilder.Um().ConstruirDesativado();

        var resultado = estabelecimento.Desativar(EstabelecimentoBuilder.CadastroPadrao.AddYears(2));

        resultado.Error.Should().Be(EstabelecimentoErrors.JaDesativado);
    }

    [Fact]
    public void Cnpj_ganha_uso_em_producao_e_normaliza_a_mascara()
    {
        var comMascara = EstabelecimentoBuilder.Um().ComCnpj("11.222.333/0001-81").Construir();
        var semMascara = EstabelecimentoBuilder.Um().ComCnpj("11222333000181").Construir();

        comMascara.Cnpj.Should().Be(semMascara.Cnpj);
        comMascara.Cnpj.Formatado.Should().Be("11.222.333/0001-81");
    }
}

public sealed class FuncionarioTests
{
    [Fact]
    public void Admitir_nasce_ativo_e_levanta_evento()
    {
        var funcionario = FuncionarioBuilder.Um().ComCargo(Cargo.Garcom).Construir();

        funcionario.Ativo.Should().BeTrue();
        funcionario.Cargo.Should().Be(Cargo.Garcom);
        funcionario.DesligadoEm.Should().BeNull();
    }

    [Fact]
    public void AlterarCargo_registra_anterior_e_novo()
    {
        var funcionario = FuncionarioBuilder.Um().ComCargo(Cargo.Garcom).Construir();

        var resultado = funcionario.AlterarCargo(Cargo.Gerente);

        resultado.Sucesso.Should().BeTrue();
        var evento = funcionario.DomainEvents.OfType<CargoDoFuncionarioAlteradoDomainEvent>().Single();
        evento.CargoAnterior.Should().Be("Garcom");
        evento.CargoNovo.Should().Be("Gerente");
    }

    [Fact]
    public void AlterarCargo_para_o_mesmo_nao_levanta_evento()
    {
        var funcionario = FuncionarioBuilder.Um().ComCargo(Cargo.Garcom).Construir();

        funcionario.AlterarCargo(Cargo.Garcom);

        funcionario.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Funcionario_desligado_nao_muda_de_cargo()
    {
        var funcionario = FuncionarioBuilder.Um().ConstruirDesligado();

        var resultado = funcionario.AlterarCargo(Cargo.Gerente);

        resultado.Error.Should().Be(FuncionarioErrors.Desligado);
    }

    [Fact]
    public void Desligar_duas_vezes_falha()
    {
        var funcionario = FuncionarioBuilder.Um().ConstruirDesligado();

        var resultado = funcionario.Desligar(FuncionarioBuilder.AdmissaoPadrao.AddYears(1));

        resultado.Error.Should().Be(FuncionarioErrors.JaDesligado);
    }

    [Fact]
    public void Desligar_antes_da_admissao_falha()
    {
        var funcionario = FuncionarioBuilder.Um().Construir();

        var resultado = funcionario.Desligar(FuncionarioBuilder.AdmissaoPadrao.AddDays(-1));

        resultado.Error.Should().Be(FuncionarioErrors.DesligamentoAntesDaAdmissao);
        funcionario.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Desligar_levanta_evento_com_o_cargo_que_ocupava()
    {
        var funcionario = FuncionarioBuilder.Um().ComCargo(Cargo.Cozinha).Construir();

        funcionario.Desligar(FuncionarioBuilder.AdmissaoPadrao.AddMonths(3));

        funcionario.DomainEvents.OfType<FuncionarioDesligadoDomainEvent>().Single()
            .Cargo.Should().Be("Cozinha");
    }
}

public sealed class CargoTests
{
    [Theory]
    [InlineData("Proprietario", true, true, true, true, true)]
    [InlineData("Gerente", true, true, true, true, true)]
    [InlineData("Garcom", false, false, true, false, true)]
    [InlineData("Cozinha", false, false, false, true, false)]
    public void Cargo_carrega_as_permissoes_como_conceito_de_dominio(
        string nome,
        bool cardapio,
        bool equipe,
        bool pedido,
        bool preparo,
        bool conta)
    {
        var cargo = Cargo.DeNome(nome);

        cargo.PodeGerenciarCardapio.Should().Be(cardapio);
        cargo.PodeGerenciarEquipe.Should().Be(equipe);
        cargo.PodeRegistrarPedido.Should().Be(pedido);
        cargo.PodeAvancarPreparo.Should().Be(preparo);
        cargo.PodeFecharConta.Should().Be(conta);
    }

    [Fact]
    public void Todos_expoe_os_quatro_cargos() => Cargo.Todos.Should().HaveCount(4);

    [Fact]
    public void Somente_cozinha_avanca_preparo_sem_registrar_pedido() =>
        Cargo.Todos.Where(cargo => cargo.PodeAvancarPreparo && !cargo.PodeRegistrarPedido)
            .Should().ContainSingle().Which.Should().Be(Cargo.Cozinha);
}
