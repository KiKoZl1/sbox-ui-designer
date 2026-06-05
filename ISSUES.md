# Sbox UI Designer — Known Issues

Bugs encontrados em produção que ainda **não estão resolvidos** (ou estão resolvidos parcialmente / com caveats). Issues resolvidos não são apagados — são movidos para a seção **Archive — Resolved before V1.5** no fim deste arquivo, com 1 linha de sumário + commit de referência. O corpo desta página fica focado em issues ativos (open / partial / investigating / external / fixed-aguardando-release).

Convenção:
- **Severity** — `blocker` quebra workflow / `major` atrapalha mas tem workaround / `minor` cosmético
- **Status** — `open` / `partial` / `investigating` / `external` (bug em código de fora) / `fixed in <release>` (encerrado neste release, aguarda mover pro Archive na próxima limpeza)
- **Hypothesis** — onde provavelmente está a raiz, baseado em leitura do código
- **Archive** — issues resolvidos viram 1 linha no rodapé com data + commit; o detalhe histórico fica no git log

---

## ISSUE-004 — `<label>` element ignora alpha em `background-color` rgba() em runtime Play mode

**Reportado:** 2026-05-11
**Severity:** major
**Status:** fixed in V1.5 — see CHANGELOG, codegen now wraps Text background in outer div

### Sintoma

Elementos do tipo `Text` (que o generator emite como `<label>`) com `background-color: rgba(r,g,b,a)` onde `a < 1` renderizam o fundo como **totalmente opaco** em Test in Play e Preview tab. O alpha é ignorado visualmente.

Canvas (paint-based, depois do fix ISSUE-005 do ParseColor) renderiza corretamente — `rgba(34,197,94,0.12)` aparece como verde muito faint sobre o sidebar dark.

Em runtime: o mesmo elemento aparece como verde sólido/saturado, como se alpha = 1.0.

**Reprodução confirmada:** quest_log.sui samples q5/q6 (Drink from the River / Light a Campfire) e q1 (Find the Lost Camp com azul rgba 0.18). Todos `<label>` com rgba alpha < 0.5 ficam visualmente sólidos no runtime.

### Hypothesis

Sandbox.UI's `<label>` panel pode ter handling especial pra background-color que difere do `<div>`. Não confirmado por leitura de source — research em `gh search code --repo Facepunch/sbox-public` não achou o ponto exato no Sandbox.UI parser.

Evidência circumstancial:
- Pesquisa confirma rgba alpha É suportado em geral pelo Sandbox.UI CSS engine (exemplos: `rgba( 0, 0, 0, 0.55 )` em HUDs oficiais)
- Todos os `<label>` no nosso preview ignoram alpha — padrão consistente
- Este `ISSUES.md` (entrada antiga em ISSUE-002) já mencionou suspeita similar sobre element-type-specific color handling

### Possíveis caminhos de resolução

1. **Mudar generator** pra emitir Text elements como `<div>` em vez de `<label>` quando há `background-color` definido — risco baixo, mantém compatibilidade visual; custo: 30min + teste
2. **Wrap label em div**: `<div class="..."><label>text</label></div>` — emite bg no outer div, mantém label pra text rendering — risco baixo; custo: similar
3. **Reportar pra Facepunch** se confirmar que é bug do engine — não dá pra fixar do nosso lado se for issue do Sandbox.UI core

### Path de teste

1. Abrir `quest_log.sui` em Test in Play
2. Verificar que q5/q6 (Drink from the River, Light a Campfire) têm bg verde sólido
3. Comparar com canvas — canvas mostra bg muito faint (correto pra alpha 0.12)
4. Trocar manualmente um Text element por Panel + Text inside no .sui → ver se alpha passa a funcionar

### Arquivo relacionado
- [`Code/Generation/SuiRazorGenerator.cs:97`](Code/Generation/SuiRazorGenerator.cs) — `EmitTextElement` emite `<label>`
- Samples afetados: `Assets/SuiSamples/quest_log.sui`, `Assets/SuiSamples/loot_pickup.sui`

