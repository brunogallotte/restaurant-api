namespace Restaurant.Domain.SharedKernel.Identifiers;

public readonly record struct FuncionarioId(Guid Valor)
{
    public static FuncionarioId Novo() => new(Guid.CreateVersion7());
}
