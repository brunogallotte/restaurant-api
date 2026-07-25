# Racional de modelagem

Por que cada coisa é o que é. Direto ao ponto.

---

## 1. Entidade vs Value Object

O critério não é "tem muitos campos" nem "é tabela no banco". São duas perguntas:

1. **Identidade importa?** Se duas instâncias com os mesmos dados são a *mesma coisa* → VO. Se são *coisas diferentes* → entidade.
2. **Muda ao longo do tempo?** Se você troca em vez de alterar → VO. Se acompanha um ciclo de vida → entidade.

### É entidade

| Tipo | Por quê |
|---|---|
| `Pedido` | Dois pedidos com mesma mesa, mesmo cliente e mesmos itens são **pedidos diferentes**. Tem ciclo de vida (`Aberto → … → Fechado`) e continuidade: é "o pedido da mesa 5" mesmo depois de mudar todos os itens. |
| `ItemPedido` | Duas linhas de "2× Picanha" na mesma comanda são **itens distintos** — a cozinha prepara dois pratos, um pode ficar pronto e o outro queimar. Cada um tem status próprio. Sem identidade, você não consegue dizer *qual* cancelar. |
| `Mesa`, `Produto`, `Estabelecimento`, `Funcionario` | Todos rastreados individualmente e mutáveis no tempo. |

### É value object

| Tipo | Por quê |
|---|---|
| `Dinheiro` | R$ 25,00 é R$ 25,00. Não existe "aquele R$ 25 específico". Imutável: somar produz um **novo** `Dinheiro`. |
| `Quantidade` | 3 é 3. Encapsula a invariante 1..99 num só lugar em vez de espalhar `if (qtd < 1)` por handlers. |
| `ProdutoDoPedido` | **O caso mais interessante.** É um *snapshot* de `ProdutoId` + `Nome` + `PrecoUnitario`. Não é o `Produto` — é o que o produto *era* quando entrou no pedido. Se o preço subir amanhã, o pedido de hoje não muda. Não tem identidade própria: dois itens do mesmo produto ao mesmo preço carregam snapshots iguais e isso está correto. |
| `NumeroPedido` | `20260725-0042`. Comportamento (formatar, reconstituir) junto do dado, em vez de string solta. |
| `Observacao`, `MotivoCancelamento`, `NomeCliente` | Strings com regra. Uma `string` crua não impede 10.000 caracteres; o VO impede — e a regra fica num lugar. |
| `Cnpj` | Melhor exemplo de "VO como guardião de invariante": valida os dois dígitos verificadores e rejeita dígitos repetidos. Um `Cnpj` que existe é um CNPJ válido — não há como construir um inválido. Também normaliza: `11.222.333/0001-81` e `11222333000181` são **iguais**. |
| `Percentual`, `Email`, `Telefone`, `NomePessoa` | Mesma ideia: faixa/formato garantidos na construção. |

### Casos limítrofes e como decidimos

- **`Moeda`** poderia ser VO, mas é um conjunto fechado e conhecido → **smart enum** (singleton), com `Simbolo` pendurado. Comparação por referência funciona e não há alocação por uso.
- **`Categoria`** virou **agregado próprio**, não VO dentro de `Produto`: tem nome editável e ordem de exibição administrados independentemente. Se fosse só um rótulo, seria VO.
- **`TempoDecorrido` / `PrioridadeEfetiva`** não são nem entidade nem VO persistido — são **derivados** (ver §6).

---

## 2. Identidade substituta e o `Entity<TId>` base

`Entity<TId>` usa **chave artificial**, nunca chave natural. Motivo: chave natural muda. O número do pedido parece estável até o dia em que o restaurante quer reiniciar a numeração, ou opera duas unidades. Identidade substituta não tem opinião sobre o negócio, então o negócio pode mudar sem migração de FK.

Em cima disso, **strongly-typed IDs**:

```csharp
public readonly record struct PedidoId(Guid Valor)
{
    public static PedidoId Novo() => new(Guid.CreateVersion7());
}
```

