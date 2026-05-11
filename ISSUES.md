# Sbox UI Designer — Known Issues

Bugs encontrados em produção que ainda **não estão resolvidos** (ou estão resolvidos parcialmente / com caveats). Resolvidos confirmados saem daqui.

Convenção:
- **Severity** — `blocker` quebra workflow / `major` atrapalha mas tem workaround / `minor` cosmético
- **Status** — `open` / `partial` / `investigating` / `external` (bug em código de fora)
- **Hypothesis** — onde provavelmente está a raiz, baseado em leitura do código

---

## ISSUE-001 — ColorPicker SV box não repinta gradiente quando Hue muda

**Reportado:** 2026-05-08
**Resolvido:** 2026-05-08 via ISSUE-003 (substituição do `Editor.ColorPicker` por implementação custom)
**Severity:** minor
**Status:** ✅ RESOLVED — superseded by ISSUE-003

### Sintoma

Abre o picker do Tint/BackgroundColor/etc. Move o slider de Hue (arco-íris) para uma cor nova (ex.: vermelho → roxo). O hex e o slider mostram a nova cor (`#4416EC`), mas a caixa de Saturação×Brilho em cima continua renderizando o gradiente da cor antiga (gradiente vermelho). A bolinha branca dentro da caixa SV fica na posição correta de S/V, mas o "fundo" não foi repintado.

Confirmado pelo usuário em 2026-05-08 via screenshots.

### Caveat importante

O **documento recebe a cor correta** em tempo real (live commit do nosso fix anterior). O canvas atrás do picker mostra a cor nova. É puramente um glitch de paint do widget interno do editor.

### Hypothesis

`Editor.ColorPicker.OpenColorPopup` retorna um `picker` que internamente tem múltiplos sub-widgets (SV box, hue slider, alpha slider, hex field). O callback `c => ...` é chamado quando QUALQUER um deles muda a cor, mas a caixa SV não está se invalidando quando o hue muda — provavelmente a SV box escuta seu próprio valor `c.s` / `c.v` mas não escuta `c.h` para repintar o gradiente de fundo.

### Possíveis caminhos de resolução

1. **Tentar `picker.Update()` no callback** — força repaint do widget inteiro. Hack barato.
   - Risco: se a SV box tiver cache interno do gradiente baseado em hue antigo, Update() pode não invalidar.
2. **Tentar acessar o SV sub-widget via reflection e chamar Update() nele** — mais cirúrgico.
3. **Substituir por nosso color picker** — V2 work. Razor + SCSS + 3 sliders + SV canvas. ~1 dia.
4. **Reportar pra Facepunch** — fix do editor downstream.

### Path de teste

- Detalhes panel → seleciona um Image element → abre Tint
- Move slider de Hue de vermelho pra roxo lentamente
- Observa se a caixa SV repinta o gradiente (deveria virar gradiente roxo)

### Arquivo relacionado

[Editor/Widgets/SuiDetailsWidget.cs:530-562](Editor/Widgets/SuiDetailsWidget.cs) — `AddColorRow` / `swatch.Clicked`

---

## ISSUE-002 — Text element: alinhamento vertical desalinhado entre canvas e runtime preview, +redesign pra auto-size

**Reportado:** 2026-05-08
**Resolvido:** 2026-05-08 (Batch 3)
**Severity:** major
**Status:** ✅ RESOLVED — `SuiTextSizeMode { Auto, Fixed, AutoHeightWrap }` implementado.

### Solução implementada

