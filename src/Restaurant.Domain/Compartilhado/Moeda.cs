using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed class Moeda : SmartEnum<Moeda>
{
    public static readonly Moeda Real = new(1, "BRL", "R$");
    public static readonly Moeda Dolar = new(2, "USD", "US$");
    public static readonly Moeda Euro = new(3, "EUR", "€");

    private Moeda(int valor, string nome, string simbolo) : base(valor, nome) => Simbolo = simbolo;

    public string Simbolo { get; }
}