---

## ISSUE-005 — PreviewCount badges (stack counts) não emitidos em Razor

**Reportado:** 2026-05-11
**Severity:** minor
**Status:** open

### Sintoma

`InventorySlot` / `ItemIcon` elements têm prop `PreviewCount` (ex: "20", "3", "8"). O canvas paint (`SuiCanvasRenderer.PaintItemIcon`) desenha esse count como overlay no canto bottom-right do slot.

**Em runtime Test in Play / Preview, o count não aparece.** O Razor generator não emite o `<label>` filho com o count text.

Resultado: divergência canvas vs runtime — canvas mostra "20" em cima do berry stack, runtime mostra só o ícone.

### Hypothesis

`SuiRazorGenerator.EmitContainerElement` chama `EmitIntrinsicContent` que só trata o caso `Button` (label do botão). Não há case pra `InventorySlot` / `ItemIcon` emitindo o count overlay.

### Possíveis caminhos de resolução

1. Adicionar case em `EmitIntrinsicContent` pra `InventorySlot`/`ItemIcon`: se `PreviewCount > 0`, emite `<label class="count">{PreviewCount}</label>` + SCSS pra posicionar absolute bottom-right
2. Adicionar SCSS automaticamente pro `.count` (position: absolute; right: 4px; bottom: 4px; font-weight: bold; color: white; text-shadow: ...)

### Arquivo relacionado
- [`Code/Generation/SuiRazorGenerator.cs:141`](Code/Generation/SuiRazorGenerator.cs) — `EmitIntrinsicContent` ponto de extensão
- [`Editor/Canvas/SuiCanvasRenderer.cs:385`](Editor/Canvas/SuiCanvasRenderer.cs) — `PaintItemIcon` referência de como canvas pinta

---

## ISSUE-006 — Shift/Ctrl+click no Hierarchy não faz multi-select

