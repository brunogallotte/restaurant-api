using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Contas.AdmitirFuncionario;

public sealed record AdmitirFuncionarioCommand(string Nome, string Email, string Cargo) : ICommand<Guid>;