- `readonly record struct` → igualdade estrutural grátis, zero alocação no heap.
- O compilador impede passar um `MesaId` onde se espera um `PedidoId`. Com `Guid` cru, isso compila e falha em produção.
- `Guid.CreateVersion7()` (.NET 9+) embute um timestamp de milissegundos nos 48 bits altos. Ordenado como o Postgres ordena `uuid` (bytes big-endian), IDs gerados ao longo do tempo ficam **próximos no índice** — inserções vão para o fim do B-tree em vez de espalhar páginas como um v4 faz. **Caveat honesto:** dentro do mesmo milissegundo o .NET preenche o resto com bits aleatórios, sem contador monotônico. A garantia é o prefixo de timestamp não retroceder, não ordem total. É isso que `Prefixo_de_timestamp_do_guid_v7_nunca_retrocede` testa.

**Identidade local vs global:** `PedidoId` é globalmente único e tem repositório. `ItemPedidoId` só precisa ser único *dentro* do pedido — o item não tem repositório e não é alcançável fora da raiz. Essa distinção é o que define a fronteira do agregado.

**Igualdade — a diferença que os testes travam:**

| | Base da igualdade |
|---|---|
| `Entity<TId>` | `Id` + tipo concreto. Dois `Pedido` com dados idênticos e IDs diferentes **não** são iguais. |
| `ValueObject` | *todos* os componentes (`GetEqualityComponents()`). Dois `Dinheiro` de R$10 **são** iguais. |
| `SmartEnum<T>` | `Valor` + tipo, e as instâncias são singletons (`DeNome("Aberto")` é `BeSameAs(Aberto)`). |

---

## 3. Onde a lógica mora: agregado vs domain service

**Default é método no agregado.** Domain service é exceção, justificada por um destes três motivos:

### Motivo 1 — é política, não regra do agregado: `PoliticaDePrioridade`

```csharp
public PrioridadePedido Calcular(PrioridadePedido prioridadeManual, TimeSpan decorrido, StatusPedido status)
```

Função **pura**, sem dependência. Extraída porque:
- Os limiares (20 min → Alta, 35 min → Urgente) são configuração de negócio que varia por estabelecimento, não uma verdade do `Pedido`.
- É consumida pela **escrita** e pela **leitura** (o painel calcula prioridade efetiva sem carregar o agregado). Se estivesse dentro do `Pedido`, o read side seria obrigado a hidratar o agregado só para exibir uma cor na tela.
- Sendo pura, testa-se com uma `[Theory]` de seis linhas.

### Motivo 2 — precisa de estado externo: `IGeradorDeNumeroDePedido`

Numeração sequencial por estabelecimento/dia exige consultar o que já foi emitido. Um agregado **não pode** — ele só conhece a si mesmo. Então: **porta declarada no domínio**, adapter em `Persistence` (sequence do Postgres). O domínio expressa a necessidade sem saber como é satisfeita.

### Motivo 3 — invariante que atravessa agregados: `IVerificadorDeNomeUnicoDeProduto`

"Não pode haver dois produtos com o mesmo nome neste estabelecimento" não é verificável por um `Produto` — ele não vê os irmãos. Também porta no domínio, adapter na persistência.

### O que **não** justifica domain service

Tudo que o agregado consegue decidir sozinho. `Confirmar`, `AdicionarItem`, `CancelarItem`, cálculo de `Subtotal`/`Total` — tudo método de `Pedido`, porque toda a informação necessária está dentro da fronteira. Extrair isso para um `PedidoService` produziria exatamente o modelo anêmico que o exercício quer evitar.

### O agregado como fronteira de consistência

`Pedido` + `ItemPedido` são **um** agregado: nenhuma invariante cruza a fronteira. "Pedido só fica pronto quando todos os itens estão prontos" é verificável dentro de `Pedido`, por isso os itens estão dentro. `Mesa` é agregado **separado**, referenciado só por `MesaId` — a ocupação da mesa é eventualmente consistente com o pedido, via domain event. Se `Mesa` estivesse dentro de `Pedido`, dois garçons em mesas diferentes competiriam pelo mesmo lock.

---

## 4. Domain events: estrutura e motivo

### Como funciona

`AggregateRoot<TId>` acumula eventos numa lista privada; só a raiz pode levantar:

```csharp
private readonly List<IDomainEvent> _domainEvents = [];
public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
public void ClearDomainEvents() => _domainEvents.Clear();
```

