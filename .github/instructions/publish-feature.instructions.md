---
applyTo: "BotDeScans.App/Features/Publish/**"
description: >
  Skill (conhecimento + diretrizes) para evoluir e revisar a feature
  `Features/Publish`, responsável por orquestrar o pipeline de publicação
  de capítulos: download dos arquivos, processamento (compressão, ZIP, PDF),
  upload em provedores externos (Mega, Box, Google Drive, MangaDex,
  Sakura Mangás) e publicação no Blogger, com feedback em tempo real no
  Discord.
---

# Skill — `Features/Publish`

> Esta skill descreve **como o pipeline de publicação funciona hoje**, quais
> são os contratos esperados ao adicionar/alterar passos, e os trade-offs
> conhecidos da arquitetura atual. Use-a como base obrigatória ao revisar PRs
> ou propor refatorações nesta feature.

## 1. Visão geral arquitetural

A feature segue o padrão **Vertical Slice + Pipeline of Steps** dentro do
projeto `BotDeScans.App`. Está dividida em duas sub-slices:

| Pasta | Responsabilidade |
|-------|------------------|
| `Publish/Command/` | Slash-command `/publish` que abre uma **Modal** Discord para o usuário preencher os dados do capítulo. |
| `Publish/Interaction/` | Recebe o submit da modal, monta o estado e executa o pipeline de passos. |

DI é registrada de forma autossuficiente por slice via arquivos
`+Dependencies.cs` (`AddPublishServices` ? `AddCommands` + `AddInteractions`
+ `AddPublishSteps` + `AddPings`). Todos os tipos são `Scoped`, exceto pings
sem estado (`EveryonePing`, `NonePing`) que são `Singleton`. O escopo do
contêiner casa com o ciclo de vida de **uma interação Discord**, então o
`State` compartilhado vive apenas durante uma execução do comando.

## 2. Fluxo de execução (alto nível)

```
/publish (Commands.cs)
   ??? Modal "Features.Publish"
         ??? Interactions.ExecuteAsync (submit)
               1. Preenche State.ChapterInfo
               2. State.Steps = StepsService.GetEnabledSteps()
               3. Handler.ExecuteAsync(ct)
                     ?? ManagementSteps ? Execute (em ordem)
                     ?? PublishSteps    ? Validate (todos antes de qualquer execute)
                     ?? PublishSteps    ? Execute  (em ordem)
               4. Sucesso ? DiscordPublisher.SuccessReleaseMessageAsync
                  Falha   ? DiscordPublisher.ErrorReleaseMessageAsync
```

