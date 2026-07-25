using Restaurant.Domain.Abstractions;

namespace Restaurant.Domain.Compartilhado;

public static class CompartilhadoErrors
{
    public static readonly Error DinheiroNegativo = Error.Validacao(
        "Dinheiro.Negativo",
        "Valor monetario nao pode ser negativo.");

    public static readonly Error DinheiroMoedasDiferentes = Error.Validacao(
        "Dinheiro.MoedasDiferentes",
        "Nao e possivel operar valores em moedas diferentes.");

    public static readonly Error PercentualForaDaFaixa = Error.Validacao(
        "Percentual.ForaDaFaixa",
        "Percentual deve estar entre 0 e 100.");

    public static readonly Error EmailVazio = Error.Validacao(
        "Email.Vazio",
        "E-mail e obrigatorio.");

    public static readonly Error EmailFormatoInvalido = Error.Validacao(
        "Email.FormatoInvalido",
        "E-mail em formato invalido.");

    public static readonly Error CnpjFormatoInvalido = Error.Validacao(
        "Cnpj.FormatoInvalido",
        "CNPJ deve conter 14 digitos.");

    public static readonly Error CnpjDigitoVerificadorInvalido = Error.Validacao(
        "Cnpj.DigitoVerificadorInvalido",
        "CNPJ com digito verificador invalido.");

    public static readonly Error TelefoneFormatoInvalido = Error.Validacao(
        "Telefone.FormatoInvalido",
        "Telefone deve conter 10 ou 11 digitos com DDD.");

    public static readonly Error NomePessoaVazio = Error.Validacao(
        "NomePessoa.Vazio",
        "Nome e obrigatorio.");

    public static readonly Error NomePessoaMuitoLongo = Error.Validacao(
        "NomePessoa.MuitoLongo",
        $"Nome nao pode exceder {NomePessoa.TamanhoMaximo} caracteres.");
}