O agregado **acumula e não despacha**. O despacho é responsabilidade da infraestrutura: um `SaveChangesInterceptor` em `Persistence` colhe os eventos antes do save e publica via MediatR **após** o commit. Duas razões: (1) o domínio não deve conhecer o mediator nem a transação; (2) publicar antes do commit vazaria efeitos de uma transação que pode falhar.

### Por que eventos e não chamada direta

Sem eventos, `Pedido.Fechar()` precisaria chamar `Mesa.Liberar()` — o que exige carregar outro agregado dentro de um método de domínio, acoplar os dois contextos, e escrever dois agregados na mesma transação. Com evento, `Pedido` só **anuncia o fato** (`PedidoFechadoDomainEvent`) e quem se interessa reage. `Pedidos` não conhece `Salao`.

### Convenções e por quê

| Convenção | Motivo |
|---|---|
| `sealed record` | evento é um fato imutável no passado; `record` dá igualdade estrutural e desconstrução de graça |
| Nome no passado + sufixo `DomainEvent` | `PedidoConfirmadoDomainEvent`, não `ConfirmarPedido`. Fato ocorrido, não comando |
| Carrega **IDs e valores primitivos**, nunca o agregado | se carregasse `Pedido`, o handler poderia mutá-lo fora da fronteira, e o evento deixaria de ser serializável para um outbox |
| Timestamp vem por parâmetro | o agregado nunca lê o relógio; quem chama injeta o instante (vem de `TimeProvider`) |

Os testes de arquitetura reprovam evento não-selado, não-record, sem o sufixo, ou que carregue entidade.

### Granularidade

Eventos existem nos dois níveis: `ItemDoPedidoProntoDomainEvent` (a cozinha terminou **um** prato → avisa o garçom) e `PedidoProntoDomainEvent` (a comanda toda está pronta → libera a entrega). São fatos de negócio diferentes com consumidores diferentes.

**Evolução prevista:** publicar após o commit tem uma janela de falha (commit ok, publicação morre). A solução é o **padrão Outbox** — gravar os eventos na mesma transação e um worker publicar depois. Fora do escopo atual, anotado de propósito.

---

## 5. Erros: `Result<T>` e quando exception

Decisão: **`Result<T>` para falha de negócio esperada; exception só para o inesperado.**

Uma mesa ocupada, um pedido sem itens, um CNPJ inválido — não são acidentes, são fluxo normal. Exception para isso usa o mecanismo de "algo deu errado" para modelar "o negócio disse não", e torna a falha invisível na assinatura.

```csharp
public Result Confirmar(DateTimeOffset confirmadoEm)      // pode falhar, está na assinatura
public static Result<Cnpj> Criar(string? entrada)         // pode falhar, está na assinatura
```

`Error` carrega `Codigo` (`Pedido.SemItens`) e `ErrorType` (`Validacao | ConflitoDeEstado | NaoEncontrado`) — o `ErrorType` mapeia para status HTTP na API sem `if` por caso, e o `Codigo` é estável para o front tratar.

**VO nunca tem construtor que lança.** Construtor privado + `static Result<T> Criar(...)`. Assim `new Cnpj(...)` não existe e um `Cnpj` que existe é válido por construção.

`DomainException` fica para **estado impossível**, em exatamente três lugares:
- `Result<T>.Value` de um `Result` que falhou (bug de quem chama).
- `SmartEnum.DeValor(99)` (dado corrompido).
- `GarantirTransicaoDeItem` — transição interna do agregado que as guardas anteriores deveriam ter tornado inalcançável. Se estourar, o agregado está inconsistente e travar é melhor que continuar.

---

## 6. Dado derivado nunca é persistido

`Total`, `Subtotal`, `ValorDaTaxaDeServico` são **propriedades calculadas** sobre `ItensAtivos`. Não têm setter. Isso torna impossível o total divergir dos itens — o bug clássico de ERP.

`TempoDecorrido(agora)` recebe o instante como parâmetro em vez de ler o relógio, e congela em `FechadoEm` quando o pedido fecha. `MinutosDecorridos` e `PrioridadeEfetiva` são calculados **na leitura**, via `TimeProvider` + `PoliticaDePrioridade`.

