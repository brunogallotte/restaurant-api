namespace Restaurant.Domain.SharedKernel.Identifiers;

public readonly record struct EstabelecimentoId(Guid Valor)
{
    public static EstabelecimentoId Novo() => new(Guid.CreateVersion7());
}
