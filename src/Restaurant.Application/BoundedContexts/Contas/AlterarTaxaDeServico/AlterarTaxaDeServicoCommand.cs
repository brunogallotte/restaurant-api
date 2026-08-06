using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Contas.AlterarTaxaDeServico;

public sealed record AlterarTaxaDeServicoCommand(decimal TaxaDeServico) : ICommand;
