namespace Restaurant.Application.BoundedContexts.Salao.Contracts;

public sealed record MesaLivre(Guid MesaId, string Numero, int Lugares);
