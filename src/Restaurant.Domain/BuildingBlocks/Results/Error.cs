namespace Restaurant.Domain.BuildingBlocks.Results;

public sealed record Error(string Codigo, string Mensagem, ErrorType Tipo)
{
    public static readonly Error Nenhum = new(string.Empty, string.Empty, ErrorType.Validacao);

    public static Error Validacao(string codigo, string mensagem) =>
        new(codigo, mensagem, ErrorType.Validacao);

    public static Error ConflitoDeEstado(string codigo, string mensagem) =>
        new(codigo, mensagem, ErrorType.ConflitoDeEstado);

    public static Error NaoEncontrado(string codigo, string mensagem) =>
        new(codigo, mensagem, ErrorType.NaoEncontrado);

    public override string ToString() => $"{Codigo}: {Mensagem}";
}