Consequência prática: o painel de pedidos funciona **sem job de atualização**. Nada precisa varrer a tabela reclassificando prioridade — a prioridade é uma função do relógio, avaliada quando alguém olha.

**Exceção deliberada:** `PedidoFechadoDomainEvent` carrega `Subtotal`, `TaxaDeServico` e `Total` como valores. Ali o número precisa ser congelado: é o registro fiscal do que foi cobrado, não uma projeção.

---

## 7. Recursos .NET / C# usados e para quê

| Recurso | Onde | Para quê |
|---|---|---|
| `readonly record struct` | todos os IDs | igualdade estrutural + zero alocação + type safety entre IDs |
| `Guid.CreateVersion7()` | `PedidoId.Novo()` etc | timestamp nos bits altos → localidade de índice no Postgres |
| `record` / `sealed record` | domain events, `Error` | imutabilidade e igualdade estrutural sem boilerplate |
| `TimeProvider` + `FakeTimeProvider` | política de prioridade, tempo decorrido | testar lógica temporal sem `Thread.Sleep` e sem teste dependente do relógio real |
| Collection expressions `[]` | `_itens = []`, tabelas de transição | menos ruído que `new List<T>()` |
| `IReadOnlyList<T>` + backing field | `Pedido.Itens` | impede `pedido.Itens.Add(...)` de fora do agregado |
| `FrozenDictionary` | lookup do `SmartEnum` | dicionário imutável otimizado para leitura, construído uma vez via `Lazy` |
| `[GeneratedRegex]` | `Email` | regex compilada em tempo de build (source generator), sem custo de interpretação em runtime |
| `params ReadOnlySpan<T>` | `Result.PrimeiraFalha` | combinar vários `Result` sem alocar array |
| Pattern matching (`is < 1 or > Maxima`) | validações de VO | expressa faixa numa linha legível |
| Nullable reference types | tudo | `Observacao?` diz no tipo que é opcional; `NomeCliente?` também |
| `MidpointRounding.ToEven` | `Dinheiro` | arredondamento bancário, evita viés acumulado em somas |
| Construtor privado sem parâmetros | `Pedido`, `ItemPedido` | permite o EF Core materializar sem abrir brecha na factory |
| `internal` em métodos de `ItemPedido` | `AlterarQuantidade`, `Cancelar`, `TransicionarPara` | só o agregado (mesmo assembly) manipula a entidade filha; a API pública do domínio é o `Pedido` |
| Construtor primário | handlers, adapters (a vir) | injeção de dependência sem campo + atribuição. **Não** em agregado/VO: primary ctor de classe é sempre público e furaria a factory |
| `MediatR.Contracts` só | `IDomainEvent` | marker interface, ~8KB, zero comportamento — o domínio fica praticamente puro |
| xunit v3 `[Theory]` + `[InlineData]` | matriz de transição de status | 25 combinações permitidas/proibidas sem 25 métodos |
| Reflection nos testes de arquitetura | `Restaurant.ArchitectureTests` | trava a regra de dependência e as convenções de POO no CI, em vez de confiar em revisão |

---

## 8. Por que smart enum e não `enum`

`enum` é um `int` com nome — não carrega comportamento. Com `enum`, "de qual status posso ir para qual" viraria um `switch` que se repete em todo lugar que precisa da resposta, e cada status novo obriga a caçar todos eles (violação direta de OCP).

`StatusPedido : SmartEnum<StatusPedido>` põe a resposta no próprio status:

```csharp
public bool PodeTransicionarPara(StatusPedido destino) => TransicoesPermitidas().Contains(destino);
public bool AceitaNovosItens => this == Aberto || this == Confirmado || this == EmPreparo || this == Pronto;
public bool EhFinal => this == Fechado || this == Cancelado;
```

Status novo entra em **um** arquivo. Bônus: persiste legível no banco (`"EmPreparo"`, não `3`) e `Todos` permite varrer o conjunto nos testes — é assim que `Status_finais_nao_tem_saida` prova que `Fechado` e `Cancelado` não têm saída *para nenhum* destino, sem enumerar à mão.

