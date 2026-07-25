namespace Restaurant.Domain.Pedidos.Services;

public sealed class PoliticaDePrioridade
{
    public static readonly PoliticaDePrioridade Padrao = new(
        TimeSpan.FromMinutes(20),
        TimeSpan.FromMinutes(35));

    public PoliticaDePrioridade(TimeSpan limiteParaAlta, TimeSpan limiteParaUrgente)
    {
        if (limiteParaUrgente <= limiteParaAlta)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limiteParaUrgente),
                "Limite para urgente deve ser maior que o limite para alta.");
        }

        LimiteParaAlta = limiteParaAlta;
        LimiteParaUrgente = limiteParaUrgente;
    }

    public TimeSpan LimiteParaAlta { get; }

    public TimeSpan LimiteParaUrgente { get; }

    public PrioridadePedido Calcular(PrioridadePedido prioridadeManual, TimeSpan decorrido, StatusPedido status)
    {
        if (status.EhFinal)
        {
            return prioridadeManual;
        }

        return PrioridadePedido.MaisAlta(prioridadeManual, DerivarDoTempo(decorrido));
    }

    private PrioridadePedido DerivarDoTempo(TimeSpan decorrido)
    {
        if (decorrido >= LimiteParaUrgente)
        {
            return PrioridadePedido.Urgente;
        }

        if (decorrido >= LimiteParaAlta)
        {
            return PrioridadePedido.Alta;
        }

        return PrioridadePedido.Normal;
    }
}
