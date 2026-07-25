using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Events;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.Identifiers;
using Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Enumerations;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.Tenancy;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Pedidos.PedidoAggregate;

public sealed class Pedido : AggregateRoot<PedidoId>, ITenantScoped
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido(
        PedidoId id,
        EstabelecimentoId estabelecimentoId,
        MesaId mesaId,
        FuncionarioId abertoPor,
        NumeroPedido numero,
        NomeCliente? nomeCliente,
        Observacao? observacao,
        Percentual taxaDeServico,
        DateTimeOffset abertoEm) : base(id)
    {
        EstabelecimentoId = estabelecimentoId;
        MesaId = mesaId;
        AbertoPor = abertoPor;
        Numero = numero;
        NomeCliente = nomeCliente;
        Observacao = observacao;
        TaxaDeServico = taxaDeServico;
        AbertoEm = abertoEm;
        Status = StatusPedido.Aberto;
        Prioridade = PrioridadePedido.Normal;
        Moeda = Moeda.Real;
    }

    private Pedido()
    {
        Numero = null!;
        Status = null!;
        Prioridade = null!;
        TaxaDeServico = null!;
        Moeda = null!;
    }

    public EstabelecimentoId EstabelecimentoId { get; private set; }

    public MesaId MesaId { get; private set; }

    public FuncionarioId AbertoPor { get; private set; }

    public NumeroPedido Numero { get; private set; }

    public NomeCliente? NomeCliente { get; private set; }

    public Observacao? Observacao { get; private set; }

    public StatusPedido Status { get; private set; }

    public PrioridadePedido Prioridade { get; private set; }

    public Percentual TaxaDeServico { get; private set; }

    public Moeda Moeda { get; private set; }

    public DateTimeOffset AbertoEm { get; private set; }

    public DateTimeOffset? ConfirmadoEm { get; private set; }

    public DateTimeOffset? ProntoEm { get; private set; }

    public DateTimeOffset? EntregueEm { get; private set; }

    public DateTimeOffset? FechadoEm { get; private set; }

    public IReadOnlyList<ItemPedido> Itens => _itens.AsReadOnly();

    public IEnumerable<ItemPedido> ItensAtivos => _itens.Where(item => !item.EstaCancelado);

    public Dinheiro Subtotal => ItensAtivos.Aggregate(
        Dinheiro.Zero(Moeda),
        (acumulado, item) => acumulado.Somar(item.Total).Value);

    public Dinheiro ValorDaTaxaDeServico => Subtotal.AplicarPercentual(TaxaDeServico);

    public Dinheiro Total => Subtotal.Somar(ValorDaTaxaDeServico).Value;

    public static Result<Pedido> Abrir(
        EstabelecimentoId estabelecimentoId,
        MesaId mesaId,
        FuncionarioId abertoPor,
        NumeroPedido numero,
        NomeCliente? nomeCliente,
        Observacao? observacao,
        Percentual taxaDeServico,
        DateTimeOffset abertoEm)
    {
        var pedido = new Pedido(
            PedidoId.Novo(),
            estabelecimentoId,
            mesaId,
            abertoPor,
            numero,
            nomeCliente,
            observacao,
            taxaDeServico,
            abertoEm);

        pedido.Raise(new PedidoAbertoDomainEvent(
            pedido.Id,
            estabelecimentoId,
            mesaId,
            abertoPor,
            numero.Valor,
            abertoEm));

        return pedido;
    }

    public Result AdicionarItem(
        ProdutoDoPedido produto,
        Quantidade quantidade,
        Observacao? observacao,
        DateTimeOffset adicionadoEm)
    {
        if (!Status.AceitaNovosItens)
        {
            return Result.Failure(PedidoErrors.NaoAceitaItens(Status));
        }

        if (!produto.PrecoUnitario.MesmaMoedaQue(Dinheiro.Zero(Moeda)))
        {
            return Result.Failure(Dinheiro.MoedasDiferentes);
        }

        var item = ItemPedido.Criar(produto, quantidade, observacao);
        _itens.Add(item);

        RetornarParaPreparoSeJaEstavaPronto(adicionadoEm);

        Raise(new ItemAdicionadoAoPedidoDomainEvent(
            Id,
            item.Id,
            produto.ProdutoId,
            produto.Nome,
            quantidade.Valor,
            produto.PrecoUnitario.Valor));

        return Result.Success();
    }

    public Result AlterarQuantidadeDoItem(ItemPedidoId itemId, Quantidade novaQuantidade)
    {
        var item = LocalizarItem(itemId);

        if (item is null)
        {
            return Result.Failure(PedidoErrors.ItemNaoEncontrado);
        }

        var quantidadeAnterior = item.Quantidade.Valor;
        var alteracao = item.AlterarQuantidade(novaQuantidade);

        if (alteracao.Falhou)
        {
            return alteracao;
        }

        Raise(new QuantidadeDoItemAlteradaDomainEvent(Id, itemId, quantidadeAnterior, novaQuantidade.Valor));

        return Result.Success();
    }

    public Result CancelarItem(ItemPedidoId itemId, MotivoCancelamento? motivo)
    {
        var item = LocalizarItem(itemId);

        if (item is null)
        {
            return Result.Failure(PedidoErrors.ItemNaoEncontrado);
        }

        var jaEstavaEmProducao = item.Status.JaEntrouEmProducao;
        var cancelamento = item.Cancelar(motivo);

        if (cancelamento.Falhou)
        {
            return cancelamento;
        }

        Raise(new ItemDoPedidoCanceladoDomainEvent(
            Id,
            itemId,
            item.ProdutoId,
            item.Quantidade.Valor,
            jaEstavaEmProducao,
            motivo?.Valor));

        return Result.Success();
    }

    public Result Confirmar(DateTimeOffset confirmadoEm)
    {
        if (!ItensAtivos.Any())
        {
            return Result.Failure(PedidoErrors.SemItens);
        }

        var transicao = TransicionarPara(StatusPedido.Confirmado, confirmadoEm);

        if (transicao.Falhou)
        {
            return transicao;
        }

        ConfirmadoEm = confirmadoEm;

        Raise(new PedidoConfirmadoDomainEvent(
            Id,
            EstabelecimentoId,
            MesaId,
            ItensAtivos.Count(),
            Subtotal.Valor,
            confirmadoEm));

        return Result.Success();
    }

    public Result IniciarPreparo(DateTimeOffset iniciadoEm)
    {
        if (!ItensAtivos.Any())
        {
            return Result.Failure(PedidoErrors.TodosOsItensCancelados);
        }

        var transicao = TransicionarPara(StatusPedido.EmPreparo, iniciadoEm);

        if (transicao.Falhou)
        {
            return transicao;
        }

        foreach (var item in ItensAtivos.Where(item => item.Status == StatusItemPedido.Pendente))
        {
            GarantirTransicaoDeItem(item, StatusItemPedido.EmPreparo);
        }

        return Result.Success();
    }

    public Result MarcarItemComoPronto(ItemPedidoId itemId)
    {
        var item = LocalizarItem(itemId);

        if (item is null)
        {
            return Result.Failure(PedidoErrors.ItemNaoEncontrado);
        }

        var transicao = item.TransicionarPara(StatusItemPedido.Pronto);

        if (transicao.Falhou)
        {
            return transicao;
        }

        Raise(new ItemDoPedidoProntoDomainEvent(Id, itemId, item.ProdutoId));

        return Result.Success();
    }

    public Result MarcarComoPronto(DateTimeOffset prontoEm)
    {
        if (!ItensAtivos.Any())
        {
            return Result.Failure(PedidoErrors.TodosOsItensCancelados);
        }

        if (!TodosOsItensAtivosEstaoProntos())
        {
            return Result.Failure(PedidoErrors.ItensPendentes);
        }

        var transicao = TransicionarPara(StatusPedido.Pronto, prontoEm);

        if (transicao.Falhou)
        {
            return transicao;
        }

        ProntoEm = prontoEm;

        Raise(new PedidoProntoDomainEvent(Id, MesaId, prontoEm));

        return Result.Success();
    }

    public Result Entregar(DateTimeOffset entregueEm)
    {
        var transicao = TransicionarPara(StatusPedido.Entregue, entregueEm);

        if (transicao.Falhou)
        {
            return transicao;
        }

        EntregueEm = entregueEm;

        foreach (var item in ItensAtivos)
        {
            GarantirTransicaoDeItem(item, StatusItemPedido.Entregue);
        }

        Raise(new PedidoEntregueDomainEvent(Id, MesaId, entregueEm));

        return Result.Success();
    }

    public Result Fechar(DateTimeOffset fechadoEm)
    {
        var transicao = TransicionarPara(StatusPedido.Fechado, fechadoEm);

        if (transicao.Falhou)
        {
            return transicao;
        }

        FechadoEm = fechadoEm;

        Raise(new PedidoFechadoDomainEvent(
            Id,
            EstabelecimentoId,
            MesaId,
            Subtotal.Valor,
            ValorDaTaxaDeServico.Valor,
            Total.Valor,
            fechadoEm));

        return Result.Success();
    }

    public Result Cancelar(MotivoCancelamento motivo, DateTimeOffset canceladoEm)
    {
        var transicao = TransicionarPara(StatusPedido.Cancelado, canceladoEm);

        if (transicao.Falhou)
        {
            return transicao;
        }

        foreach (var item in ItensAtivos.ToList())
        {
            GarantirCancelamentoDeItem(item, motivo);
        }

        Raise(new PedidoCanceladoDomainEvent(Id, MesaId, motivo.Valor, canceladoEm));

        return Result.Success();
    }

    public Result ElevarPrioridade(PrioridadePedido novaPrioridade)
    {
        if (Status.EhFinal)
        {
            return Result.Failure(PedidoErrors.TransicaoInvalida(Status, Status));
        }

        if (!novaPrioridade.EhMaiorQue(Prioridade))
        {
            return Result.Success();
        }

        var prioridadeAnterior = Prioridade;
        Prioridade = novaPrioridade;

        Raise(new PrioridadeDoPedidoElevadaDomainEvent(Id, prioridadeAnterior, novaPrioridade));

        return Result.Success();
    }

    public TimeSpan TempoDecorrido(DateTimeOffset agora) => (FechadoEm ?? agora) - AbertoEm;

    private bool TodosOsItensAtivosEstaoProntos() => ItensAtivos.All(item => item.EstaPronto);

    private static void GarantirTransicaoDeItem(ItemPedido item, StatusItemPedido destino)
    {
        var transicao = item.TransicionarPara(destino);

        if (transicao.Falhou)
        {
            throw new DomainException(
                $"Estado inconsistente do agregado: item {item.Id.Valor} nao pode ir de {item.Status} para {destino}.");
        }
    }

    private static void GarantirCancelamentoDeItem(ItemPedido item, MotivoCancelamento motivo)
    {
        var cancelamento = item.Cancelar(motivo);

        if (cancelamento.Falhou)
        {
            throw new DomainException(
                $"Estado inconsistente do agregado: item {item.Id.Valor} nao pode ser cancelado ({cancelamento.Error}).");
        }
    }

    private ItemPedido? LocalizarItem(ItemPedidoId itemId) =>
        _itens.SingleOrDefault(item => item.Id == itemId);

    private void RetornarParaPreparoSeJaEstavaPronto(DateTimeOffset em)
    {
        if (Status != StatusPedido.Pronto)
        {
            return;
        }

        ProntoEm = null;
        AlterarStatus(StatusPedido.EmPreparo, em);
    }

    private Result TransicionarPara(StatusPedido destino, DateTimeOffset em)
    {
        if (!Status.PodeTransicionarPara(destino))
        {
            return Result.Failure(PedidoErrors.TransicaoInvalida(Status, destino));
        }

        AlterarStatus(destino, em);

        return Result.Success();
    }

    private void AlterarStatus(StatusPedido destino, DateTimeOffset em)
    {
        var statusAnterior = Status;
        Status = destino;

        Raise(new StatusDoPedidoAlteradoDomainEvent(Id, statusAnterior, destino, em));
    }
}