O `Handler` mantém o invariante crítico: **se algum passo falhar, a cadeia
para imediatamente** (`if (result.IsFailed) break;`). O `DiscordPublisher`
é chamado entre cada passo para atualizar a mesma embed (a "tracking
message") refletindo `StepStatus` de cada `StepInfo`.

## 3. Modelo de domínio do pipeline

### 3.1. Hierarquia de Steps (`Steps/IStep.cs`)

```
IStep                       // ExecuteAsync, Name (StepName), Type (StepType)
 ?? IManagementStep         // + IsMandatory
 ?? IPublishStep            // + Dependency (StepName?), ValidateAsync
```

* `IManagementStep` = passos internos (Setup, Download, Compress, Zip,
  Pdf). Não têm validação prévia; os marcados como `IsMandatory = true`
  entram no pipeline **mesmo que ausentes na configuração**.
* `IPublishStep` = passos que falam com mundo externo (uploads,
  publicação no Blogger). Possuem `ValidateAsync` (chamado em fase
  separada, antes de qualquer execute) e podem declarar uma
  `Dependency` (outro `StepName`) que será automaticamente incluída.

### 3.2. Seleção de passos (`StepsService.GetEnabledSteps`)

União distinta e ordenada por `StepName` de:

1. **Configuração** — `Settings:Publish:Steps` (lista de `StepName`).
2. **Dependências** — para cada `IPublishStep` selecionado, inclui o
   `StepName` declarado em `Dependency` (ex.: `UploadZipMega` puxa
   `ZipFiles`).
3. **Mandatórios** — todos os `IManagementStep` com `IsMandatory = true`.

O resultado é encapsulado em `EnabledSteps : ReadOnlyDictionary<IStep, StepInfo>`,
que expõe `ManagementSteps`, `PublishSteps`, `MessageStatus`, `Details`,
`ColorStatus`. **Esse modelo é o "view-model" da embed do Discord**.

### 3.3. Estado compartilhado (`State`)

Bag mutável (todos `set`) com:

* `Steps` — `EnabledSteps`.
* `Title` — entidade de banco preenchida no `SetupStep`.
* `ChapterInfo` — DTO da modal.
* `ReleaseLinks` — record de URLs por destino, descobertas conforme cada
  upload termina; usado em `Links` fields da embed final e pelo
  `TextReplacer`.
* `InternalData` — paths de arquivos intermediários
  (`OriginContentFolder`, `CoverFilePath`, `ZipFilePath`, `PdfFilePath`,
  `BloggerImageAsBase64`, `BoxPdfReaderKey`, `Pings`).

`StateValidator` (FluentValidation) é executado **dentro** do
`SetupStep` e é o lugar onde regras cross-step vivem (ex.: exigir
`ExternalReference.MangaDex` se `UploadMangaDexStep` está habilitado;
exigir role configurada se `PingType.Global`).

### 3.4. Status (`StepStatus`)

```
QueuedForValidation ? QueuedForExecution ? Success
                    ? Error
                    ? Skip   (vindo de Title.SkipSteps)
```

`StepInfo.UpdateStatus` aplica a transição automática usando o
`Result` do passo. Pulos vêm de `Title.SkipSteps` (persistidos por
título) e são checados em runtime no `Handler.ShouldSkip`.

### 3.5. Pings (`Pings/`)

Estratégia poliformica selecionada por `Settings:Publish:PingType`
(`None`, `Everyone`, `Global`, `Role`). `SetupStep` resolve o `Ping`
via `pings.Single(x => x.IsApplicable)` e armazena o texto em
`InternalData.Pings`, consumido pelo `DiscordPublisher` na mensagem
final do canal de release.

### 3.6. Substituição de texto (`TextReplacer`)

Templating manual com tags `!##KEY##!` e blocos
`!##START_REMOVE_IF_EMPTY_KEY##! ... !##END_REMOVE_IF_EMPTY_KEY##!`
removidos quando o valor correspondente é vazio. Usado para o post do
Blogger e para `state.ChapterInfo.Message` na embed de release.

## 4. Convenções obrigatórias ao alterar/criar Steps

1. **Nome do arquivo prefixado por número** (`NN_NomeStep.cs`) só para
   ordenação visual; a ordem real de execução é por `StepName`
   (`OrderBy(step => step.Name)`). Se você adicionar um step novo,
   **escolha o valor numérico de `StepName` deliberadamente** — ele é
   persistido em banco (`SkipStep`) e não pode ser alterado depois.
2. Registre o step em `Steps/+Dependencies.cs` (interface `IStep`).
3. Adicione a descrição em `StepName` (vai para a embed) e o emoji
   correspondente em `StepStatus`/atributo `Emoji`.
4. Para passos com IO externo: implemente `IPublishStep` e use
   `ValidateAsync` para falhar **cedo** (antes de qualquer upload).
   Não faça side-effects em `ValidateAsync`.
5. Se o passo consome um arquivo gerado por outro (ex.: ZIP/PDF),
   declare `Dependency` para garantir inclusão automática.
6. Toda IO/dependência externa deve retornar `FluentResults.Result`;
   exceções são capturadas no `IStep.SafeCallAsync` (extension em
   `ObjectExtensions`) e convertidas em erro genérico com log Serilog.
7. **Mutar o `State`** apenas via `state.InternalData.*` (artefatos)
   ou `state.ReleaseLinks.*` (URLs). Não toque em `Title`/`ChapterInfo`
   após `SetupStep`.
8. Marque com `[ExcludeFromCodeCoverage]` apenas wrappers de
   infraestrutura (já é a convenção dos `+Dependencies.cs` e do
   `DiscordPublisher`).

## 5. Pontos de extensão típicos

* **Novo destino de upload** ? criar `IPublishStep`, adicionar entry em
  `Links` (com `[Description]`), em `TextReplacer.ReplaceRules`, em
  `StepName` (novo valor numérico) e DI.
* **Nova validação cross-step** ? `StateValidator` com `.When(...)`
  observando `state.Steps`.
* **Novo tipo de Ping** ? herdar `Ping`, registrar em `Pings/+Dependencies`
  e adicionar valor em `PingType`.

---

# Avaliação da implementação atual

## ? Pontos fortes

1. **Vertical slice bem isolado.** Toda a feature, incluindo DI,
   contratos, modelos e UI Discord, vive sob `Features/Publish`.
   `+Dependencies.cs` por pasta evita o "DI hell" central.
2. **Pipeline declarativo via DI.** Adicionar passo = registrar uma
   classe e ajustar `StepName`. O `Handler` é agnóstico do conteúdo
   dos passos.
3. **Separação Validate/Execute para passos externos** evita gastar
   tempo/banda fazendo upload parcial quando há erro de configuração
   (referência ausente, role inexistente, etc.). É um padrão correto
   de "fail-fast" para IO caro.
4. **Feedback incremental no Discord** (`DiscordPublisher.UpdateTrackingMessageAsync`
   chamado entre cada passo) — UX excelente para um processo longo.
5. **`SafeCallAsync`** garante que exceções não derrubam o bot inteiro
   e ainda compõem um `Result` rastreável.
6. **`StepName` numérico estável** permite persistir `SkipStep` por
   título (`Title.SkipSteps`) sem acoplar ao nome do tipo C#.
7. **Pings via Strategy + `IsApplicable`** é simples e extensível.

## ?? Pontos de atenção / smells

1. **`State` é uma bag mutável compartilhada por todos os steps.**
   Qualquer step pode escrever qualquer campo; o "contrato" entre
   steps (ex.: `ZipFilesStep` produz `ZipFilePath`, `UploadZipMega`
   o consome) só existe por convenção e via `!` (null-forgiving). Isso:
   * dificulta detectar dependências reais (apenas `StepName.Dependency`
     declara, mas nem todo consumo está mapeado);
   * impede paralelizar passos independentes com segurança;
   * torna testes unitários frágeis (precisa preparar um `State`
     completo).
2. **`Handler` mistura duas estratégias de iteração.** Constrói um
   `IEnumerable<Func<Task<Result>>>` com `Union`, depois usa `foreach`
   com `if (IsFailed) break;`. Funciona, mas o `Result.Merge` no laço
   só agrega o último — o "merge" perde os erros anteriores quando há
   curto-circuito. A intenção é clara, mas o código é difícil de
   evoluir (ex.: política de "continuar mesmo com erro" para
   `UploadSakuraMangasStep` — vide TODO no próprio arquivo).
3. **`Validate` antes de qualquer `Execute`** é bom, mas hoje todas as
   implementações de `ValidateAsync` retornam `Result.Ok()`. O contrato
   está pago em complexidade sem pagar dividendo. Ou se preenche, ou se
   considera remover/condensar.
4. **`StepsService` percorre `IEnumerable<IStep>` 3 vezes** e usa
   `Union`+`DistinctBy` para deduplicar. Funciona, mas a resolução de
   dependências é **rasa** (1 nível): se um dependente declarar uma
   dependência que ela mesma tem dependência, não é resolvido em
   cascata. Hoje não há esse caso, mas é uma armadilha futura.
5. **`DiscordPublisher` mantém `trackingMessage` como campo mutável**
   (estado implícito). Fora do escopo (Scoped DI), funciona; mas é
   uma fonte de bug se alguém reutilizar a instância. Marcado como
   `[ExcludeFromCodeCoverage]`, então não há testes que defendam o
   invariante.
6. **`TextReplacer.Replace` é O(N×M)** sobre o tamanho do template
   e regras, com múltiplos `text.Replace`/`IndexOf` por chave. Para
   um template de Blogger isso é irrelevante; só vira problema se o
   uso crescer.
7. **`+Dependencies.cs` em `Pings`** registra `Singleton` e `Scoped`
   no mesmo `IEnumerable<Ping>`. O `RolePing` (Scoped) injeta
   `State` (Scoped), o que está correto, mas registrar pings
   stateless como `Singleton` e stateful como `Scoped` em um mesmo
   `IEnumerable` exige cuidado — `IsApplicable` lê `IConfiguration`
   estaticamente em todos, o que está OK, mas se um dia for preciso
   chamar `pings` fora de um escopo, o resolve quebra silenciosamente.
8. **`ShouldSkip` é checado dentro do `Handler` e em `ValidateAsync`
   também marca `SetToSkip`**, mas o execute usa `Result.Ok()` (sem
   atualizar status), porque a marcação já foi feita no validate.
   Funciona, mas a regra "skip" está espalhada — concentrá-la num
   único ponto (ex.: ao montar `EnabledSteps`) deixaria mais óbvio.
9. **Acoplamento direto ao Google Drive em `DownloadStep`**. O nome
   sugere genérico, mas só sabe baixar de Drive. Se um dia houver
   outra origem (Mega, S3 etc.), vira problema. Renomear para
   `DownloadGoogleDriveStep` ou abstrair atrás de um `IContentSource`.
10. **`Result.Merge(result, await execStep())`** acumula `Reasons`,
    mas como o laço quebra no primeiro fail, na prática só transporta
    1 erro. Se o objetivo é coletar warnings/erros agregados, falta um
    canal separado.

## ?? Abordagens alternativas

### A) **Pipeline tipado com input/output explícitos** (estilo Result Pipeline)

Cada step declara `TIn`/`TOut`:

```csharp
public interface IPipelineStep<TIn, TOut> {
    Task<Result<TOut>> ExecuteAsync(TIn input, CancellationToken ct);
}
```

E o pipeline é montado em compile-time (`A ? B ? C`), com cada estágio
recebendo o output do anterior.

* **+** Elimina a bag mutável; dependências viram tipos.
* **+** Compila-se a topologia; passos opcionais ficam evidentes.
* **?** Muito mais cerimônia em C# (genéricos aninhados, builder
  específico).
