using AwesomeAssertions;
using NSubstitute;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.Events;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.ValueObjects;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.ValueObjects;
using Restaurant.Domain.UnitTests.Builders;

namespace Restaurant.Domain.UnitTests.BoundedContexts.Cardapio;

public sealed class CategoriaTests
{
    [Fact]
    public void Criar_com_ordem_negativa_falha()
    {
        var resultado = Categoria.Criar(
            EstabelecimentoId.Novo(),
            NomeDeCategoria.Criar("Bebidas").Value,
            ordem: -1);

        resultado.Error.Should().Be(CategoriaErrors.OrdemNegativa);
    }

    [Fact]
    public void Renomear_registra_anterior_e_novo()
    {
        var categoria = CategoriaBuilder.Uma().Chamada("Carnes").Construir();

        categoria.Renomear(NomeDeCategoria.Criar("Carnes Nobres").Value);

        var evento = categoria.DomainEvents.OfType<CategoriaRenomeadaDomainEvent>().Single();
        evento.NomeAnterior.Should().Be("Carnes");
        evento.NomeNovo.Should().Be("Carnes Nobres");
    }

    [Fact]
    public void Categoria_desativada_nao_renomeia()
    {
        var categoria = CategoriaBuilder.Uma().ConstruirDesativada();

        var resultado = categoria.Renomear(NomeDeCategoria.Criar("Outro Nome").Value);

        resultado.Error.Should().Be(CategoriaErrors.Desativada);
    }

    [Fact]
    public void Reordenar_para_valor_negativo_falha()
    {
        var categoria = CategoriaBuilder.Uma().Construir();

        categoria.Reordenar(-1).Error.Should().Be(CategoriaErrors.OrdemNegativa);
    }

    [Fact]
    public void Desativar_duas_vezes_falha()
    {
        var categoria = CategoriaBuilder.Uma().ConstruirDesativada();

        categoria.Desativar().Error.Should().Be(CategoriaErrors.JaDesativada);
    }
}

public sealed class ProdutoTests
{
    [Fact]
    public async Task Cadastrar_com_preco_zerado_falha()
    {
        var resultado = await Produto.CadastrarAsync(
            EstabelecimentoId.Novo(),
            CategoriaId.Novo(),
            NomeDeProduto.Criar("Couvert").Value,
            descricao: null,
            Dinheiro.ZeroEmReal(),
            TempoDePreparo.DeMinutos(5).Value,
            ProdutoBuilder.VerificadorQueAceita(),
            TestContext.Current.CancellationToken);

        resultado.Error.Should().Be(ProdutoErrors.PrecoZerado);
    }

    [Fact]
    public async Task Cadastrar_com_nome_ja_usado_no_estabelecimento_falha()
    {
        var resultado = await Produto.CadastrarAsync(
            EstabelecimentoId.Novo(),
            CategoriaId.Novo(),
            NomeDeProduto.Criar("Picanha na Chapa").Value,
            descricao: null,
            Dinheiro.CriarEmReal(89.90m).Value,
            TempoDePreparo.DeMinutos(25).Value,
            ProdutoBuilder.VerificadorQueRecusa(),
            TestContext.Current.CancellationToken);

        resultado.Error.Should().Be(ProdutoErrors.NomeJaUtilizado);
    }

