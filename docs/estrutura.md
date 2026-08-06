# Estrutura de pastas

Onde cada coisa mora e por quê. Se você está com um arquivo novo na mão, vá direto para [Onde ponho meu arquivo](#onde-ponho-meu-arquivo-novo) — ou, se ele é um caso de uso, para [a árvore de decisão da Application](#onde-ponho-meu-arquivo-novo-application).

A primeira metade deste documento é o `Restaurant.Domain`; a partir de [A Application herda o mesmo eixo](#a-application-herda-o-mesmo-eixo) é o `Restaurant.Application`.

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

## A Application herda o mesmo eixo

O Domain separa por **conhecimento**: genérico → compartilhado → contexto. A Application usa o mesmo eixo, trocando só os nomes pelo vocabulário da camada.

| Domain | Application | Critério |
|---|---|---|
| `BuildingBlocks/` | `Abstractions/` | zero negócio; copiaria para outro sistema sem alterar linha |
| `SharedKernel/` | `SharedKernel/` | conceito de aplicação usado por mais de um contexto |
| `BoundedContexts/X/` | `BoundedContexts/X/` | casos de uso de um contexto só |
| `<Nome>Aggregate/` — o agregado é a unidade | `<CasoDeUso>/` — o caso de uso é a unidade | vertical slice |

```
src/Restaurant.Application/
│
├── Abstractions/                        ← genérico: zero conhecimento de negócio
│   ├── Messaging/
│   │   ├── ICommandBase.cs              marcador que o UnitOfWorkBehavior discrimina
│   │   ├── ICommand.cs / ICommandOfT.cs IRequest<Result> e IRequest<Result<T>>
│   │   ├── IQuery.cs                    não é ICommandBase — por isso não commita
│   │   └── ICommandHandler.cs / ICommandHandlerOfT.cs / IQueryHandler.cs
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs           TimeProvider, nunca Stopwatch
│   │   ├── ValidationBehavior.cs        FluentValidation → Result, sem exception
│   │   └── UnitOfWorkBehavior.cs        commita só comando, só no sucesso
│   └── Results/
│       └── FailedResult.cs              constrói Result ou Result<T> de um TResponse genérico
│
├── SharedKernel/
│   └── Tenancy/
│       └── ITenantContext.cs            EstabelecimentoId + FuncionarioId da requisição
│
└── BoundedContexts/
    ├── Contas/       CadastrarEstabelecimento/ · AdmitirFuncionario/ · AlterarTaxaDeServico/
    ├── Cardapio/     Ports/ · Contracts/ · CriarCategoria/ · CadastrarProduto/
    │                 AlterarPrecoDoProduto/ · MarcarProdutoComoEsgotado/ · ObterCardapio/
    ├── Salao/        CadastrarMesa/ · ListarMesasLivres/ · EventHandlers/
    └── Pedidos/
        ├── Ports/           IPedidoQueries.cs
        ├── Contracts/       PedidoEmAndamento · PedidoLido · ItemLido
        ├── AbrirPedido/             AdicionarItemAoPedido/    AlterarQuantidadeDoItem/
        ├── CancelarItemDoPedido/    ConfirmarPedido/          IniciarPreparoDoPedido/
        ├── MarcarItemComoPronto/    MarcarPedidoComoPronto/   EntregarPedido/
        ├── FecharPedido/            CancelarPedido/           ElevarPrioridadeDoPedido/
        ├── ObterPainelDePedidos/    query + handler + PedidoNoPainel
        └── ObterPedidoPorId/        query + handler + PedidoDetalhado + ItemDoPedidoDetalhado
```

### Vertical slice, não `Commands/` + `Queries/`

Uma pasta por caso de uso, contendo request, handler, validator (quando existe) e os DTOs exclusivos daquele caso.

O argumento é literalmente o mesmo que justifica `PedidoAggregate/` acima: *em `Cardapio`, com `Produto` e `Categoria`, de quem é o VO `NomeDeProduto`?* Com `Commands/` + `Validators/` + `Dtos/`, a pergunta "qual validator pertence a qual comando?" volta a depender do nome do arquivo. Com `AbrirPedido/`, a pasta **é** o caso de uso — e vira teste bidirecional, como as pastas do Domain.

O preço são muitas pastas rasas (14 só em Pedidos). O que se compra é que um caso de uso é deletável apagando um diretório, e que nenhum arquivo fica órfão.

### O que cada pasta contém

| Pasta | Contém exatamente |
|---|---|
| `Abstractions/` | tipos que não referenciam `BoundedContexts` |
| `Abstractions/Behaviors/` | os `IPipelineBehavior<,>` — **e** nenhum behavior mora fora |
| `BoundedContexts/X/<CasoDeUso>/` | 1 request + 1 handler + ≤1 validator + DTOs exclusivos |
| `BoundedContexts/X/Ports/` | só interfaces (portas de leitura) |
| `BoundedContexts/X/Contracts/` | só `sealed record` sem comportamento |
| `BoundedContexts/X/EventHandlers/` | os `INotificationHandler<>` — **e** nenhum mora fora |

### Portas de leitura moram na Application, não no Domain

`IPedidoQueries` e `ICardapioQueries` ficam em `Application/BoundedContexts/X/Ports/`, e não junto de `IPedidoRepository` no Domain. Dois motivos:

1. **Porta pertence à camada mais interna que a consome.** Nenhum tipo do Domain chama `IPedidoQueries` — nem poderia, porque o read side existe justamente para *não* passar por agregado. Declará-la lá seria um contrato sem consumidor local, o oposto de DIP.
2. **Ela devolve DTO, não agregado.** Se morasse no Domain, `PedidoEmAndamento` teria de morar lá também — tipos com forma de tela dentro do modelo.

A regra do hexágono continua honrada: Application é "dentro" em relação a Persistence, que é quem implementa a porta. E o exemplo de ISP do `CLAUDE.md` (`IPedidoRepository` escrita ≠ `IPedidoQueries` leitura) segue valendo — ficou mais nítido ainda por serem camadas diferentes.

`ITenantContext` mora na Application pelo mesmo critério: nenhum agregado pergunta "em nome de quem eu estou agindo" — todos recebem `EstabelecimentoId` por parâmetro.

### Contrato fala primitivo

Request e response usam `Guid`, `string`, `decimal`, `int`, `DateTimeOffset` — nunca VO nem ID fortemente tipado. Mesma regra que os domain events já seguiam. O ganho é concreto: a Api nunca referencia `PedidoId` ou `Dinheiro`, e o OpenAPI não serializa `{"valor": ...}`.

As **portas** são a exceção e usam IDs tipados, porque são contrato interno com o adapter, que conhece o domínio.

---

## Onde ponho meu arquivo novo? (Application)

```
Meu tipo menciona algum conceito de negócio?
│
├── NÃO ──────────────────────────────► Abstractions/
│                                        └─ Messaging/ · Behaviors/ · Results/
│
└── SIM
    │
    ├── Serve mais de um contexto? ────► SharedKernel/
    │                                    └─ Tenancy/
    │
    └── É de um contexto só ───────────► BoundedContexts/<Contexto>/
        │
        ├── É um caso de uso? ─────────► <NomeDoCasoDeUso>/
        │                                └─ request + handler + validator + DTOs
        │
        ├── Reage a um fato? ──────────► EventHandlers/
        │
        ├── É contrato de leitura? ────► Ports/
        │
        └── É forma de dado lido? ─────► Contracts/
```

---

## As convenções da Application também são teste

`ConvencoesDaApplicationTests.cs` e `ConvencaoDePastasDaApplicationTests.cs` travam 17 regras. As que mais moldam o código:

| Teste | Regra |
|---|---|
| `Comandos_e_queries_sao_records_selados_publicos` | o contrato com a Api é `public sealed record` |
| `Handlers_sao_internos_e_selados` | ninguém fora da Application referencia handler; a Api fala com `ISender` |
| `Todo_request_tem_exatamente_um_handler` | zero órfão, zero duplicado |
| `Handlers_usam_as_abstracoes_da_Application` | nenhum `IRequestHandler` cru — obriga `ICommandHandler`/`IQueryHandler` |
| `Pasta_de_caso_de_uso_tem_exatamente_um_request_e_um_handler` | a pasta **é** o caso de uso |
| `Nome_da_pasta_e_o_nome_do_caso_de_uso` | último segmento do namespace = nome do request sem sufixo |
| `Handler_de_comando_nao_conhece_IUnitOfWork` | commit é do behavior; `EventHandlers/` é a exceção explícita |
| `Application_nao_declara_catalogo_de_Error` | o vocabulário de erro é do Domain |
| `Assinatura_de_um_contexto_so_menciona_Events_Ports_ou_Identifiers_de_outro` | a forma do acoplamento cruzado |

A última merece explicação, porque a primeira versão dela estava errada e o próprio teste mostrou isso.

`Contexto_nao_referencia_outro_contexto`, no Domain, é uma proibição **total** — no modelo, uma referência cruzada significa identidade e ciclo de vida compartilhados, e `Pedido` simplesmente não pode compilar contra `Produto`.

Na Application a proibição não pode ser total, porque **compor contextos é o trabalho dessa camada**. `AdicionarItemAoPedidoCommandHandler` precisa ler o `Produto` — é lendo que ele copia nome e preço para dentro do `ProdutoDoPedido`. Proibir isso seria proibir o snapshot, que é a lição central do context map.

O que fica travado é a **forma** do acoplamento, não a existência dele:

- a reação mora no contexto **que reage** (`Salao/EventHandlers/`), então a direção da seta está no caminho do arquivo;
- um contexto pode usar o agregado de outro **transitoriamente, através de uma porta**, para copiar dado;
- o que ele não pode é **expor** esse agregado no próprio contrato — nem receber por parâmetro, nem devolver, nem guardar em campo.

Por isso o teste varre a assinatura (`Reflexao.TiposNaAssinaturaDe`) e não o corpo do método.

### O limite da reflexão, declarado

`Reflexao.TiposReferenciadosPor` enxerga: tipo-base, interfaces, campos, propriedades, parâmetros e retornos de método, parâmetros de construtor e variáveis locais — incluindo as dos tipos gerados pelo compilador.

O que ela **não** enxerga: variável local dentro de método `async`. O compilador move o local para a state machine, e nem sempre como variável rastreável. Pegar isso exigiria varrer opcodes de IL, maquinaria que não se justifica aqui. As regras valem para a superfície de assinatura e para corpos síncronos, e é assim que devem ser lidas.

Todas as regras novas foram validadas por **mutação**. Quatro mutações não chegaram a rodar porque o compilador barrou a violação antes — sinal de que a regra já estava garantida mais cedo — e foram refeitas de forma que compilasse, para o teste ser exercitado de verdade. O método pagou: foi ele que revelou o erro da regra de acoplamento cruzado, e que o filtro de `Contracts/` deixava passar classe estática auxiliar, porque `static class` é `abstract sealed` em IL.

---

Para o racional de *modelagem* (por que X é value object, quando extrair domain service, como funcionam os domain events), veja [modelagem.md](modelagem.md).
