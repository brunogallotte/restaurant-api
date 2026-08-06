using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate;
using Restaurant.Domain.BoundedContexts.Contas.EstabelecimentoAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Contas.Ports;
using Restaurant.Domain.BuildingBlocks.Results;
using Restaurant.Domain.SharedKernel.ValueObjects;

namespace Restaurant.Application.BoundedContexts.Contas.CadastrarEstabelecimento;

internal sealed class CadastrarEstabelecimentoCommandHandler(
    IEstabelecimentoRepository estabelecimentos,
    IVerificadorDeCnpjUnico verificadorDeCnpj,
    TimeProvider relogio) : ICommandHandler<CadastrarEstabelecimentoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CadastrarEstabelecimentoCommand command,
        CancellationToken cancellationToken)
    {
        var nomeFantasia = NomeFantasia.Criar(command.NomeFantasia);
        var cnpj = Cnpj.Criar(command.Cnpj);
        var email = Email.Criar(command.Email);
        var telefone = Telefone.Criar(command.Telefone);
        var endereco = CriarEndereco(command);
        var taxaDeServico = Percentual.Criar(command.TaxaDeServico);

        var entradas = Result.PrimeiraFalha(nomeFantasia, cnpj, email, telefone, endereco, taxaDeServico);

        if (entradas.Falhou)
        {
            return Result.Failure<Guid>(entradas.Error);
        }

        if (await verificadorDeCnpj.JaCadastradoAsync(cnpj.Value, cancellationToken))
        {
            return Result.Failure<Guid>(EstabelecimentoErrors.CnpjJaCadastrado);
        }

        var cadastro = Estabelecimento.Cadastrar(
            nomeFantasia.Value,
            cnpj.Value,
            email.Value,
            telefone.Value,
            endereco.Value,
            taxaDeServico.Value,
            relogio.GetUtcNow());

        if (cadastro.Falhou)
        {
            return Result.Failure<Guid>(cadastro.Error);
        }

        estabelecimentos.Adicionar(cadastro.Value);

        return cadastro.Value.Id.Valor;
    }

    private static Result<Endereco> CriarEndereco(CadastrarEstabelecimentoCommand command)
    {
        var cep = Cep.Criar(command.Cep);

        if (cep.Falhou)
        {
            return Result.Failure<Endereco>(cep.Error);
        }

        return Endereco.Criar(
            command.Logradouro,
            command.Numero,
            command.Complemento,
            command.Bairro,
            command.Cidade,
            command.Uf,
            cep.Value);
    }
}
