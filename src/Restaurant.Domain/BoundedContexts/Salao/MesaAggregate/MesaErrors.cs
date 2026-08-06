using Restaurant.Domain.BoundedContexts.Salao.MesaAggregate.Enumerations;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Domain.BoundedContexts.Salao.MesaAggregate;

public static class MesaErrors
{
    public static readonly Error NaoEncontrada = Error.NaoEncontrado(
        "Mesa.NaoEncontrada",
        "Mesa nao encontrada.");

    public static readonly Error LugaresInvalidos = Error.Validacao(
        "Mesa.LugaresInvalidos",
        "Mesa deve ter ao menos um lugar.");

    public static readonly Error ReservaNoPassado = Error.Validacao(
        "Mesa.ReservaNoPassado",
        "Reserva nao pode expirar no passado.");

    public static readonly Error NaoEstaReservada = Error.ConflitoDeEstado(
        "Mesa.NaoEstaReservada",
        "Mesa nao esta reservada.");

    public static Error TransicaoInvalida(StatusMesa origem, StatusMesa destino) => Error.ConflitoDeEstado(
        "Mesa.TransicaoInvalida",
        $"Nao e possivel mudar a mesa de {origem} para {destino}.");
}
