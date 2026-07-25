using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Enumerations;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Events;
using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.ValueObjects;
using Restaurant.Domain.BuildingBlocks.Model;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.Identifiers;
using Restaurant.Domain.SharedKernel.Tenancy;

namespace Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;

public sealed class Mesa : AggregateRoot<MesaId>, ITenantScoped
{
    private Mesa(
        MesaId id,
        EstabelecimentoId estabelecimentoId,
        NumeroDaMesa numero,
        int lugares) : base(id)
    {
        EstabelecimentoId = estabelecimentoId;
        Numero = numero;
        Lugares = lugares;
        Status = StatusMesa.Livre;
    }

    private Mesa()
    {
        Numero = null!;
        Status = null!;
    }

    public EstabelecimentoId EstabelecimentoId { get; private set; }

    public NumeroDaMesa Numero { get; private set; }

    public int Lugares { get; private set; }

    public StatusMesa Status { get; private set; }

    public DateTimeOffset? OcupadaEm { get; private set; }

    public DateTimeOffset? ReservadaAte { get; private set; }

    public bool EstaLivre => Status == StatusMesa.Livre;

    public static Result<Mesa> Cadastrar(
        EstabelecimentoId estabelecimentoId,
        NumeroDaMesa numero,
        int lugares)
    {
        if (lugares < 1)
        {
            return Result.Failure<Mesa>(MesaErrors.LugaresInvalidos);
        }

        var mesa = new Mesa(MesaId.Novo(), estabelecimentoId, numero, lugares);

        mesa.Raise(new MesaCadastradaDomainEvent(mesa.Id, estabelecimentoId, numero.Valor, lugares));

        return mesa;
    }

    public Result Ocupar(DateTimeOffset ocupadaEm)
    {
        var transicao = TransicionarPara(StatusMesa.Ocupada);

        if (transicao.Falhou)
        {
            return transicao;
        }

        OcupadaEm = ocupadaEm;
        ReservadaAte = null;

        Raise(new MesaOcupadaDomainEvent(Id, EstabelecimentoId, Numero.Valor, ocupadaEm));

        return Result.Success();
    }

    public Result Liberar(DateTimeOffset liberadaEm)
    {
        var transicao = TransicionarPara(StatusMesa.Livre);

        if (transicao.Falhou)
        {
            return transicao;
        }

        OcupadaEm = null;
        ReservadaAte = null;

        Raise(new MesaLiberadaDomainEvent(Id, EstabelecimentoId, Numero.Valor, liberadaEm));

        return Result.Success();
    }

    public Result Reservar(DateTimeOffset reservadaAte, DateTimeOffset agora)
    {
        if (reservadaAte <= agora)
        {
            return Result.Failure(MesaErrors.ReservaNoPassado);
        }

        var transicao = TransicionarPara(StatusMesa.Reservada);

        if (transicao.Falhou)
        {
            return transicao;
        }

        ReservadaAte = reservadaAte;

        Raise(new MesaReservadaDomainEvent(Id, EstabelecimentoId, Numero.Valor, reservadaAte));

        return Result.Success();
    }

    public Result CancelarReserva(DateTimeOffset canceladaEm)
    {
        if (Status != StatusMesa.Reservada)
        {
            return Result.Failure(MesaErrors.NaoEstaReservada);
        }

        return Liberar(canceladaEm);
    }

    public Result AlterarLugares(int novaQuantidade)
    {
        if (novaQuantidade < 1)
        {
            return Result.Failure(MesaErrors.LugaresInvalidos);
        }

        Lugares = novaQuantidade;

        return Result.Success();
    }

    private Result TransicionarPara(StatusMesa destino)
    {
        if (!Status.PodeTransicionarPara(destino))
        {
            return Result.Failure(MesaErrors.TransicaoInvalida(Status, destino));
        }

        Status = destino;

        return Result.Success();
    }
}
