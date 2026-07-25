using System.Collections.Frozen;
using System.Reflection;

namespace Restaurant.Domain.Abstractions;

public abstract class SmartEnum<TEnum> : IEquatable<SmartEnum<TEnum>>
    where TEnum : SmartEnum<TEnum>
{
    private static readonly Lazy<FrozenDictionary<int, TEnum>> PorValor = new(CarregarPorValor);
    private static readonly Lazy<FrozenDictionary<string, TEnum>> PorNome = new(CarregarPorNome);

    protected SmartEnum(int valor, string nome)
    {
        Valor = valor;
        Nome = nome;
    }

    public int Valor { get; }

    public string Nome { get; }

    public static IReadOnlyCollection<TEnum> Todos => PorValor.Value.Values;

    public static TEnum DeValor(int valor) => PorValor.Value.TryGetValue(valor, out var encontrado)
        ? encontrado
        : throw new DomainException($"Valor {valor} nao corresponde a nenhum {typeof(TEnum).Name}.");

    public static TEnum DeNome(string nome) => PorNome.Value.TryGetValue(nome, out var encontrado)
        ? encontrado
        : throw new DomainException($"Nome '{nome}' nao corresponde a nenhum {typeof(TEnum).Name}.");

    public bool Equals(SmartEnum<TEnum>? other) =>
        other is not null && GetType() == other.GetType() && Valor == other.Valor;

    public override bool Equals(object? obj) => obj is SmartEnum<TEnum> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(typeof(TEnum), Valor);

    public override string ToString() => Nome;

    public static bool operator ==(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(SmartEnum<TEnum>? left, SmartEnum<TEnum>? right) => !(left == right);

    private static FrozenDictionary<int, TEnum> CarregarPorValor() =>
        Declarados().ToFrozenDictionary(item => item.Valor);

    private static FrozenDictionary<string, TEnum> CarregarPorNome() =>
        Declarados().ToFrozenDictionary(item => item.Nome, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<TEnum> Declarados() =>
        typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TEnum))
            .Select(field => (TEnum)field.GetValue(null)!);
}
