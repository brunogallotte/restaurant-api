namespace Restaurant.Domain.Compartilhado;

public readonly record struct EstabelecimentoId(Guid Valor)
{
    public static EstabelecimentoId Novo() => new(Guid.CreateVersion7());
}

public readonly record struct FuncionarioId(Guid Valor)
{
    public static FuncionarioId Novo() => new(Guid.CreateVersion7());
}

public readonly record struct MesaId(Guid Valor)
{
    public static MesaId Novo() => new(Guid.CreateVersion7());
}

public readonly record struct ProdutoId(Guid Valor)
{
    public static ProdutoId Novo() => new(Guid.CreateVersion7());
}

public readonly record struct CategoriaId(Guid Valor)
{
    public static CategoriaId Novo() => new(Guid.CreateVersion7());
}
