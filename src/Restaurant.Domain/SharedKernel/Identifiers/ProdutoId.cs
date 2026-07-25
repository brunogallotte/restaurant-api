namespace Restaurant.Domain.SharedKernel.Identifiers;

public readonly record struct ProdutoId(Guid Valor)
{
    public static ProdutoId Novo() => new(Guid.CreateVersion7());
}
