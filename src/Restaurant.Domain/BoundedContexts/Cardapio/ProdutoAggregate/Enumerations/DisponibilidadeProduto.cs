using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.Domain.BoundedContexts.Cardapio.ProdutoAggregate.Enumerations;

public sealed class DisponibilidadeProduto : SmartEnum<DisponibilidadeProduto>
{
    public static readonly DisponibilidadeProduto Disponivel = new(1, nameof(Disponivel));
    public static readonly DisponibilidadeProduto Esgotado = new(2, nameof(Esgotado));

    private DisponibilidadeProduto(int valor, string nome) : base(valor, nome)
    {
    }

    public bool PodeSerPedido => this == Disponivel;
}
