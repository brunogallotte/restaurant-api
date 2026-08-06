using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Pedidos.MarcarItemComoPronto;

public sealed record MarcarItemComoProntoCommand(Guid PedidoId, Guid ItemId) : ICommand;
