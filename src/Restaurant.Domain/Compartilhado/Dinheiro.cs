using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public sealed class Dinheiro : ValueObject
{
    private Dinheiro(decimal valor, Moeda moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public decimal Valor { get; }

    public Moeda Moeda { get; }

    public bool EstaZerado => Valor == 0m;

    public static Dinheiro Zero(Moeda moeda) => new(0m, moeda);

    public static Dinheiro ZeroEmReal() => Zero(Moeda.Real);

    public static Result<Dinheiro> Criar(decimal valor, Moeda moeda)
    {
        if (valor < 0m)
        {
            return Result.Failure<Dinheiro>(CompartilhadoErrors.DinheiroNegativo);
        }

        return new Dinheiro(Arredondar(valor), moeda);
    }

    public static Result<Dinheiro> CriarEmReal(decimal valor) => Criar(valor, Moeda.Real);

    public Result<Dinheiro> Somar(Dinheiro outro)
    {
        if (!MesmaMoedaQue(outro))
        {
            return Result.Failure<Dinheiro>(CompartilhadoErrors.DinheiroMoedasDiferentes);
        }

        return new Dinheiro(Arredondar(Valor + outro.Valor), Moeda);
    }

    public Result<Dinheiro> Subtrair(Dinheiro outro)
    {
        if (!MesmaMoedaQue(outro))
        {
            return Result.Failure<Dinheiro>(CompartilhadoErrors.DinheiroMoedasDiferentes);
        }

        return Criar(Valor - outro.Valor, Moeda);
    }

    public Dinheiro MultiplicarPor(int fator) => new(Arredondar(Valor * fator), Moeda);

    public Dinheiro AplicarPercentual(Percentual percentual) =>
        new(Arredondar(Valor * percentual.ComoFracao), Moeda);

    public bool MesmaMoedaQue(Dinheiro outro) => Moeda == outro.Moeda;

    public override string ToString() => $"{Moeda.Simbolo} {Valor:N2}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Moeda;
    }

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.ToEven);
}
