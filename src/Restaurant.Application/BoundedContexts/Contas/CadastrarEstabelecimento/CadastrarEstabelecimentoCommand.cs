using Restaurant.Application.Abstractions.Messaging;

namespace Restaurant.Application.BoundedContexts.Contas.CadastrarEstabelecimento;

public sealed record CadastrarEstabelecimentoCommand(
    string NomeFantasia,
    string Cnpj,
    string Email,
    string Telefone,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    decimal TaxaDeServico) : ICommand<Guid>;