* **?** Difícil acomodar passos opcionais/paralelos (Mega *e* Box ao
  mesmo tempo) sem virar um DAG.

### B) **MediatR / behaviors com pipeline behaviors**

Cada step vira `IRequest`/`IRequestHandler`, e o orquestrador envia
requests em sequência. Validation é um `IPipelineBehavior`.

* **+** Padrão muito conhecido na comunidade .NET.
* **+** Logging/telemetria via behaviors transversal.
* **?** Para um único caso de uso linear é overkill — adiciona
  indireção sem reduzir o problema do estado compartilhado.
* **?** O feedback "embed atualizada entre passos" não casa bem com
  o modelo request/response do MediatR.

### C) **Workflow engine (Elsa, MassTransit Sagas, Temporal/Workflows .NET, Hangfire continuations)**

* **+** Persistência do estado, retry/replay automáticos, paralelismo
  e DAG de dependências reais. Cada upload poderia ser uma activity
  resiliente.
* **+** Resolveria o problema de retomar uma publicação que falhou no
  meio (hoje: refazer tudo).
* **?** Custo operacional alto (storage, dashboard, versionamento de
  workflow).
* **?** Acoplar feedback em tempo real ao Discord exige callbacks/
  eventos da engine — nem todas suportam bem.

### D) **DAG com paralelismo controlado** (mantendo o IStep atual)