- `SuiEnums.cs` + `SuiElementProps.cs`: enums novos `SuiTextSizeMode`, `SuiVerticalAlign`. Props `TextSizeMode` (default Auto) e `VerticalAlign` (default Top).
- `SuiDocumentMigration.cs` (novo): `Apply(doc)` na carga do .sui converte Text legacy com W/H>0 para Fixed mode (preserva visual existente).
- `SuiLayoutSolver`: `MeasureAutoTexts` mede via `Editor.Paint.MeasureText` no início de Solve; `ResolveAbsoluteRectWithOverride` usa medição como W/H pra Auto Text. Flex children Auto Text também medem via closure intrinsic.
- `SuiCanvasRenderer.ResolveTextFlag`: Auto → LeftTop, AutoHeightWrap → align horizontal só, Fixed → matriz 3×3 TextAlign × VerticalAlign mapeada nos 9 TextFlags.
- `SuiScssGenerator`: Auto emite `width/height: auto` + `white-space: nowrap`. AutoHeightWrap emite `height: auto` + `white-space: normal`. Fixed emite `display: flex; flex-direction: column; justify-content: <map>` pra vertical-align via flex.
- `SuiDetailsWidget.BuildPropsSection`: dropdown Size Mode + condicionalmente mostra Align (não em Auto), VerticalAlign (só em Fixed). Note de help embaixo explicando o modo.

### Documentos antigos

A migration roda na carga, idempotente. Texts pré-existentes que tinham W/H definidos automaticamente viram Fixed → preservam visual. Novos Texts criados pela palette nascem em Auto (cresce com conteúdo).

---

<details>
<summary>(ORIGINAL DESIGN — kept for archive)</summary>

### Sintoma

Text element com Height maior que a altura visual da fonte:
- **Canvas** desenha o texto no **centro vertical** do rect
- **Runtime preview** desenha o texto no **topo** do rect

Visualmente: o que aparenta estar centralizado no canvas (sobre uma imagem/bar de fundo) sai pra baixo no preview (texto fica abaixo da imagem). Validado com Text rect 240×128, anchor MiddleCenter, fonte ~14-20px.

### Hypothesis (root cause)

