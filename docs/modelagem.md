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
| `Produto` | Rastreado individualmente: muda de preço, esgota, é reposto, é descontinuado. O histórico dessas mudanças importa. |
| `Categoria` | Nome e ordem de exibição editáveis pelo gerente, independentes dos produtos que ela agrupa. |
| `Mesa` | A mesa 12 continua sendo a mesa 12 depois de mil clientes. Tem status próprio (`Livre`/`Reservada`/`Ocupada`). |
| `Estabelecimento` | O tenant. Único agregado que implementa `ITenantRoot` em vez de `ITenantScoped`: ele *é* o escopo, não está dentro de um. |
| `Funcionario` | Muda de cargo, é desligado. Dois funcionários homônimos são pessoas diferentes. |

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
| `Endereco` | VO **composto**: 7 componentes que só fazem sentido juntos. Trocar de endereço é substituir o objeto inteiro, não editar o bairro. A UF é validada contra as 27 unidades federativas. |
| `Cep` | Mesmo padrão do `Cnpj`: 8 dígitos, normaliza a máscara, `01001-000` e `01001000` são iguais. |
| `TempoDePreparo` | Encapsula 1..240 minutos e expõe `Duracao` como `TimeSpan`. Um `int` cru aceitaria preparo de -5 minutos. |
| `NomeDeProduto`, `NomeDeCategoria`, `NomeFantasia`, `DescricaoDeProduto`, `NumeroDaMesa` | Strings com regra de tamanho e normalização de espaços. `NumeroDaMesa` também normaliza para maiúsculo, então `varanda-a` e `VARANDA-A` são a mesma mesa. |

### Casos limítrofes e como decidimos

- **`Moeda`** poderia ser VO, mas é um conjunto fechado e conhecido → **smart enum** (singleton), com `Simbolo` pendurado. Comparação por referência funciona e não há alocação por uso.
- **`Categoria`** virou **agregado próprio**, não VO dentro de `Produto`: tem nome editável e ordem de exibição administrados independentemente. Se fosse VO embutido, renomear "Carnes" exigiria um `UPDATE` em todos os produtos. Se fosse só um rótulo imutável, seria VO.
- **`Cargo`** é smart enum, não VO nem string: conjunto fechado de quatro valores, e cada um carrega **comportamento** (`PodeGerenciarCardapio`, `PodeFecharConta`). Permissão vira conceito de domínio em vez de `if` na API.
- **`Disponibilidade`** poderia ser `bool Esgotado`. Virou smart enum porque `Disponivel`/`Esgotado` tem nome no negócio, e porque `PodeSerPedido` fica pendurado nele em vez de espalhado.
- **`TempoDecorrido` / `PrioridadeEfetiva`** não são nem entidade nem VO persistido — são **derivados** (ver §7).

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

## 3. O agregado: o que é e como identificar

### O que quebra sem a fronteira

Suponha que `Pedido` e `ItemPedido` fossem agregados separados, cada um com seu repositório e sua transação:

```
19:42:01.000  Cozinha:  le os 3 itens do pedido, todos Pronto
19:42:01.010  Garcom:   cliente pediu mais uma cerveja
                        INSERT ItemPedido (Pendente)    [transacao propria]  COMMIT
19:42:01.020  Cozinha:  UPDATE Pedido SET Status = 'Pronto'
                                                        [transacao propria]  COMMIT
```

Resultado gravado: **pedido `Pronto` com um item `Pendente`**.

Nenhuma exceção, nenhum log, nenhum teste vermelho. O painel mostra o pedido pronto, a cerveja nunca é feita. A invariante "pedido pronto ⇒ todos os itens prontos" está violada em disco, permanentemente.

O agregado é exatamente o que torna esse cenário impossível: as duas operações passam por `Pedido`, disputam a mesma linha e o mesmo token de concorrência otimista. Uma delas falha, relê e enxerga o item novo.

### A definição mecânica

Um agregado é:

- **1 repositório** — você busca pela raiz, nunca pelos filhos
- **1 transação** — tudo dentro salva junto ou não salva
- **1 token de concorrência** — uma linha versionada que serializa escritas concorrentes
- **N objetos** que viajam juntos por causa disso