Custo: reflection na primeira leitura de `Todos`/`DeValor`, mitigado por `Lazy<FrozenDictionary>`. E os analisadores reclamam (`CA1000`, `CA1711`), suprimidos só em `Domain/BuildingBlocks` com o motivo registrado no `CLAUDE.md`.

---

## 9. Por que as pastas são assim

A estrutura de pastas não é organização cosmética — cada nível responde a uma pergunta diferente do desenho.

### Os três níveis de topo separam por *conhecimento*

| Pasta | O que sabe |
|---|---|
| `BuildingBlocks/` | **nada** de negócio. `Entity`, `Result`, `SmartEnum` funcionariam num sistema bancário sem uma linha alterada — poderia virar um NuGet |
| `SharedKernel/` | negócio **compartilhado** entre contextos: `Dinheiro`, `Cnpj`, `EstabelecimentoId` |
| `BoundedContexts/` | negócio **de um contexto só** |

A fronteira entre a primeira e a segunda é a que mais escorrega. Na primeira versão, `ITenantScoped` estava em `Abstractions/` e importava `EstabelecimentoId` — ou seja, os "blocos genéricos" conheciam um conceito deste projeto. Mover `ITenantScoped` para `SharedKernel/Tenancy/` restaurou a separação, e `BuildingBlocks_nao_conhece_negocio` impede a regressão.

### Dentro do contexto, o agregado é a unidade

`PedidoAggregate/` agrupa a raiz, a entidade filha, os IDs, enums, VOs e eventos **daquele** agregado. Isso responde a uma pergunta que a organização por tipo não responde: em `Cardapio`, com `Produto` e `Categoria`, de quem é o VO `NomeDeProduto`? Agrupando por agregado, a resposta está no caminho do arquivo.

`Policies/` e `Ports/` ficam **fora** do agregado, no nível do contexto, porque não pertencem a um agregado específico — `PoliticaDePrioridade` é política do contexto, e as portas são contratos que a infraestrutura implementa.

### Por que `PedidoAggregate/` e não `Pedido/`

`Aggregates/Pedido/` produziria o namespace `...Aggregates.Pedido` contendo a classe `Pedido` — classe com o mesmo nome do namespace que a contém. Gera ambiguidade de resolução de nome e a Microsoft desaconselha. É a mesma armadilha que evitamos chamando o tenant de `Estabelecimento` em vez de `Restaurant`.

### Um tipo por arquivo, namespaces alinhados

Os 12 domain events viviam em `PedidoDomainEvents.cs`; agora são 12 arquivos. Não é purismo: você acha `PedidoConfirmadoDomainEvent` pelo nome do arquivo, e o diff do git aponta exatamente qual evento mudou.

Namespaces batem 1:1 com as pastas — sem suprimir `IDE0130`. O efeito colateral é bom: `Pedido.cs` declara 10 `using`, e essa lista **é** a documentação de quais blocos ele toca. Dá para ver de relance que ele usa `SharedKernel` e nenhum outro contexto.

### Erro mora com a regra que o produz

`Dinheiro.Negativo`, `Cnpj.DigitoVerificadorInvalido`, `Quantidade.ForaDaFaixa` — cada VO declara os próprios erros. Antes havia uma classe central `CompartilhadoErrors` para metade dos VOs enquanto a outra metade já declarava os seus; duas convenções brigando.

`PedidoErrors` continua central e isso é coerente: `SemItens`, `ItensPendentes`, `TransicaoInvalida` são invariantes **do agregado**, não de um tipo isolado — não há um único dono natural.

### A convenção é teste, não disciplina

Sete testes em `ConvencaoDePastasTests` travam tudo acima. O detalhe que os torna úteis é a **direção inversa**: não basta "todo tipo em `ValueObjects/` é um `ValueObject`" — também vale "todo `ValueObject` está em `ValueObjects/`". Sem isso, criar um VO no lugar errado passaria despercebido; com isso, a pasta é a *definição* do que ela contém.

Cada um foi validado por mutação — quebrar a regra de propósito e confirmar que o teste reprova. Vale o método: o primeiro deles passava vacuamente, porque a varredura filtrava só classes concretas e a violação que eu plantei estava numa **interface**. Teste de arquitetura que nunca viu falhar não prova nada.