Manter `IStep`/`IPublishStep` mas substituir o `Handler` por um
scheduler que respeita `Dependency` como aresta de um grafo:

* Ms (Setup ? Download ? Compress) sequencial.
* `ZipFiles` e `PdfFiles` em paralelo (não dependem entre si).
* Cada `UploadX` paralelo a outros uploads que compartilham a mesma
  dependência (`ZipFiles` ou `PdfFiles`).

* **+** Ganho real de throughput (uploads para Mega/Box/Drive são
  IO-bound).
* **+** Mantém o modelo mental atual (steps, status, embed).
* **?** Exige tornar `State` thread-safe ou particionar por
  destino (`Links`/`InternalData` campos atômicos por upload já
  ajudam).
* **?** Atualização de embed precisa serializar (lock) para evitar
  rate-limit e race-condition na `trackingMessage`.

### E) **Imutabilizar o `State` via "context per stage"**

Trocar a bag mutável por um record imutável `PublishContext` que cada
step transforma e devolve. Equivalente a uma versão "leve" de (A) sem
genéricos:

```csharp
public interface IStep {
    Task<Result<PublishContext>> ExecuteAsync(PublishContext ctx, CancellationToken ct);
}
```

* **+** Testes ficam triviais; imutabilidade documenta o que cada
  passo produz.
* **+** Permite "dry-run" / simulação fácil.
* **?** `Links`/`InternalData` viram cópias — overhead irrisório aqui,
  mas mais código `with { ... }`.
* **?** Refator grande para todos os 14 steps.

---

## 6. Checklist de PR para esta feature

* [ ] Novo `StepName` tem valor numérico **único e final**.
* [ ] Step registrado em `Steps/+Dependencies.cs`.
* [ ] `Description` no `StepName` e `Emoji` correspondente em `StepStatus` (se status novo).
* [ ] `Dependency` declarada se consome `ZipFilePath`/`PdfFilePath`/etc.
* [ ] `ValidateAsync` faz checagens **sem side-effects**.
* [ ] Mutação de `State` apenas em `InternalData`/`ReleaseLinks`.
* [ ] Novo provedor externo: cobrir com `Result` e mapear exceções
      conhecidas em `FluentResultsExtensions.GetErrorsInfo`.
* [ ] Atualizar `Links` + `TextReplacer.ReplaceRules` se houver nova URL.
* [ ] Atualizar `StateValidator` se a presença do step exige uma
      `ExternalReference` ou configuração.
* [ ] Testes unitários cobrindo o novo step (StepInfo + Result).
