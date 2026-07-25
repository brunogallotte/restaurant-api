namespace Restaurant.Domain.SharedKernel.Identifiers;

public readonly record struct CategoriaId(Guid Valor)
{
    public static CategoriaId Novo() => new(Guid.CreateVersion7());
}
