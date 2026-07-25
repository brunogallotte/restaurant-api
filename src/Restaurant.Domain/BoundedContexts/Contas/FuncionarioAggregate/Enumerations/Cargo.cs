using Restaurant.Domain.BuildingBlocks.Model;

namespace Restaurant.Domain.BoundedContexts.Contas.FuncionarioAggregate.Enumerations;

public sealed class Cargo : SmartEnum<Cargo>
{
    public static readonly Cargo Proprietario = new(
        1,
        nameof(Proprietario),
        gerenciaCardapio: true,
        gerenciaEquipe: true,
        registraPedido: true,
        avancaPreparo: true,
        fechaConta: true);

    public static readonly Cargo Gerente = new(
        2,
        nameof(Gerente),
        gerenciaCardapio: true,
        gerenciaEquipe: true,
        registraPedido: true,
        avancaPreparo: true,
        fechaConta: true);

    public static readonly Cargo Garcom = new(
        3,
        nameof(Garcom),
        gerenciaCardapio: false,
        gerenciaEquipe: false,
        registraPedido: true,
        avancaPreparo: false,
        fechaConta: true);

    public static readonly Cargo Cozinha = new(
        4,
        nameof(Cozinha),
        gerenciaCardapio: false,
        gerenciaEquipe: false,
        registraPedido: false,
        avancaPreparo: true,
        fechaConta: false);

    private Cargo(
        int valor,
        string nome,
        bool gerenciaCardapio,
        bool gerenciaEquipe,
        bool registraPedido,
        bool avancaPreparo,
        bool fechaConta) : base(valor, nome)
    {
        PodeGerenciarCardapio = gerenciaCardapio;
        PodeGerenciarEquipe = gerenciaEquipe;
        PodeRegistrarPedido = registraPedido;
        PodeAvancarPreparo = avancaPreparo;
        PodeFecharConta = fechaConta;
    }

    public bool PodeGerenciarCardapio { get; }

    public bool PodeGerenciarEquipe { get; }

    public bool PodeRegistrarPedido { get; }

    public bool PodeAvancarPreparo { get; }

    public bool PodeFecharConta { get; }
}
