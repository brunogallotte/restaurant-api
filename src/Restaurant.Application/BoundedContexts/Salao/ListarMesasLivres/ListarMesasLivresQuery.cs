using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.BoundedContexts.Salao.Contracts;

namespace Restaurant.Application.BoundedContexts.Salao.ListarMesasLivres;

public sealed record ListarMesasLivresQuery(int LugaresMinimos) : IQuery<IReadOnlyList<MesaLivre>>;
