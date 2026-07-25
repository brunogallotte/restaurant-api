using Restaurant.Domain.Compartilhado;

namespace Restaurant.Domain.Abstractions;

public interface ITenantScoped
{
    EstabelecimentoId EstabelecimentoId { get; }
}
