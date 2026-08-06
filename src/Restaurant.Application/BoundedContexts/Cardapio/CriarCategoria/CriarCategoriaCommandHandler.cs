using Restaurant.Application.Abstractions.Messaging;
using Restaurant.Application.SharedKernel.Tenancy;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate;
using Restaurant.Domain.BoundedContexts.Cardapio.CategoriaAggregate.ValueObjects;
using Restaurant.Domain.BoundedContexts.Cardapio.Ports;
using Restaurant.Domain.BuildingBlocks.Results;

namespace Restaurant.Application.BoundedContexts.Cardapio.CriarCategoria;

internal sealed class CriarCategoriaCommandHandler(
    ICategoriaRepository categorias,
    ITenantContext tenant) : ICommandHandler<CriarCategoriaCommand, Guid>
{
    public Task<Result<Guid>> Handle(CriarCategoriaCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Criar(command));

    private Result<Guid> Criar(CriarCategoriaCommand command)
    {
        var nome = NomeDeCategoria.Criar(command.Nome);

        if (nome.Falhou)
        {
            return Result.Failure<Guid>(nome.Error);
        }

        var criacao = Categoria.Criar(tenant.EstabelecimentoId, nome.Value, command.Ordem);

        if (criacao.Falhou)
        {
            return Result.Failure<Guid>(criacao.Error);
        }

        categorias.Adicionar(criacao.Value);

        return criacao.Value.Id.Valor;
    }
}
