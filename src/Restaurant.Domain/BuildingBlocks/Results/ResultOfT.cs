namespace Restaurant.Domain.BuildingBlocks.Results;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool sucesso, Error error) : base(sucesso, error) => _value = value;

    public TValue Value => Sucesso
        ? _value!
        : throw new DomainException("Nao se le o valor de um Result que falhou.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public Result<TDestino> Map<TDestino>(Func<TValue, TDestino> projecao) =>
        Sucesso ? Success(projecao(Value)) : Failure<TDestino>(Error);
}
