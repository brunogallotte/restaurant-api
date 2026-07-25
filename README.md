# Restaurant API

API de gestao de pedidos para restaurantes. Um estabelecimento cria sua conta, cadastra o cardapio e passa a operar o salao: o garcom abre um pedido para a mesa, lanca itens conforme o cliente pede, a cozinha avanca o preparo item a item e a conta e fechada com a taxa de servico aplicada. Ha tambem uma consulta de painel que devolve os pedidos em andamento com mesa, cliente, status, prioridade e tempo decorrido desde a abertura, pensada para alimentar um monitor de cozinha ou de salao.

O projeto e um exercicio de Domain Driven Design aplicado com rigor. O modelo de dominio concentra as regras de negocio e nao conhece banco de dados, HTTP nem framework de persistencia. As decisoes de modelagem seguem os blocos taticos descritos por Eric Evans e o tratamento de agregados e consistencia proposto por Vaughn Vernon.

## Arquitetura

Arquitetura hexagonal (ports and adapters). As camadas sao projetos separados e a regra de dependencia aponta sempre para dentro: nada que esteja em uma camada externa e visivel para uma camada interna.

```
Api  ->  Persistence  ->|
  |  ->  Infrastructure ->|-->  Application  -->  Domain  -->  (nada)
  |  ->  Application   ->|
```

| Projeto | Responsabilidade |
|---|---|
| `Restaurant.Domain` | Agregados, entidades, value objects, domain events, domain services e as portas. Nao referencia projeto algum. |
| `Restaurant.Application` | Casos de uso em CQRS: commands, queries, handlers e pipeline behaviors. |
| `Restaurant.Persistence` | Unico projeto que conhece EF Core: DbContext, mapeamentos, migrations, repositorios e o read side. |
| `Restaurant.Infrastructure` | Demais adapters: relogio, hashing, geracao de token, contexto de tenant. |
| `Restaurant.Api` | Composition root, endpoints e traducao de erro de dominio para ProblemDetails. |

`Persistence` e `Infrastructure` nunca referenciam um ao outro. Ambos sao adapters que implementam portas declaradas nas camadas de dentro, e a separacao mantem a dependencia de EF Core isolada em um unico projeto.

A regra de dependencia nao depende de disciplina: o projeto `Restaurant.ArchitectureTests` a verifica por reflexao a cada execucao da suite.

## Desenho estrategico

O sistema e dividido em bounded contexts, cada um com seu proprio modelo e sua propria linguagem.

| Contexto | Classificacao | Agregados |
|---|---|---|
| `Pedidos` | Core domain | `Pedido` (raiz) e `ItemPedido` (entidade filha) |
| `Cardapio` | Supporting | `Produto`, `Categoria` |
| `Salao` | Supporting | `Mesa` |
| `Contas` | Generic | `Estabelecimento` (tenant), `Funcionario` |

O context map define `Pedidos` como cliente de `Cardapio` em uma relacao customer/supplier. Quando um item entra no pedido, o agregado copia nome e preco do produto para um value object de snapshot em vez de guardar referencia viva. Se o preco mudar depois, o pedido ja emitido nao muda, porque o preco no momento do pedido pertence ao contexto de Pedidos. Referencias entre contextos sao sempre feitas por identificador, nunca por navegacao de objeto.

A ubiquitous language e mantida em portugues, que e a lingua dos especialistas do dominio. Os blocos tecnicos permanecem em ingles. O codigo fala `Pedido`, `Mesa`, `Comanda` e `TaxaDeServico`, e nao uma traducao aproximada desses termos.

## Blocos taticos

| Bloco | Aplicacao no projeto |
|---|---|
| Entity | `Pedido` e `ItemPedido`, com identidade substituta em `Guid` versao 7 e igualdade por identidade |
| Value Object | `Dinheiro`, `Cnpj`, `Quantidade`, `ProdutoDoPedido` e outros, imutaveis, com igualdade estrutural e construtor privado |
| Aggregate | `Pedido` e a fronteira de consistencia: nenhuma invariante atravessa seus limites, e `ItemPedido` so e alcancavel atraves da raiz |
| Domain Event | Doze eventos que registram fatos ocorridos, acumulados no agregado e publicados pela infraestrutura apos o commit |
| Domain Service | `PoliticaDePrioridade` para politica que nao pertence a um agregado, e portas como `IGeradorDeNumeroDePedido` para o que exige estado externo |
| Repository | Uma porta por agregado, declarada no dominio e implementada na persistencia |
| Factory | Metodos estaticos como `Pedido.Abrir` e `Dinheiro.Criar`, que retornam `Result` e impedem a construcao de estado invalido |

Duas decisoes merecem destaque por serem consequencia direta do modelo:

**Invariantes vivem no agregado.** Um pedido nao e confirmado sem itens, so fica pronto quando todos os itens ativos estao prontos, e um item que ja entrou em producao exige motivo para ser cancelado. Essas regras sao metodos de `Pedido`, nao validacoes espalhadas por handlers.

**Dado derivado nao e persistido.** `Subtotal`, `Total` e o valor da taxa de servico sao calculados a partir dos itens e nao possuem setter, o que torna impossivel o total divergir do conteudo do pedido. O tempo decorrido e a prioridade efetiva sao calculados na leitura a partir de `TimeProvider`, o que permite ao painel funcionar sem nenhuma rotina de atualizacao.

Falhas de negocio esperadas sao devolvidas como `Result` e fazem parte da assinatura dos metodos. Excecao fica reservada para estado impossivel.

## Stack

.NET 10 e C# 14, PostgreSQL 18 via Docker Compose, Entity Framework Core 10 com Npgsql, MediatR para despacho de commands e domain events, FluentValidation, xUnit v3 sobre Microsoft Testing Platform, NSubstitute e AwesomeAssertions.

## Executando

```bash
docker compose up -d
dotnet run --project src/Restaurant.Api
```

Migrations:

```bash
dotnet ef migrations add <Nome> -p src/Restaurant.Persistence -s src/Restaurant.Api
dotnet ef database update -p src/Restaurant.Persistence -s src/Restaurant.Api
```

## Commits

O repositorio segue Conventional Commits, validado por um hook em `.githooks/commit-msg`. Apos clonar, ative com:

```bash
git config core.hooksPath .githooks
```

## Testes

```bash
dotnet test
```

A suite cobre as invariantes do dominio nos caminhos de sucesso e de falha, a matriz completa de transicao de status, a diferenca entre igualdade estrutural de value object e igualdade por identidade de entidade, e a verificacao de que cada operacao levanta o domain event correto.

Os testes de arquitetura garantem que o dominio nao referencia projeto algum, que a camada de aplicacao nao conhece EF Core, que agregados nao expoem setter publico nem colecao mutavel, e que cada pasta contem exatamente o bloco tatico que anuncia.

## Documentacao

- [docs/estrutura.md](docs/estrutura.md): organizacao de pastas do dominio e criterio de cada uma
- [docs/modelagem.md](docs/modelagem.md): racional de modelagem, incluindo por que cada tipo e entidade ou value object, quando um domain service se justifica e como os domain events funcionam

## Referencias

- Eric Evans. *Domain-Driven Design: Tackling Complexity in the Heart of Software*
- Vaughn Vernon. *Implementing Domain-Driven Design*
- Alistair Cockburn. *Hexagonal Architecture (Ports and Adapters)*
