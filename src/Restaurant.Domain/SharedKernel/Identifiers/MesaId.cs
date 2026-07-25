namespace Restaurant.Domain.SharedKernel.Identifiers;

public readonly record struct MesaId(Guid Valor)
{
    public static MesaId Novo() => new(Guid.CreateVersion7());
}