[SuiCanvasRenderer.cs:204](Editor/Canvas/SuiCanvasRenderer.cs#L204) — `MapTextAlign` mapeia todos os `SuiTextAlign` para `TextFlag.*Center` (vertical centralizado):

```csharp
SuiTextAlign.Left   => TextFlag.LeftCenter,
SuiTextAlign.Center => TextFlag.Center,
SuiTextAlign.Right  => TextFlag.RightCenter,
```

[SuiScssGenerator.cs:325-336](Code/Generation/SuiScssGenerator.cs#L325) — para Text só emite `font-*`, `color`, `text-align` (horizontal). Não emite nada que controle alinhamento vertical → CSS default = top-aligned.

A schema **não tem** propriedade `VerticalAlign` (só `TextAlign` que é horizontal). Daí a divergência: canvas escolheu Center hardcoded, runtime não tem como controlar e fica Top.

### Decisão de design (validada com usuário 2026-05-08)

Em vez de só adicionar `VerticalAlign`, **redesign do Text** pra auto-size estilo UMG/UE — caixa cresce com o conteúdo, sem espaço extra → sem necessidade de alinhamento vertical pra maioria dos casos.

#### Schema proposto

```csharp
public enum SuiTextSizeMode
{
    Auto,            // default — W/H derivam do texto. Sem wrap. Sem align vertical.
    Fixed,           // user define W/H. Suporta TextAlign + VerticalAlign.
    AutoHeightWrap,  // user define W (max-width). H cresce com linhas. TextAlign horizontal só.
}

public enum SuiVerticalAlign { Top, Center, Bottom }

// SuiElementProps
public SuiTextSizeMode TextSizeMode { get; set; } = SuiTextSizeMode.Auto;
public SuiVerticalAlign VerticalAlign { get; set; } = SuiVerticalAlign.Top;  // só usado em Fixed
```

#### Comportamento por modo

| Modo | W/H | Wrap | Vertical Align | Use case |
|---|---|---|---|---|
| **Auto** (default) | derivado do texto | não | n/a | "só digito e funciona" — usa pra labels, botões, indicadores |
| **Fixed** | user define | não | Top/Center/Bottom | caixa fixa estilo botão grande, com texto centralizado |
| **AutoHeightWrap** | W fixo, H auto | sim | n/a | parágrafo/descrição com largura definida |

### Plano de implementação

#### A. Schema (Code/Runtime)

- `SuiEnums.cs` ou novo arquivo: `SuiTextSizeMode`, `SuiVerticalAlign` enums.
- `SuiElementProps.cs`: adicionar `TextSizeMode` (default Auto) e `VerticalAlign` (default Top).
- `SuiSelfTest.cs`: testes pra round-trip dos novos enums.
- Migração: documentos antigos sem o campo deserializam como `Auto`. **Caveat de retrocompat:** Text antigo com W/H fixos vai virar Auto e PERDER o tamanho fixo no canvas (mas runtime não muda — já era Top-aligned). Mitigação: on-load, se Text element tem W>0 ou H>0, força modo `Fixed` em vez de Auto.

#### B. Canvas solver (`SuiLayoutSolver`)

- Quando elemento é Text + Auto: medir texto via `Editor.Paint.MeasureText(font, fontSize, weight, text)` antes de calcular rect. Override do W/H de `el.Layout` com a medição.
- **Risco crítico:** confirmar se `MeasureText` é chamável FORA do contexto de Paint (durante solve pass que roda antes de OnPaint). Se não for, soluções:
  - (a) Cachear medições por `(text, font, size, weight)` — primeira render mede e guarda
  - (b) Fazer solver chamado durante OnPaint (já é hoje)
  - (c) Estimar largura por `text.Length * fontSize * 0.55f` (heurística — fallback grosseiro)
- AutoHeightWrap: medir altura usando largura fixa, contar linhas via word-wrap simulado.

#### C. Canvas renderer (`SuiCanvasRenderer`)

- Auto: `TextFlag.LeftTop` fixo (rect == texto, alinhamento dentro é trivial)
- Fixed: `MapTextAlign(h, v)` 2D switch (ver discussão anterior na conversa)
- AutoHeightWrap: só TextAlign horizontal, vertical sempre Top

#### D. SCSS generator

- Auto: NÃO emitir `width`/`height`. Emitir `white-space: nowrap`.
  - **Risco:** s&box engine pode forçar 100% ou 0 em flex children sem width/height. Testar com elemento simples.
- Fixed: emitir W/H + flex tricks pra VerticalAlign:
  ```scss
  display: flex;
  flex-direction: column;
  justify-content: flex-start | center | flex-end;
  ```
  - **Risco:** s&box trata atributo `text` de forma especial — flex pode ou não afetar text positioning. Fallback: `line-height: <Height>px` (single-line apenas).
- AutoHeightWrap: emitir `max-width`, omitir height. `white-space: normal`. Texto quebra naturalmente.

#### E. Details panel UX

- Adicionar dropdown `Text Size Mode` no Props section.
- Por modo:
  - **Auto**: ocultar W, H, VerticalAlign. Mostrar nota "Width/Height auto from text"
  - **Fixed**: mostrar W, H, TextAlign, VerticalAlign (todos visíveis)
  - **AutoHeightWrap**: renomear W pra "Max Width", ocultar H, mostrar TextAlign, ocultar VerticalAlign

#### F. Drag handles (`SuiCanvasViewport` / `SuiCanvasWidget`)

- Modo Auto: ocultar handles de resize (não faz sentido resize). Manter só drag de move.
- Modo Fixed: handles normais.
- Modo AutoHeightWrap: mostrar só os 2 handles E e W (resize horizontal apenas).

### Dúvidas abertas pra validar antes de codar

1. **MeasureText fora de Paint:** chama `Editor.Paint.MeasureText(...)` fora de OnPaint funciona? Se não, opção (a) cache ou (b) solver dentro de OnPaint? — investigar com `gh search code "MeasureText" --repo Facepunch/sbox-public`.
2. **CSS auto-size em flex children s&box:** `width: auto` / `height: auto` num panel filho de outro panel-flex realmente expande pra max-content? Ou força 100%/0?
3. **Button labels:** estende `TextSizeMode` pra Button também? Recomendação atual = NÃO, Button continua manual (semântica de "área clicável" justifica tamanho fixo). Confirmar com user antes.
4. **AutoHeightWrap V1 ou V2:** vale incluir ou ataco só Auto + Fixed por agora? Recomendação = só Auto+Fixed pra V1, AutoHeightWrap quando alguém pedir.
5. **Migração on-load:** force `Fixed` em Text antigos com W>0/H>0? Ou deixa virar Auto e o user re-cria? Recomendação = forçar Fixed (preserva visual existente).

### Workaround temporário até implementar

User pode:
- (a) Diminuir Height do Text element até "encolher" no texto (visualmente equivalente a Auto, manual)
- (b) Aceitar que canvas mostra centralizado mas runtime mostra no topo — design no preview, não no canvas, pra textos grandes

### Path de teste pós-implementação

1. Drop Text via palette → modo Auto default → digitar texto → caixa cresce visualmente em sync com texto, no canvas E no preview
2. Drop Text → mudar TextSizeMode pra Fixed no Details → W/H aparecem editáveis → set H=128 → mudar VerticalAlign entre Top/Center/Bottom → conferir que canvas e preview ficam idênticos em cada modo
3. Documento antigo (.sui pré-migração): abrir → Text antigo deve virar Fixed (não Auto) preservando visual

### Arquivos relacionados

- [Code/Runtime/SuiEnums.cs](Code/Runtime/SuiEnums.cs)
- [Code/Runtime/SuiElementProps.cs](Code/Runtime/SuiElementProps.cs)
- [Editor/Canvas/SuiLayoutSolver.cs](Editor/Canvas/SuiLayoutSolver.cs)
- [Editor/Canvas/SuiCanvasRenderer.cs](Editor/Canvas/SuiCanvasRenderer.cs)
- [Editor/Widgets/SuiDetailsWidget.cs](Editor/Widgets/SuiDetailsWidget.cs)
- [Code/Generation/SuiScssGenerator.cs](Code/Generation/SuiScssGenerator.cs)
- [Editor/Widgets/SuiCanvasWidget.cs](Editor/Widgets/SuiCanvasWidget.cs)

### Estimativa de esforço

~30% mais trabalho que o quick-fix vertical-align puro, mas resolve o sintoma E moderniza UX do Text (alinha com UMG/UE convention). Confirmado com user 2026-05-08 que vale o investimento.

</details>

---

## ISSUE-003 — Color picker do editor é instável: substituir por implementação custom

**Reportado:** 2026-05-08
**Resolvido:** 2026-05-08 (Batch 3)
**Severity:** major
**Status:** ✅ RESOLVED — `SuiColorPickerPopup` + `SuiColorSwatchField` substituem o `Editor.ColorPicker` no Details panel.

### Solução implementada

- `Editor/Widgets/SuiColorPickerPopup.cs` — popup com SV square (saturation × value, hue-driven gradient cacheado em Pixmap), Hue slider (rainbow horizontal), Alpha slider (checkerboard + linear gradient), Hex input, RGB inputs (0-255), Old/New comparison swatches, OK/Cancel.
- `Editor/Widgets/SuiColorSwatchField.cs` — substitui o LineEdit + swatch button no Details. Full-width swatch que mostra a cor diretamente (com hex overlaid em cor de contraste). Click abre picker, right-click menu com Copy/Paste/Clear hex. Empty state mostra "(no color — click to set)".
- Estado interno em `ColorHsv` — todas as subwidgets leem/escrevem da mesma fonte, sem round-trips lossy via hex/RGB.
- Pixmap caches: SV gradient regenera só quando Hue muda; Hue slider gradient cacheado static (não depende de estado).
- `SuiDetailsWidget.AddColorRow` agora cria um `SuiColorSwatchField` em vez do par LineEdit+Button.

### Sintomas resolvidos

1. ✅ SV box stale ao mudar Hue — agora invalida cache + repinta sempre
2. ✅ Lag input — paint pipeline próprio, sem race conditions
3. ✅ Commit intermitente — callback live + safety final no EditingFinished
4. ✅ Múltiplas escolhas na mesma sessão — cada mudança commita independentemente
5. ✅ Estado inicial errado — ColorHsv conversion + RefreshAllFromState no construtor garante sincronização

---

<details>
<summary>(ORIGINAL DESIGN — kept for archive)</summary>

### Sintomas observados (todos no `Editor.ColorPicker.OpenColorPopup`)

1. **SV box stale (= ISSUE-001)** — gradiente do quadro Saturação×Brilho não repinta quando Hue muda. Resolve fechando + reabrindo.
2. **Lag input** — mover sliders ou clicar dentro da SV box demora pra refletir; sensação de "engasgado" especialmente em monitores com refresh rate alto.
3. **Commit intermitente** — escolho uma cor, às vezes não persiste no documento; precisa abrir + fechar + escolher de novo.
4. **Múltiplas escolhas na mesma sessão do popup** — abro picker, escolho azul, sem fechar tento mudar pra vermelho, **só o azul commita**. A segunda escolha em diante é silenciosamente ignorada (parece que `EditingFinished` só dispara na primeira mudança).
5. **Estado inicial errado** — abre o picker e a posição inicial dos sliders/SV cursor não bate com a cor atual do hex/swatch.

### Hypothesis

Bugs de race condition + paint scheduling dentro do widget `Editor.ColorPicker`. Múltiplos sub-widgets (SV box, hue slider, alpha slider, hex field, RGB) que escutam parcialmente uns aos outros, com cache interno de gradiente que não invalida em todos os caminhos.

Não é bug nosso — `Editor.ColorPicker.OpenColorPopup` é uma chamada estática direta. Nosso wrapper em [SuiDetailsWidget.cs:530-562](Editor/Widgets/SuiDetailsWidget.cs#L530) só registra callbacks de mudança de cor.

### Decisão de design

**Construir nosso próprio color picker como Razor + SCSS dentro do designer.** O editor s&box é Qt-driven (limitado a customizar), mas o nosso designer já é Razor+SCSS+C# nativo. Fazer um picker customizado nos dá:

- Controle total de UX (fix dos 5 sintomas)
- Live preview garantido (já temos pipeline DocumentChanged → SelectionChanged → repaint)
- Integração com nosso command stack (undo/redo limpo)
- Possibilidade de features extra: paletas salvas, eyedropper, gradient/recent colors, HSV vs RGB tabs, alpha slider robusto

### Componentes propostos

```
SuiColorPickerPopup (Razor)
├── SV Square (canvas paint custom, pinta gradiente baseado em hue atual)
│   └── Crosshair pra S/V atual
├── Hue Slider (vertical bar, gradient arco-íris, knob arrastável)
├── Alpha Slider (checkered bg + color tint, knob arrastável)
├── Color Preview (current vs old, lado-a-lado)
├── Hex Input (LineEdit com validação)
├── RGB Inputs (3 NumberInputs 0-255)
├── HSV Inputs (opcional, 3 NumberInputs)
└── Footer:
    ├── Eyedropper button (V2 — captura pixel da tela)
    ├── Recent colors strip (V2 — últimos 8 escolhidos)
    └── OK / Cancel buttons (commit final ou rollback)
```

### Plano de implementação

#### A. Novo widget `SuiColorPickerPopup`

- Arquivo: `Editor/Widgets/SuiColorPickerPopup.cs` (Editor `Widget` ou `Window` modal/popup)
- Layout: Column principal, sub-widgets via Layout.AddRow
- Estado: `Color _current`, `Color _initial` (pra cancel rollback)
- Eventos: `event Action<Color> ColorChanged` (live), `event Action<Color> ColorCommitted` (OK/close)

#### B. SV box (parte mais técnica)

- Custom `Widget` subclass com OnPaint override.
- A cada paint: pinta gradiente HSV (variando S no eixo X, V no Y) usando o Hue atual.
  - Implementação: cache `Pixmap` 256×256, redesenha apenas quando Hue muda.
  - Gradiente via duas passadas de Paint.SetBrush + DrawRect com gradient brushes (se API existir) ou via per-pixel write em Pixmap.
- Mouse drag dentro: atualiza S/V via `S = mouseX/width`, `V = 1 - mouseY/height`.
- Crosshair desenhado em (S*width, (1-V)*height).

#### C. Hue slider

- Custom Widget vertical (32×256 ou similar).
- Gradiente arco-íris via cache Pixmap (uma vez só, hue não muda).
- Mouse drag → `H = mouseY / height` (0..1).
- Knob desenhado em (centerX, H*height).

#### D. Alpha slider

- Igual hue slider mas horizontal (256×24).
- BG: checkerboard (já temos código no canvas viewport pra reusar).
- Foreground: gradient transparente → opaco da cor atual.
- Drag → `A = mouseX / width`.

#### E. Hex/RGB/HSV inputs

- LineEdit com EditingFinished pra hex.
- 3x SuiNumberInput (existe ou criar) pra RGB inteiros 0-255.
- Sync bidirecional: mudar qualquer um atualiza os outros + a SV box + sliders.

#### F. Wiring no Details

Substituir [SuiDetailsWidget.AddColorRow](Editor/Widgets/SuiDetailsWidget.cs#L483) — em vez de `ColorPicker.OpenColorPopup`, abrir nossa popup. Manter o callback que faz `onCommit`. Live commit já funciona (do nosso fix anterior).

#### G. Cleanup

Remover dependência do `Editor.ColorPicker` do nosso codebase. Marcar ISSUE-001 como resolvido (porque o picker novo não tem o bug do SV box stale).

### Riscos técnicos

1. **Pintar gradiente HSV efficiently** — no editor Paint API, gradientes lineares existem (`Paint.SetBrush` aceita `Color` + gradient stops? não confirmado). Fallback: gerar Pixmap 256×256 via loop pixel-a-pixel uma vez por hue change, cachear, desenhar com `Paint.Draw(rect, pixmap)`.
2. **Mouse capture durante drag** — o widget precisa capturar mouse pra slider drag fora dos bounds. Padrão Qt funciona automático com `MouseTracking = true` + `OnMouseMove`.
3. **Popup positioning + dismissal** — clicar fora deve fechar + commitar (ou cancelar?). UX decision: clicar fora = commit final (cor atual vira definitiva, undo restaura pra inicial).
4. **Performance da SV box em monitores grandes** — 256×256 = 65k pixels, paint a cada hue change. Em hue scrub rápido pode laggar. Mitigação: gerar em background thread + double-buffer.

### Estimativa de esforço

~1-1.5 dia. Inclui: 4 sub-widgets custom, layout, math HSV↔RGB, mouse handling, wiring no Details. Hex/RGB inputs reusam editores existentes do Details.

### Path de teste pós-implementação

Cobrir os 5 sintomas reportados:
1. Mudar Hue de vermelho pra roxo → SV box repinta gradient roxo na hora ✓
2. Arrastar slider rapidamente → sem lag perceptível ✓
3. Selecionar cor → fechar picker → reabrir → cor preservada no documento ✓
4. Mudar cor 5x sem fechar popup → todas as 5 são commitadas (live + final) ✓
5. Abrir picker num elemento com cor azul → SV cursor + hue knob aparecem na posição azul ✓

### Decisão de PRIORIZAÇÃO

Construir esse picker antes de M12 (compile final)? Argumentos:
- **Pró:** afeta workflow diário hoje, todo elemento usa cor. M12 é importante mas é "exportação", se o user não consegue editar bem, M12 não importa.
- **Contra:** M12 é o que destrava "designer → código real". Sem ele, ninguém usa o designer pra entregar UI de verdade.

**Recomendação:** se o user vai usar o designer pra produção daily, picker primeiro. Se M12 é o último bloqueio antes do "release", M12 primeiro e picker imediatamente depois.

### Arquivos relacionados

- [Editor/Widgets/SuiDetailsWidget.cs:483-580](Editor/Widgets/SuiDetailsWidget.cs#L483) — `AddColorRow` atual
- Novo: `Editor/Widgets/SuiColorPickerPopup.cs`
- Novo (talvez): `Editor/Widgets/SuiSvSquare.cs`, `SuiHueSlider.cs`, `SuiAlphaSlider.cs`

</details>

---

## ISSUE-004 — `<label>` element ignora alpha em `background-color` rgba() em runtime Play mode

**Reportado:** 2026-05-11
**Severity:** major
**Status:** investigating

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