    [Fact]
    public async Task Cadastrar_consulta_a_porta_de_unicidade_antes_de_criar()
    {
        var verificador = ProdutoBuilder.VerificadorQueAceita();
        var estabelecimento = EstabelecimentoId.Novo();
        var nome = NomeDeProduto.Criar("Moqueca").Value;

        await Produto.CadastrarAsync(
            estabelecimento,
            CategoriaId.Novo(),
            nome,
            descricao: null,
            Dinheiro.CriarEmReal(70m).Value,
            TempoDePreparo.DeMinutos(30).Value,
            verificador,
            TestContext.Current.CancellationToken);

        await verificador.Received(1).EhUnicoAsync(
            estabelecimento,
            nome,
            Arg.Is<ProdutoId?>(id => id == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Renomear_ignora_o_proprio_produto_na_checagem_de_unicidade()
    {
        var produto = ProdutoBuilder.Um().Construir();
        var verificador = ProdutoBuilder.VerificadorQueAceita();

        await produto.RenomearAsync(
            NomeDeProduto.Criar("Picanha Premium").Value,
            verificador,
            TestContext.Current.CancellationToken);

        await verificador.Received(1).EhUnicoAsync(
            produto.EstabelecimentoId,
            Arg.Any<NomeDeProduto>(),
            produto.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Renomear_para_nome_ja_usado_falha_e_preserva_o_nome_atual()
    {
        var produto = ProdutoBuilder.Um().Chamado("Picanha na Chapa").Construir();

        var resultado = await produto.RenomearAsync(
            NomeDeProduto.Criar("Moqueca").Value,
            ProdutoBuilder.VerificadorQueRecusa(),
            TestContext.Current.CancellationToken);

        resultado.Error.Should().Be(ProdutoErrors.NomeJaUtilizado);
        produto.Nome.Valor.Should().Be("Picanha na Chapa");
    }

    [Fact]
    public async Task Renomear_para_o_mesmo_nome_nao_consulta_a_porta()
    {
        var produto = ProdutoBuilder.Um().Chamado("Picanha na Chapa").Construir();
        var verificador = ProdutoBuilder.VerificadorQueRecusa();

        var resultado = await produto.RenomearAsync(
            NomeDeProduto.Criar("Picanha na Chapa").Value,
            verificador,
            TestContext.Current.CancellationToken);

        resultado.Sucesso.Should().BeTrue();
        await verificador.DidNotReceiveWithAnyArgs().EhUnicoAsync(
            default,
            default!,
            default,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Cadastrar_nasce_disponivel_e_ativo()
    {
        var produto = ProdutoBuilder.Um().Construir();

        produto.Ativo.Should().BeTrue();
        produto.Disponibilidade.Should().Be(DisponibilidadeProduto.Disponivel);
        produto.PodeEntrarEmPedido.Should().BeTrue();
    }

    [Fact]
    public void AlterarPreco_registra_anterior_e_novo_para_auditoria()
    {
        var produto = ProdutoBuilder.Um().ComPreco(89.90m).Construir();

        var resultado = produto.AlterarPreco(Dinheiro.CriarEmReal(99.90m).Value);

        resultado.Sucesso.Should().BeTrue();
        var evento = produto.DomainEvents.OfType<PrecoDoProdutoAlteradoDomainEvent>().Single();
        evento.PrecoAnterior.Should().Be(89.90m);
        evento.PrecoNovo.Should().Be(99.90m);
    }

    [Fact]
    public void AlterarPreco_para_zero_falha()
    {
        var produto = ProdutoBuilder.Um().Construir();

        produto.AlterarPreco(Dinheiro.ZeroEmReal()).Error.Should().Be(ProdutoErrors.PrecoZerado);
    }

    [Fact]
    public void AlterarPreco_em_moeda_diferente_falha()
    {
        var produto = ProdutoBuilder.Um().Construir();
        var emDolar = Dinheiro.Criar(50m, Moeda.Dolar).Value;

        produto.AlterarPreco(emDolar).Error.Should().Be(Dinheiro.MoedasDiferentes);
    }

    [Fact]
    public void AlterarPreco_para_o_mesmo_valor_nao_levanta_evento()
    {
        var produto = ProdutoBuilder.Um().ComPreco(50m).Construir();

        produto.AlterarPreco(Dinheiro.CriarEmReal(50m).Value);

        produto.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarcarComoEsgotado_tira_o_produto_do_pedido()
    {
        var produto = ProdutoBuilder.Um().Construir();

        var resultado = produto.MarcarComoEsgotado();

        resultado.Sucesso.Should().BeTrue();
        produto.PodeEntrarEmPedido.Should().BeFalse();
        produto.DomainEvents.Should().ContainItemsAssignableTo<ProdutoEsgotadoDomainEvent>();
    }

    [Fact]
    public void MarcarComoEsgotado_duas_vezes_falha()
    {
        var produto = ProdutoBuilder.Um().ConstruirEsgotado();

        produto.MarcarComoEsgotado().Error.Should().Be(ProdutoErrors.JaEsgotado);
    }

    [Fact]
    public void Repor_produto_disponivel_falha()
    {
        var produto = ProdutoBuilder.Um().Construir();

        produto.Repor().Error.Should().Be(ProdutoErrors.JaDisponivel);
    }

    [Fact]
    public void Repor_produto_esgotado_volta_a_disponibilidade()
    {
        var produto = ProdutoBuilder.Um().ConstruirEsgotado();

        var resultado = produto.Repor();

        resultado.Sucesso.Should().BeTrue();
        produto.PodeEntrarEmPedido.Should().BeTrue();
    }

    [Theory]
    [InlineData("preco")]
    [InlineData("nome")]
    [InlineData("categoria")]
    [InlineData("esgotado")]
    [InlineData("preparo")]
    public async Task Produto_descontinuado_nao_aceita_alteracao(string operacao)
    {
        var produto = ProdutoBuilder.Um().ConstruirDescontinuado();

        var resultado = operacao switch
        {
            "preco" => produto.AlterarPreco(Dinheiro.CriarEmReal(10m).Value),
            "nome" => await produto.RenomearAsync(
                NomeDeProduto.Criar("Outro Nome").Value,
                ProdutoBuilder.VerificadorQueAceita(),
                TestContext.Current.CancellationToken),
            "categoria" => produto.MoverParaCategoria(CategoriaId.Novo()),
            "esgotado" => produto.MarcarComoEsgotado(),
            _ => produto.AlterarTempoDePreparo(TempoDePreparo.DeMinutos(30).Value),
        };

        resultado.Error.Should().Be(ProdutoErrors.Descontinuado);
    }

    [Fact]
    public void Descontinuar_duas_vezes_falha()
    {
        var produto = ProdutoBuilder.Um().ConstruirDescontinuado();

        produto.Descontinuar().Error.Should().Be(ProdutoErrors.JaDescontinuado);
    }

    [Fact]
    public void MoverParaCategoria_registra_a_troca()
    {
        var origem = CategoriaId.Novo();
        var destino = CategoriaId.Novo();
        var produto = ProdutoBuilder.Um().NaCategoria(origem).Construir();

        produto.MoverParaCategoria(destino);

        var evento = produto.DomainEvents.OfType<ProdutoMovidoDeCategoriaDomainEvent>().Single();
        evento.CategoriaAnterior.Should().Be(origem);
        evento.CategoriaNova.Should().Be(destino);
    }

    [Fact]
    public void MoverParaCategoria_para_a_mesma_nao_levanta_evento()
    {
        var categoria = CategoriaId.Novo();
        var produto = ProdutoBuilder.Um().NaCategoria(categoria).Construir();

        produto.MoverParaCategoria(categoria);

        produto.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(241)]
    public void TempoDePreparo_fora_da_faixa_e_rejeitado(int minutos) =>
        TempoDePreparo.DeMinutos(minutos).Error.Should().Be(TempoDePreparo.ForaDaFaixa);

    [Fact]
    public void DescricaoDeProduto_vazia_produz_nulo_sem_erro() =>
        DescricaoDeProduto.Criar("   ").Value.Should().BeNull();

    [Fact]
    public void Produto_e_Categoria_sao_agregados_independentes()
    {
        var produto = ProdutoBuilder.Um().Construir();

        produto.GetType().GetProperty("Categoria").Should().BeNull();
        produto.CategoriaId.Should().NotBe(default(CategoriaId));
    }
}
