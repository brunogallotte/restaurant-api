using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Domain.BoundedContexts.Contas.Ports;

public interface IVerificadorDeCnpjUnico
{
    Task<bool> JaCadastradoAsync(Cnpj cnpj, CancellationToken cancellationToken = default);
}