**Reportado:** 2026-05-13 (primeiro issue da comunidade — [#2](https://github.com/KiKoZl1/sbox-ui-designer/issues/2), por @FinallyDeadUwU)
**Severity:** major
**Status:** open — scheduled for V1.5.1; workaround: use single-select / delete one at a time

### Sintoma

Segurar Shift e clicar em vários elementos no painel Hierarchy seleciona **apenas um** (o último clicado), em vez de acumular a seleção. O canvas já suporta multi-select (marquee + Shift), mas o Hierarchy não.

**Reprodução confirmada:** criar `.sui`, soltar elementos no designer, segurar Shift e clicar em múltiplos no Hierarchy → só o último fica selecionado.

### Hypothesis

A infra de multi-select já existe (`controller.SelectedSet` + `controller.SetSelection(HashSet)`, usada pelo canvas marquee em `SuiCanvasWidget.cs:342-347`). O Hierarchy só não foi ligado nela. Cadeia do bug:

1. `SuiHierarchyWidget.cs:~834` — `SuiTreeRow.OnMousePress` chama `_tree.OnRowClicked(this)` **sem passar `e.KeyboardModifiers`**
2. `SuiHierarchyWidget.cs:~475` — `SuiTreeView.OnRowClicked` só invoca `OnElementSelected(row.Element)` — single element, sem modifier
3. `SuiDesignerWindow.cs:776` — handler: `_controller.SetSelected(el)` → **substitui a seleção inteira**

### Possíveis caminhos de resolução

1. **Wire dos modifiers pela cadeia** (~30-40 linhas, baixo risco):
   - `SuiTreeRow.OnMousePress` passa o `MouseEvent` pro `OnRowClicked`
   - `SuiTreeView.OnRowClicked(row, e)` lê `(e.KeyboardModifiers & KeyboardModifiers.Shift)` / `.Ctrl` e roteia pra 1 de 3 callbacks: `OnElementSelected` (replace, existente), `OnElementAddedToSelection` (Shift = add), `OnElementToggled` (Ctrl = toggle)
   - `SuiHierarchyWidget` ganha 2 events novos; `SuiDesignerWindow` liga eles em `controller.SetSelection(...)`
   - Comportamento final = idêntico ao canvas marquee (Shift = add) + Ctrl (toggle), batendo com `docs/reference/keyboard-shortcuts.md`
2. **Range select** (Shift+click do primeiro → Shift+click do último seleciona o intervalo) — mais complexo, **deferido pra V1.0.3 ou V1.5**, fora do escopo do fix imediato.

### Path de teste

1. Criar `.sui`, soltar 3+ elementos (Text, Panel, etc.)
2. Click num elemento no Hierarchy → seleciona ele (replace) ✓
3. Shift+click em outro → ambos selecionados ✓
4. Ctrl+click num já-selecionado → remove ele da seleção ✓
5. Ctrl+click num não-selecionado → adiciona ✓
6. Confirmar que Details panel + canvas refletem a multi-seleção

### Arquivo relacionado

- [`Editor/Widgets/SuiHierarchyWidget.cs`](Editor/Widgets/SuiHierarchyWidget.cs) — `SuiTreeRow.OnMousePress` (~834), `SuiTreeView.OnRowClicked` (~475), event wiring (~90)
- [`Editor/SuiDesignerWindow.cs:776`](Editor/SuiDesignerWindow.cs) — `_hierarchy.ElementSelected` handler
- [`Editor/Widgets/SuiCanvasWidget.cs:342-347`](Editor/Widgets/SuiCanvasWidget.cs) — referência: como o canvas faz Shift-additive

---

## ISSUE-007 — Delete em multi-seleção só apaga 1 elemento

**Reportado:** 2026-05-14 (reportado pela comunidade)
**Severity:** major
**Status:** open — scheduled for V1.5.1; workaround: use single-select / delete one at a time

### Sintoma

Selecionar vários elementos no canvas (Shift+marquee ou Shift+click) e pressionar Del apaga **apenas um** (o primary — último focado). Os outros selecionados permanecem.

Mesma família do ISSUE-006: a infra de multi-select existe (`controller.SelectedSet`), mas a operação de delete não consome ela — só age sobre o primary `Selected`.

### Hypothesis

`SuiDesignerController.DeleteElement(element = null)` (`SuiDesignerController.cs:310-318`):

```csharp
public void DeleteElement( SuiElement element = null )
{
    element ??= Selected;   // ← só o primary, ignora SelectedSet inteiro
    if ( element == null || string.IsNullOrEmpty( element.ParentId ) ) return;
    var newSelection = Document.GetElement( element.ParentId ) ?? Document.GetRoot();
    Execute( new SuiDeleteElementCommand( element.Id ) );  // command singular
    SetSelected( newSelection );
}
```

`element ??= Selected` resolve só pro primary. Call sites afetados:
- `OnShortcutDelete()` → `DeleteElement()` sem arg → deveria ser batch
- Edit menu "Delete" → `DeleteElement()` sem arg → deveria ser batch
- `CutElement` → `DeleteElement(el)` com arg → single (correto — cut é 1 elemento)
- Hierarchy right-click → `DeleteElement(el)` com arg → single

### Possíveis caminhos de resolução

1. **Novo command `SuiDeleteElementsCommand`** (batch, plural) — espelha o padrão de `SuiAlignElementsCommand` ("single undo entry covers every element"). Recebe `IEnumerable<string>`, 1 Apply / 1 Undo. Nuances:
   - **Dedup:** se um elemento selecionado é descendente de outro também selecionado, captura só o ancestral (o subtree dele já leva o filho — evita double-capture / double-remove)
   - **Filtra root** (não deletável)
   - Captura subtree + sibling index de cada raiz-de-seleção; Undo re-adiciona respeitando ordem e índices
2. **Editar `DeleteElement()` no controller** (~15 linhas):
   - `DeleteElement(null)` → deleta `SelectedSet` inteiro via o novo command
   - `DeleteElement(specificEl)` → mantém single (preserva `CutElement` + context-menu do hierarchy)
   - Pós-delete: seleciona o pai do primary, ou root se o pai também foi deletado
3. **(opcional, refinamento)** Hierarchy right-click → Delete num elemento que faz parte de multi-seleção → deletar todos. Deferível — o bug reportado é o Del key no canvas.

### Path de teste

1. Criar `.sui`, soltar 4+ elementos no canvas
2. Shift+marquee ou Shift+click pra selecionar 3+
3. Pressionar Del → **todos os selecionados** somem (não só 1) ✓
4. Ctrl+Z → **todos voltam** num único undo ✓
5. Selecionar pai + filho juntos, Del → não crash, ambos somem, Ctrl+Z restaura subtree inteiro ✓
6. Confirmar que `CutElement` (Ctrl+X) ainda funciona em elemento único

### Arquivo relacionado

- [`Editor/SuiDesignerController.cs:310-318`](Editor/SuiDesignerController.cs) — `DeleteElement` resolve só pro primary
- [`Editor/Commands/SuiDeleteElementCommand.cs`](Editor/Commands/SuiDeleteElementCommand.cs) — command singular existente (reusar lógica de subtree capture)
- [`Editor/Commands/SuiAlignElementsCommand.cs`](Editor/Commands/SuiAlignElementsCommand.cs) — referência: padrão de batch command com single-undo
- [`Editor/SuiDesignerWindow.cs:395,800,1136`](Editor/SuiDesignerWindow.cs) — call sites de `DeleteElement`

---

## (template para próximos issues)

```
## ISSUE-XXX — Título curto

**Reportado:** YYYY-MM-DD
**Severity:** blocker | major | minor
**Status:** open | partial | investigating | external

### Sintoma
O que o usuário vê.

### Hypothesis
Onde provavelmente está a raiz no código, com referência a arquivo:linha.

### Possíveis caminhos de resolução
1. Opção A — risco/custo
2. Opção B — risco/custo

### Path de teste
Como reproduzir.

### Arquivo relacionado
[caminho:linha](caminho)
```

---

## Archive — Resolved before V1.5

Issues fechados antes do release V1.5. Cada linha resume o sintoma + aponta pro commit que aplicou o fix. Detalhe histórico (hypothesis, design alternativo, paths de teste) está preservado no git log do commit referenciado e nas versões anteriores deste arquivo.

- **ISSUE-001** — ColorPicker SV box não repintava o gradiente quando o Hue mudava (Editor.ColorPicker bug). Superseded pelo picker custom em ISSUE-003. _Resolved 2026-05-08 via Batch 3_ — see commit [`b2b4400`](https://github.com/KiKoZl1/sbox-ui-designer/commit/b2b4400) (M11-M14 canvas redesign + V1.0 finalization).
- **ISSUE-002** — Text element com vertical-align desalinhado entre canvas e runtime. Resolvido via redesign do Text para auto-size estilo UMG: `SuiTextSizeMode { Auto, Fixed, AutoHeightWrap }` + `SuiVerticalAlign`, com migration on-load preservando visual antigo. _Resolved 2026-05-08 via Batch 3_ — see commit [`b2b4400`](https://github.com/KiKoZl1/sbox-ui-designer/commit/b2b4400).
- **ISSUE-003** — `Editor.ColorPicker.OpenColorPopup` era instável (SV stale, lag, commit intermitente, multi-choice ignorado, estado inicial errado). Substituído por `SuiColorPickerPopup` + `SuiColorSwatchField` custom (SV square com Pixmap cache, Hue/Alpha sliders próprios, RGB/Hex sync, Old/New comparison). _Resolved 2026-05-08 via Batch 3_ — see commit [`b2b4400`](https://github.com/KiKoZl1/sbox-ui-designer/commit/b2b4400).