Não é "objeto com filhos", nem "coisas que aparecem juntas na tela". É **o conjunto mínimo que precisa ser carregado e travado junto para que uma regra nunca possa ser violada**.

Se você consegue nomear a regra que obriga dois objetos a viajarem juntos, eles são um agregado. Se não consegue nomear, são dois.

### Como identificar: procedimento, não intuição

1. Escreva as regras de negócio como frases.
2. Sublinhe os substantivos de cada uma.
3. Regra que toca dois ou mais substantivos indica candidatos ao mesmo agregado.
4. Pergunte: **"essa regra pode ficar violada por 1 segundo sem causar dano?"**

O teste do 1 segundo é a ferramenta mais afiada aqui, porque traduz uma decisão de design numa pergunta que qualquer garçom responde.

| Regra | Toca | Violável por 1s? | Decisão |
|---|---|---|---|
| Pedido só fica pronto se todos os itens estiverem prontos | `Pedido`, `ItemPedido` | **Não**, prato sai faltando | mesmo agregado |
| Pedido confirmado tem ao menos um item | `Pedido`, `ItemPedido` | **Não** | mesmo agregado |
| Total é a soma dos itens ativos | `Pedido`, `ItemPedido` | **Não**, cobra errado | mesmo agregado |
| Mesa com pedido aberto está ocupada | `Mesa`, `Pedido` | **Sim**, 50 ms de atraso não quebra nada | separados, ligados por domain event |
| Produto pertence a uma categoria | `Produto`, `Categoria` | **Sim** | separados, referência por `CategoriaId` |
| Funcionário desligado não muda de cargo | `Funcionario` | um substantivo só | invariante interna do agregado |

### Quando criar um agregado novo

Quatro sinais. Dois ou mais já indicam:

1. **Ciclo de vida próprio**, nasce e morre independente
2. **Referenciado por id de fora**, outro contexto aponta para ele
3. **Editado isoladamente**, alguém altera só ele
4. **Precisa de repositório**, você quer buscá-lo direto

`Produto` marca os quatro. `ItemPedido` não marca nenhum: só existe dentro do pedido, ninguém o referencia de fora, não tem repositório e não é editável sem passar pela raiz.

### A regra dos agregados pequenos

Vernon: proteja invariantes verdadeiras dentro da fronteira, projete agregados **pequenos**, referencie outros só por identidade, use consistência eventual fora da fronteira.

A segunda é a mais violada, e a tentação apareceu neste projeto: criar um agregado `Cardapio` contendo todos os produtos e categorias, porque "fazem sentido juntos". O custo seria um lock único no cardápio inteiro, e dois gerentes não conseguiriam editar produtos diferentes ao mesmo tempo. Por isso `Produto` e `Categoria` são agregados separados.

Mesmo raciocínio manteve `Mesa` fora de `Pedido`: se estivesse dentro, dois garçons em **mesas diferentes** competiriam pelo mesmo agregado sem nenhum motivo de negócio.

### O caso que não cabe em agregado nenhum

"Não pode haver dois produtos com o mesmo nome no estabelecimento."

Sublinhe os substantivos: toca `Produto` e `Produto`. A regra atravessa **instâncias do mesmo agregado**. Um `Produto` não enxerga os irmãos, e criar um agregado que contenha todos travaria o cardápio a cada edição.

Quando o teste do 1 segundo dá "não" mas a regra atravessa instâncias, a resposta não é agregado maior, é **porta**: `IVerificadorDeNomeUnicoDeProduto`, mais um índice único no banco como rede de segurança. Mesma situação de `IVerificadorDeCnpjUnico` em `Contas`.

### O erro que mais atrapalha

Pensar em agregado como estrutura de dados. É regra de **concorrência**.

"Preciso listar os produtos de uma categoria" não é motivo para `Categoria` conter `Produto`: isso é necessidade de **consulta**, e consulta se resolve no read side com um `WHERE CategoriaId = @id`. Agregado é sobre **escrita**.

A pergunta certa nunca é "o que vive dentro do quê", e sim:

> **Que coisas precisam ser salvas na mesma transação para que essa regra seja impossível de violar?**

A resposta a essa pergunta *é* o agregado.

---

