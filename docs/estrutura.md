# Estrutura de pastas do Domain

Onde cada coisa mora e por quê. Se você está com um arquivo novo na mão, vá direto para [Onde ponho meu arquivo](#onde-ponho-meu-arquivo-novo).

---

## O que são "building blocks"

Termo do Eric Evans para os **padrões táticos** do DDD — a Parte II do livro azul se chama "The Building Blocks of Model-Driven Design". São as peças de vocabulário com que se monta qualquer modelo de domínio, independente do negócio: **Entity**, **Value Object**, **Aggregate**, **Repository**, **Factory**, **Domain Service**, **Domain Event**, **Module**.

A distinção que mais confunde:

| | O quê | Onde mora |
|---|---|---|
| **O padrão** | "existe uma coisa chamada Entity, que tem identidade e ciclo de vida" | `BuildingBlocks/Model/Entity.cs` |
| **A instância do padrão** | "`Pedido` é uma Entity" | `BoundedContexts/Pedidos/PedidoAggregate/Pedido.cs` |

`Pedido` **é** uma entidade — mas não fica em `BuildingBlocks/`. Lá fica a classe base `Entity<TId>`, que define *o que é ser uma entidade* neste projeto: igualdade por identidade, `Id` com setter privado, nada mais.

**`BuildingBlocks/` é a caixa de ferramentas. `BoundedContexts/` é o que foi construído com ela.**

Uma honestidade: nem tudo lá é building block do Evans. `SmartEnum` é padrão da comunidade .NET, e `Result`/`Error` é tratamento funcional de erro, que o Evans não aborda. A definição operacional aqui é mais larga que a do livro: **tipos-base genéricos, sem conhecimento de negócio, que sustentam os padrões táticos**. O critério é a ausência de negócio, não a filiação bibliográfica.

---

## A árvore

```
src/Restaurant.Domain/
│
├── BuildingBlocks/                      ← genérico: zero conhecimento de negócio
│   ├── Model/
│   │   ├── Entity.cs                    identidade substituta + igualdade por Id
│   │   ├── AggregateRoot.cs             Entity + coleção de domain events
│   │   ├── IAggregateRoot.cs            marcador da raiz
│   │   ├── ValueObject.cs               igualdade estrutural por componentes
│   │   └── SmartEnum.cs                 enum com comportamento
│   ├── Events/
│   │   └── IDomainEvent.cs              único ponto de contato com MediatR.Contracts
│   ├── Results/
│   │   ├── Result.cs / ResultOfT.cs     falha de negócio na assinatura
│   │   ├── Error.cs / ErrorType.cs      código + tipo, para virar ProblemDetails
│   │   └── DomainException.cs           só para estado impossível
│   └── Ports/
│       ├── IRepository.cs
│       └── IUnitOfWork.cs
│
├── SharedKernel/                        ← negócio compartilhado entre contextos
│   ├── Tenancy/
│   │   ├── ITenantScoped.cs             todo aggregate root implementa
│   │   └── ITenantRoot.cs               só Estabelecimento: ele *e* o tenant
│   ├── Identifiers/                     IDs de agregados de outros contextos
│   │   ├── EstabelecimentoId.cs         FuncionarioId.cs   MesaId.cs
│   │   └── ProdutoId.cs                 CategoriaId.cs
│   ├── ValueObjects/
│   │   ├── Dinheiro.cs                  Percentual.cs      Cnpj.cs
│   │   ├── Email.cs                     Telefone.cs        NomePessoa.cs
│   │   └── Endereco.cs                  Cep.cs
│   └── Enumerations/
│       └── Moeda.cs
│
└── BoundedContexts/                     ← negócio de um contexto só
    ├── Contas/                          Estabelecimento (tenant), Funcionario
    ├── Cardapio/                        Produto, Categoria
    ├── Salao/                           Mesa
    └── Pedidos/                         o core, detalhado abaixo
        ├── PedidoAggregate/
        │   ├── Pedido.cs                raiz do agregado
        │   ├── ItemPedido.cs            entidade filha
        │   ├── PedidoErrors.cs          erros das invariantes do agregado
        │   ├── Identifiers/             PedidoId, ItemPedidoId
        │   ├── Enumerations/            StatusPedido, StatusItemPedido, PrioridadePedido
        │   ├── ValueObjects/            NumeroPedido, ProdutoDoPedido, Quantidade,
        │   │                            Observacao, MotivoCancelamento, NomeCliente
        │   └── Events/                  12 arquivos, um por evento
        ├── Policies/
        │   └── PoliticaDePrioridade.cs
        └── Ports/
            ├── IPedidoRepository.cs
            └── IGeradorDeNumeroDePedido.cs
```

---

## Os três níveis de topo separam por *conhecimento*

| Pasta | O que sabe | Teste prático |
|---|---|---|
| `BuildingBlocks/` | **nada** de negócio | Copiaria isto para um sistema bancário sem alterar uma linha? |
| `SharedKernel/` | negócio **compartilhado** | É conceito de negócio usado por mais de um contexto? |
| `BoundedContexts/` | negócio **de um contexto** | É específico de Pedidos / Cardápio / Salão? |

A fronteira entre a primeira e a segunda é a que mais escorrega. Na primeira versão do projeto, `ITenantScoped` estava junto dos blocos genéricos e importava `EstabelecimentoId` — parecia plumbing, mas conhecia um conceito deste negócio. Mover para `SharedKernel/Tenancy/` restaurou a separação, e hoje um teste impede a regressão.

---

## Dentro do contexto, o agregado é a unidade

`PedidoAggregate/` agrupa a raiz, a entidade filha, os IDs, enums, VOs e eventos **daquele** agregado.

Isso responde a uma pergunta que a organização por tipo não responde: em `Cardapio`, com `Produto` e `Categoria`, de quem é o VO `NomeDeProduto`? Agrupando por agregado, a resposta está no caminho do arquivo.

`Policies/` e `Ports/` ficam **fora** do agregado, no nível do contexto, porque não pertencem a um agregado específico — `PoliticaDePrioridade` é política do contexto, e as portas são contratos que a infraestrutura implementa.

### Por que `PedidoAggregate/` e não `Pedido/`

`Aggregates/Pedido/` produziria o namespace `...Aggregates.Pedido` contendo a classe `Pedido` — classe com o mesmo nome do namespace que a contém. Gera ambiguidade de resolução de nome e a Microsoft desaconselha. É a mesma armadilha que evitamos chamando o tenant de `Estabelecimento` em vez de `Restaurant`.

---

## Convenções de nome e arquivo

**Idioma:** categoria técnica em inglês (`BuildingBlocks`, `ValueObjects`, `Events`, `Ports`), conceito de negócio em português (`Pedidos`, `PedidoAggregate`). Mesma regra do código — a Ubiquitous Language é a língua do especialista, o vocabulário técnico é o da plataforma.

**Um tipo público por arquivo.** Você acha `PedidoConfirmadoDomainEvent` pelo nome do arquivo, e o diff do git aponta exatamente qual evento mudou.

**Namespaces alinhados 1:1 com as pastas**, sem suprimir `IDE0130`. O efeito colateral é bom: `Pedido.cs` declara 10 `using`, e essa lista **é** a documentação de quais blocos ele toca — dá para ver de relance que ele usa `SharedKernel` e nenhum outro contexto.

**Erro mora com a regra que o produz.** Cada VO declara os próprios (`Dinheiro.Negativo`, `Cnpj.DigitoVerificadorInvalido`, `Quantidade.ForaDaFaixa`). `PedidoErrors` é a exceção e continua central porque `SemItens`, `ItensPendentes` e `TransicaoInvalida` são invariantes **do agregado** — não há um tipo dono natural.

---

## Onde ponho meu arquivo novo?

```
Meu tipo menciona algum conceito de negócio?
│
├── NÃO ──────────────────────────────► BuildingBlocks/
│                                        └─ Model/ · Events/ · Results/ · Ports/
│
└── SIM
    │
    ├── Serve mais de um contexto? ────► SharedKernel/
    │                                    └─ ValueObjects/ · Identifiers/ ·
    │                                       Enumerations/ · Tenancy/
    │
    └── É de um contexto só ───────────► BoundedContexts/<Contexto>/
        │
        ├── Pertence a um agregado? ───► <Nome>Aggregate/
        │                                ├─ raiz e entidades filhas: na pasta
        │                                └─ Identifiers/ · Enumerations/ ·
        │                                   ValueObjects/ · Events/
        │
        ├── É política do contexto? ───► Policies/
        │
        └── É contrato p/ a infra? ────► Ports/
```

---

## A convenção é teste, não disciplina

`tests/Restaurant.ArchitectureTests/ConvencaoDePastasTests.cs` trava tudo acima por reflexão:

| Teste | Regra |
|---|---|
| `BuildingBlocks_nao_conhece_negocio` | nenhum tipo em `BuildingBlocks/` referencia `SharedKernel` ou `BoundedContexts` |
| `Contexto_nao_referencia_outro_contexto` | tipos de `BoundedContexts/X/` não tocam `BoundedContexts/Y/` |
| `Pasta_Events_contem_exatamente_os_domain_events` | todo tipo em `Events/` implementa `IDomainEvent` — **e** nenhum evento mora fora |
| `Pasta_ValueObjects_contem_exatamente_os_value_objects` | todo tipo em `ValueObjects/` herda `ValueObject` — **e** nenhum VO mora fora |
| `Pasta_Identifiers_contem_exatamente_os_ids_fortemente_tipados` | `readonly record struct` — **e** nenhum ID mora fora |
| `Pasta_Enumerations_contem_exatamente_os_smart_enums` | herda `SmartEnum<>` — **e** nenhum smart enum mora fora |
| `Pasta_Ports_so_tem_interface` | só interfaces |

O **"e nenhum X mora fora"** é o que dá valor. Sem a direção inversa, criar um VO no lugar errado passaria despercebido — o teste só olharia a pasta certa. Com ela, a pasta é a **definição** do que ela contém, não uma sugestão.

Cada teste foi validado por **mutação**: quebrar a regra de propósito e confirmar que ele reprova. Vale o método — o primeiro deles passava vacuamente, porque a varredura filtrava só classes concretas e a violação plantada estava numa *interface*. Teste de arquitetura que nunca se viu falhar não prova nada.

---

## Como escala

Os outros três contextos seguiram o mesmo formato, sem inventar nada:

```
BoundedContexts/Cardapio/
├── ProdutoAggregate/       Produto + VOs, DisponibilidadeProduto, 6 eventos
├── CategoriaAggregate/     Categoria + NomeDeCategoria, 3 eventos
└── Ports/                  IProdutoRepository, ICategoriaRepository,
                            IVerificadorDeNomeUnicoDeProduto

BoundedContexts/Salao/
├── MesaAggregate/          Mesa + NumeroDaMesa, StatusMesa, 4 eventos
└── Ports/                  IMesaRepository

BoundedContexts/Contas/
├── EstabelecimentoAggregate/   Estabelecimento (ITenantRoot) + NomeFantasia, 3 eventos
├── FuncionarioAggregate/       Funcionario + Cargo, 3 eventos
└── Ports/                      IEstabelecimentoRepository, IFuncionarioRepository,
                                IVerificadorDeCnpjUnico
```

Os sete testes de convenção **não precisaram de uma linha de alteração** para absorver os três contextos novos, que é exatamente o que se espera deles.

Uma exceção nasceu junto: `Estabelecimento` **é** o tenant, então não faz sentido implementar `ITenantScoped` apontando para si mesmo. Em vez de esconder isso numa lista de exclusão no teste, criamos o marcador `ITenantRoot`, e a regra virou "todo agregado é `ITenantScoped` **ou** `ITenantRoot`". A exceção fica documentada no tipo.

Quando um contexto crescer a ponto de justificar isolamento de compilação, ele vira projeto próprio (`Restaurant.Pedidos.Domain`) — a estrutura interna não muda, só sobe um nível. Hoje contextos são pastas porque 8 projetos são gerenciáveis e 20 não.

---

Para o racional de *modelagem* (por que X é value object, quando extrair domain service, como funcionam os domain events), veja [modelagem.md](modelagem.md).