## 4. Onde a lógica mora: agregado vs domain service

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

Este é o único ponto do domínio em que uma porta é consumida **dentro** de um método de agregado, o que é o padrão que Vernon recomenda: passar o domain service como parâmetro do método, em vez de injetá-lo no construtor. A consequência honesta é que `Produto.CadastrarAsync` e `Produto.RenomearAsync` são assíncronos, porque a verificação toca o banco. `RenomearAsync` passa o próprio `ProdutoId` no parâmetro `ignorando`, senão o produto colidiria consigo mesmo.

`IVerificadorDeCnpjUnico` em `Contas` é o mesmo padrão com uma diferença: a unicidade do CNPJ é **global**, não por tenant, porque não existe escopo acima do estabelecimento.

### O que **não** justifica domain service

Tudo que o agregado consegue decidir sozinho. `Confirmar`, `AdicionarItem`, `CancelarItem`, cálculo de `Subtotal`/`Total` — tudo método de `Pedido`, porque toda a informação necessária está dentro da fronteira. Extrair isso para um `PedidoService` produziria exatamente o modelo anêmico que o exercício quer evitar.

### E os agregados?

A fronteira de consistência é assunto da [§3](#3-o-agregado-o-que-é-e-como-identificar). O que importa aqui: domain service é o que sobra quando a lógica **não cabe** em nenhum agregado.

---

## 5. Domain events: estrutura e motivo

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

### Os 31 eventos por contexto

| Contexto | Eventos | Papel |
|---|---|---|
| `Pedidos` | 12 | ciclo de vida do pedido e dos itens |
| `Cardapio` | 9 | `PrecoDoProdutoAlteradoDomainEvent` carrega preço **anterior e novo**, porque auditoria de preço é requisito real de restaurante |
| `Salao` | 4 | `MesaOcupadaDomainEvent`, `MesaLiberadaDomainEvent` e companhia |
| `Contas` | 6 | cadastro, mudança de taxa, admissão e desligamento |

### O evento que ainda não tem ouvinte

`PedidoAbertoDomainEvent` e `PedidoFechadoDomainEvent` carregam `MesaId` justamente para que um handler em Application chame `Mesa.Ocupar()` e `Mesa.Liberar()`. Os dois métodos existem e estão testados, mas **nenhum handler foi escrito ainda** — Application ainda é casca.

Vale notar o desenho: `Mesa` **não** referencia `PedidoId`. A relação é unidirecional `Pedido → Mesa`. A mesa precisa saber que está ocupada, não por quem. Isso evita referência bidirecional entre contextos e é verificado pelo teste `Contexto_nao_referencia_outro_contexto`.

**Evolução prevista:** publicar após o commit tem uma janela de falha (commit ok, publicação morre). A solução é o **padrão Outbox** — gravar os eventos na mesma transação e um worker publicar depois. Fora do escopo atual, anotado de propósito.

---

## 6. Erros: `Result<T>` e quando exception

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

## 7. Dado derivado nunca é persistido

`Total`, `Subtotal`, `ValorDaTaxaDeServico` são **propriedades calculadas** sobre `ItensAtivos`. Não têm setter. Isso torna impossível o total divergir dos itens — o bug clássico de ERP.

`TempoDecorrido(agora)` recebe o instante como parâmetro em vez de ler o relógio, e congela em `FechadoEm` quando o pedido fecha. `MinutosDecorridos` e `PrioridadeEfetiva` são calculados **na leitura**, via `TimeProvider` + `PoliticaDePrioridade`.

Consequência prática: o painel de pedidos funciona **sem job de atualização**. Nada precisa varrer a tabela reclassificando prioridade — a prioridade é uma função do relógio, avaliada quando alguém olha.

**Exceção deliberada:** `PedidoFechadoDomainEvent` carrega `Subtotal`, `TaxaDeServico` e `Total` como valores. Ali o número precisa ser congelado: é o registro fiscal do que foi cobrado, não uma projeção.

A regra "o relógio congela em `FechadoEm`" saiu de dentro de `Pedido.TempoDecorrido` para `PoliticaDePrioridade.Decorrido(abertoEm, fechadoEm, agora)` quando a Application chegou: o read side calcula prioridade efetiva **sem carregar o agregado**, logo precisa da regra sem ter a quem perguntar. `Pedido.TempoDecorrido` passou a delegar, então a regra continua com um dono único.

O cálculo acontece no **handler de query**, não no adapter de persistência. Três razões: a política continua testável com `FakeTimeProvider` no nível do caso de uso; o SQL fica determinístico, sem `now()`; e a ordenação do painel depende da prioridade efetiva, logo só pode acontecer depois do cálculo.

**Gap conhecido — fuso horário.** `AbrirPedido` gera o número do dia com `DateOnly.FromDateTime(agora.UtcDateTime)`, o que coloca a virada do dia em UTC. Um restaurante que fecha às 2h da manhã vai ver o sequencial virar no meio do expediente. `Estabelecimento` não tem fuso modelado ainda; quando tiver, é nesse handler que ele entra.

---

## 8. Recursos .NET / C# usados e para quê

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
| `FrozenSet` | validação de UF em `Endereco` | 27 valores, lidos sempre e nunca alterados: estrutura imutável otimizada para leitura |
| Interface marcadora | `ITenantRoot` | `Estabelecimento` **é** o tenant, então implementar `ITenantScoped` apontando para si mesmo seria mentira. A exceção fica documentada no tipo, não escondida numa lista de exclusão do teste |
| Domain service como parâmetro de método | `Produto.CadastrarAsync(..., IVerificadorDeNomeUnicoDeProduto)` | padrão recomendado por Vernon: injetar no método, não no construtor. Consequência honesta: o método vira assíncrono, porque a verificação toca o banco |

---

## 9. Por que smart enum e não `enum`

`enum` é um `int` com nome — não carrega comportamento. Com `enum`, "de qual status posso ir para qual" viraria um `switch` que se repete em todo lugar que precisa da resposta, e cada status novo obriga a caçar todos eles (violação direta de OCP).

`StatusPedido : SmartEnum<StatusPedido>` põe a resposta no próprio status:

```csharp
public bool PodeTransicionarPara(StatusPedido destino) => TransicoesPermitidas().Contains(destino);
public bool AceitaNovosItens => this == Aberto || this == Confirmado || this == EmPreparo || this == Pronto;
public bool EhFinal => this == Fechado || this == Cancelado;
```

Status novo entra em **um** arquivo. Bônus: persiste legível no banco (`"EmPreparo"`, não `3`) e `Todos` permite varrer o conjunto nos testes — é assim que `Status_finais_nao_tem_saida` prova que `Fechado` e `Cancelado` não têm saída *para nenhum* destino, sem enumerar à mão.

O exemplo mais forte de "enum com comportamento" acabou sendo o `Cargo`:

```csharp
Cargo.Todos.Where(cargo => cargo.PodeAvancarPreparo && !cargo.PodeRegistrarPedido)
// => [Cozinha]
```

As permissões viram dado consultável do domínio, e não uma cascata de `if (usuario.Cargo == "Garcom")` espalhada pela API. Um cargo novo entra em um arquivo e todas as permissões vêm junto.

Custo: reflection na primeira leitura de `Todos`/`DeValor`, mitigado por `Lazy<FrozenDictionary>`. E os analisadores reclamam (`CA1000`, `CA1711`), suprimidos só em `Domain/BuildingBlocks` com o motivo registrado no `CLAUDE.md`.

---
## 10. Por que as pastas são assim

A estrutura de pastas não é organização cosmética — cada nível responde a uma pergunta diferente do desenho, e a resposta é travada por teste de arquitetura.

O documento dedicado é [estrutura.md](estrutura.md): o que significa "building blocks", os três níveis de conhecimento (genérico → compartilhado → contexto), por que o agregado é a unidade de agrupamento dentro do contexto, uma árvore de decisão para "onde ponho meu arquivo novo", e como os sete testes de `ConvencaoDePastasTests` transformam a convenção em garantia.

O ponto que mais importa, em uma linha: **`BuildingBlocks/` é a caixa de ferramentas, `BoundedContexts/` é o que foi construído com ela** — `Pedido` é uma entidade, mas quem mora em `BuildingBlocks/Model/` é a classe base `Entity<TId>`, não o `Pedido`.
