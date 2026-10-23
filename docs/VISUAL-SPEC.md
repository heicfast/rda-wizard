# VISUAL-SPEC.md — Authoritative Visual Parameter Specification

> **Status**: SINGLE SOURCE OF TRUTH for every visual parameter of the rda-wizard UI.
> Ordering of authority when sources disagree: **QSS/themdark.ini hard evidence → VLM corpus
> consensus → atlas coordinates → [ADAPTATION] design decision** (rationale required).
> Machine-readable source of truth: `design/tokens.json` (+ generator
> `design/generate-tokens.mjs` → XAML dictionaries + C# metrics). This document is the
> human-readable register; the JSON is what CI regenerates from — **they must stay in sync**
> (CI job `design-tokens` verifies the generated XAML is byte-stable).
>
> Evidence keys: `QSS L####` = og-codebase/style.css line · `TDI n##/d##` = themdark.ini
> normal/dark slot · `VE §…` = VISUAL-EVIDENCE.md · `QE §…` = QSS-EVIDENCE.md ·
> `A35/A05/A21/…` = research artifacts · `[ADV]` = deliberate adaptation (rationale given) ·
> `EST` = estimated, unresolved (tracked).

---

## 1. Screenshot corpus manifest

63 captures across 7 areas; full per-shot extraction in **VISUAL-EVIDENCE.md §1/§3**. Mapping:

| Corpus area | Pages covered (UI-MAP IDs) | Key states extracted |
|---|---|---|
| 01-splash (2) | P00 | enumerating; stuck (Wine) |
| 02-landing (21) | P02, P02a–P02e, P30, P36, P32 | baseline, tiles+selection, AI panel, solution wizard 1–3/3, activate (3 states), hamburger open, 5 sidebar pages |
| 03-scan-phase (7) | P06, P08 | 21%/34% mid-scan, 210s deep, paused, resumed, stop-confirm, post-stop-attempt |
| 04-scan-complete (2) | P08a | celebration ×2 runs |
| 05-results (14) | P06, P37 | baseline ×3, filter open ×2, path dropdown, type transition, row selected ×3, context menu ×3, search-gate |
| 06-preview (2) | P18a | live panel; HTML source render |
| 07-recovery (15) | P23, P24, P25, P28, P41 | destination ×6 (local/cloud/folder/early/repair-off/clean), entry, insufficient-space, retention ×4, after-ESC |

## 2. Token system (the complete register)

### 2.1 Architecture

- **Theme-independent tokens** (`Tokens.xaml`): spacing, sizes, radii, type ramp, motion timing,
  icon sizes, window metrics. Identical in light/dark/HC.
- **Theme token dictionaries** (`Theme.Light/Dark/HighContrast.xaml`): colors, brushes, gradients,
  elevation effects. Every key present in ALL THREE themes (parity is CI-checkable by key-set
  diff). `Brush.*` SolidColorBrush · `Gradient.*` gradient brushes · `Effect.*` shadows.
- **C# metrics** (`RdaWizard.Design/Metrics.cs`, generated): numeric tokens ViewModels need
  (row heights, pane min widths, status bar height…) — geometry only, never colors.
- Naming carried over from the v0.1.0-ui foundation (main agent's consumers use DynamicResource
  keys like `Brush.Brand.Primary`, `Space.M`…). **No renames of existing keys** — only additions.

### 2.2 Color — core semantic tokens (light / dark / high-contrast)

| Token | Light | Dark | HC | Source |
|---|---|---|---|---|
| Brand.Primary | `#0560FD` | `#5094FF` | `#0000FF`‑style system highlight | TDI 6; VE V-01 |
| Brand.Hover / Pressed / Deep | `#3D84FF`/`#004DD1`/`#0448BE` | `#68B4FF`/`#3874FF`/`#284A7F` | HC focus scheme | QSS L1836/40, TDI d64/65/27 |
| Brand.Interaction | `#2978FF` | `#A8CAFF` | link blue | census 59× |
| Brand.On | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` | QSS passim |
| Gradient.Cta | `#0C99FF→#0560FD` 135° | `#059AFD→#0CCDFF` | solid Brand.Primary | TDI 10 |
| Gradient.WindowHero | `#EFF5FF→#DEEAFF` 180° | `#1B1D1F→#18191B` | Bg.Window flat | TDI 1 |
| Gradient.Upgrade | `#89BEFF→#3992FF→#4D95FF` | darkened | solid | QSS L742 [ADV: dark uses flat] |
| Gradient.Danger | `#FCE7DF→#FFEFEA→#FFB7AB` | `#533531→#4A2324` | solid Danger | TDI 63/d63 |
| BrandTint / BrandTintStrong | `#1A0560FD`/`#330560FD` | `#1A8DBDFF`/`#338DBDFF` | 50% dither-free | TDI 13/25 |
| Status.Success (+Text/Bg) | `#30B373`/`#1E8A56`/`#E6FFF3` | `#349A37`/`#5BC98A`/`#1F3A26` | `#008000`-family | TDI 59; text [ADV contrast] |
| Status.Warning (+Strong/Bg/Text) | `#FFC10E`/`#D4760B`/`#FFF3D1`/`#413B34` | `#FFC764`/`#D6890B`/`#47433E`/`#FFC764` | `#800000`-family | TDI 11/46/45/49 |
| Status.Danger (+Alt/Pressed/Bg/Text) | `#F72C2C`/`#E74F4F`/`#D23636`/`#FDD5D5`/`#C21F1F` | `#FF5A5A`/`#FF8C8C`/`#E5484D`/`#652C2D`/`#FF8C8C` | `#FF0000`-family | TDI 36/60/38 |
| Bg.Window/Canvas/Surface/SurfaceAlt/Card/Elevated | `#FFFFFF`/`#F6F9FD`/`#F0F2F6`/`#E9EDF5`/`#FFFFFF`/`#FFFFFF` | `#1B1D1F`/`#202428`/`#252B33`/`#2F3847`/`#202428`/`#32383E` | HC system set | TDI 21/70/d2/d32/d102 |
| Bg.CardHover / SelectedRow / Overlay / Sidebar / SearchStrip / Tooltip | `#F2F7FF`/`#1A0560FD`/`#66000000`/`#E5EEFB`/`#ECF4FF`/`#E60560FD` | `#2A313A`/`#148DBDFF`/`#8C000000`/`#252B33`/`#31405C`/`#E60560FD` | HC equivalents | TDI/tint ladder |
| Text.Primary/Secondary/Tertiary/Disabled/Link/LinkPressed/OnAccent/Inverse | `#1A1A1A`/`#616777`/`#8794B3`/`#A0A6B1`/`#2978FF`/`#0F62F0`/`#FFFFFF`/`#FFFFFF` | `#F1F5FF`/`#B0B5C0`/`#7B808A`/`#596680`/`#A8CAFF`/`#669EFE`/`#FFFFFF`/`#0B1220` | HC system | census; TDI d3/17/28/96/26/105 |
| Border.Hairline/Default/Strong/Selected/Focus/Glass/Danger | `#DFE6ED`/`#E7E7E7`/`#C1CCDF`/`#0560FD`/`#0560FD`/`#590560FD`/`#F72C2C` | `#313237`/`#404C63`/`#596680`/`#5094FF`/`#669EFE`/`#338DBDFF`/`#FF5A5A` | HC system | QSS L1639; census 82×; TDI d40/96/d24 |
| Tint ladder T1–T5 | `#F2F7FF`/`#E9EDF5`/`#E0EAFF`/`#C4D7FF`/`#CCD8ED` | `#DAE7FF`/`#284A7F`/`#338DBDFF`/`#4D6E97`/`#CCD8ED`→dk | HC: 2-step | census A21 §4 |
| Confidence Excellent/Partial/Overwritten/MetadataOnly/Unknown (Bg/Fg) | see DESIGN-SYSTEM §2.7 | dark pairs | HC outlines+glyphs | [ADV] 4-tier from corpus hues |
| Health Good/Caution/Bad/Unknown | `#30B373`/`#FFC10E`/`#F72C2C`/`#A0A6B1` | `#349A37`/`#FFC764`/`#FF5A5A`/`#596680` | HC | A19; TDI |
| Chart 1–8 | `#2978FF`/`#FF7342`/`#0560FD`/`#89C5DA`/`#FFB025`/`#30B373`/`#8794B3`/`#A0A6B1` | +20%L variants | HC-safe | [ADV] A09 §4 + TDI |
| Disabled.Fill / Text | `#DEE1ED`/`#FFFFFF`→`#A0A6B1` | `#434851`/`#596680` | HC hatch | QSS L603/9; TDI d9 |

### 2.3 Typography

| Token | Value | Source |
|---|---|---|
| Font.Family (UI) | `Segoe UI Variable Display, Segoe UI, Microsoft YaHei UI, Meiryo UI, Malgun Gothic` | CFG [style] + [ADV] variable-first |
| Font.Family.Body | `Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI, Meiryo UI, Malgun Gothic` | same |
| Font.Family.Mono | `Consolas, Cascadia Mono, Courier New` | [ADV] |
| Font.Display 28/34 ·700 | splash title 24 [QSS L2182] + DisplayLarge 28 [ADV] | |
| Font.Title 22/28 ·600 | QSS L1661 | |
| Font.Subtitle 16/22 ·600 | QSS L1676 | |
| Font.Body 14/20 ·400 ·600(strong) | QSS L6; semibold dominance A21 §4 | |
| Font.CaptionStrong 13/18 ·600 | QSS L17438 | |
| Font.Caption 12/16 ·400 | QSS L2188 | |
| Font.Micro 11/14 ·600 | [ADV] badges only | |
| Numerals | `Typography.NumeralAlignment=Tabular` on counters/sizes; Consolas fallback | [ADV] from DRW counter rendering |
| HC | system font, no variable face (compat) | [ADV] |

### 2.4 Spacing, sizing, radius, elevation (theme-independent)

Space: `2/4/8/12/16/20/24/32/40/48` (4-grid [ADV]; DRW was 6/12/20/24/31/36/40/45/48 — A05 §5).
Heights: XS 32 · S 28 · M 32 · L 36 · Header 40 · Row 44 · TitleBar 40 · Toolbar 48 · StatusBar 88 ·
SidebarRow 48 · MenuItem 36 · DialogButton 28 (QSS L604/1827/3620/17419/17436; [ADV] on 40 titlebar
vs DRW 35, 36 menu vs 43.5, safe targets ≥32). Radii: `4/6/8/12/20/999` (VE §2.3 histogram 1:1).
Widths: Sidebar 220/48 · Content.MaxWidth 899 (QSS L1737) · Coaching 720 [ADV] · Window
1240×840/800×600 (CFG). Icon sizes: `16/20/24/32/48/72/160` (IRX standards). Strokes: hairline 1 ·
control border 1 · focus ring 2 (+2 offset, [ADV] — DRW had none, QSS L18).
Elevation: 5-level DropShadow ladder on `#04052B` light / border-driven dark (QE §2.4; QSS L1448/
1761): Card(2,8,.08) · CardHover(4,12,.12) · Flyout(8,24,.16) · Dialog(16,48,.24) · Window(20,60,.30).

### 2.5 Motion tokens

| Token | ms | Easing | Source |
|---|---|---|---|
| Motion.PageTransition | 300 | CubicEase-out (0.1,0.9,0.2,1) | [ADV] DRW instant (ARCH05 §3) |
| Motion.PanelOpen/Close | 250/200 | fade+scale .96→1 | [ADV] |
| Motion.Hover/Press | 120/80 | color-only / scale .98 | [ADV] |
| Motion.ProgressSmoothing | 300 | ease-out | sector jumps DYN-scan §7 |
| Motion.ShimmerLoop | 1600 | band 40% width | replaces PNG strips (A01) |
| Motion.CountUp | 250 | tabular tick | DYN-scan §7 |
| Motion.TreeExpand | 200 | height+fade | [ADV] |
| Reduce motion | →0 / shimmer→2Hz 10% dip | `SystemParameters.ClientAreaAnimation` | [ADV] WCAG 2.3.3 |

## 3. Per-screen visual specs (our 10 shipped pages + dialogs)

Each spec names the layout skeleton, key metrics, and the states that must be designed. Full XAML
comps: `src/RdaWizard.App/DesignSpecs/<Page>.xaml`.

| Page | Skeleton & key metrics | States (all as visual designs) |
|---|---|---|
| **Splash** | 420×280 card, r20, WindowHero bg, logo 80, title 24, status 12, progress hairline 2px full-bottom, Elevation.Dialog | enumerating · slow-disk (animated dots) · stuck/timeout (retry link) |
| **Home** | sidebar 220 + canvas; title 22; sections 16/tertiary; DriveCard 192w grid gap 12 (icon 32, name BodyStrong, capacity bar 156×4 r2, badges 22×18 + TRIM pill); SessionBanner 48 strip | loading (skeleton cards) · empty (no drives + coach) · drive-lost (link → LostPartition) · session-present · scanning-locked |
| **ScanConfig** | mode cards 3-up 220×140 r8, icon 48, radio-select ring; deep-scan explainer | selected · hover · disabled (engine-missing) · BitLocker-locked |
| **ScanProgress** | hero: circular gauge 96 (ring 8, band by phase) + stats block (tabular counters) + Searching strip 32 SearchStrip bg; bottom: pause/stop 32 + Recover CTA 36; results 3-pane fills live | running · paused (red strip + frozen gauge) · deep-phase (darker strip) · stop-confirm dialog (stat tiles 2-up, risk copy, danger/primary pair) · disconnected (ErrorState overlay) |
| **Results** | toolbar 48 (nav cluster + breadcrumb + view-toggle + filter + search 220); 3-pane tree 240 / list flex (row 44, header 40, checkbox col 28) / preview 300; status bar 88 | baseline · row-selected (checkbox fill + contextual CTA) · search-active (chip + breadcrumb swap) · filter-open (bar 56 + dropdowns) · type-tab transition (tree rebuild skeleton) · 1M-row (virtualized, slim scrollbar 6) · empty-search (strategy coach) |
| **Recovery (destination)** | modal 560×520 r12; tabs Local/Cloud 32; destination rows 56 (drive icon 32 + capacity + fit verdict); folder-picker row; repair toggle; Save 28 r4 disabled→enabled | local · cloud · folder-picked · source-overlap warning row · insufficient-space gate (Danger gradient frame) · validating (spinner on Save) |
| **Completion** | celebration card 540×380 r20; stats grid 2×4 (tabular); Go-to-Recover CTA 36 centered 0.48/0.66 window-fraction (A35 §7); confetti-free (restraint P1) | complete · partial-failures (count + log link) |
| **Health** | gauge 96 + band label; SMART table (header 40, row 32, tabular); caution callout Banner | good · caution · bad · unknown · driver-missing (ErrorState) |
| **Repair** | drop-zone card 320 r8 dashed Border.Strong; queue rows 56 with per-file status pill | empty (drop coach) · queued · repairing (per-file progress) · done/failed |
| **Settings/About** | settings list 32 rows + toggle cluster; About: logo lockup 64 + version Caption + links | default · theme-preview chips · language grid (22 locales) |

Dialog family (all): modal 540 default / 720 max, r12, Elevation.Dialog, scrim `Bg.Overlay`,
header icon 48 + Title 18/600, body Body max-w 720, footer right (primary + secondary 28 r4),
ESC-dismissible except acknowledgment gates (double-confirm on destructive).

## 4. Component inventory & state matrix

**Inventory (60 components)** — full specs in DESIGN-SYSTEM §8 (existing 17) + this session's
additions (43): Button(primary/secondary/outline/ghost/danger/link) · IconButton · SplitButton ·
Toggle/CheckBox/Radio/Switch · TextField/SearchField/ComboBox/NumericStepper/Slider ·
Segmented · Tabs · WizardStepper · Breadcrumb · Badge/ConfidenceBadge/StatusPill · Tooltip ·
Popover · ContextMenu · Dialog(confirm/danger/ack) · Toast · Banner(info/warn/error/success) ·
Sidebar/NavRail · DriveCard/MediaTile · ScanModeCard · FileTypeFilterChip · ResultTree ·
FileListView(detail+icon) · PreviewPane · RecoveryQueue · ProgressSurface(linear/determinate/
indeterminate/segmented/circular) · HealthGauge · SMARTTable · SessionPanel · LogPanel ·
EmptyState · LoadingSkeleton · ErrorState · DataTable · ScrollBar · ResizeGrip · Divider ·
Card/Panel/Section · Overlay/Mask · FocusRing · Stepper · StateSurface.

**State matrix (every interactive component)**: normal · hover · pressed · focus · disabled ·
error(where meaningful) · selected/toggled(where meaningful) · loading(where meaningful).
Non-interactive surfaces: default + (page-level) loading/empty/error/partial/disconnected/
permission-denied/driver-missing/corrupt-session/destination-overlap.

## 5. Acceptance envelope (diff gate for the VLM refine loop)

A surface is **PASS** only if all 20 quality parameters hold (VISUAL-ACCEPTANCE.md grid) AND
numeric diffs vs this spec are inside:

| Metric class | Tolerance (vs spec/comp) |
|---|---|
| Color (fill/border/text) | ΔE(RGB) ≤ 8 per token; gradient stops exact |
| Typography size/weight | ±0px (exact token); family per fallback matrix |
| Control height/width | ±2px (DIP, at 100%); icons exact-token |
| Spacing/padding/margin | ±2px cumulative per container edge |
| Corner radius | ±1px |
| Alignment | 0px off-grid (4px grid; 0.5px snapping for hairlines at 125/175%) |
| Shadow | blur ±4px, opacity ±4pp, offset ±1px |
| Contrast (token pairs) | ≥ 4.5:1 body/UI text (≥ 3:1 large ≥18px & UI borders); critical counters ≥ 7:1 target |
| DPI | 100/125/150/175/200% — no blurry border, no clipped glyph, hairlines snapped |
| Theme parity | key-set equality light/dark/HC; no element renders default gray |

**Diff protocol**: main-UI-agent page → FlaUI screenshot on Windows runner → VLM side-by-side
against both (a) the reference corpus shot and (b) our DesignSpec comp → annotated diff + token
delta table → issue → fix → re-shoot. Iterate to envelope.

## 6. ESTIMATED register & interim assets

| Item | Status | Track |
|---|---|---|
| Dark-theme golden images | spec complete (TDI); renders unverified | Windows-runner gallery capture (T18) |
| Icon-view grid metrics | from IRX 160 standard | verify on gallery |
| ContextMenu 36 vs DRW 43.5 | [ADV] grid discipline | accept, note in gallery |
| Chart accent hues 4/8 | [ADV] composite | a11y non-color rule covers |
| File-tile art v1 (2-letter ext tiles) | INTERIM | v2 bespoke set — INTERIM-ASSETS.md |
| Fluent-icon-derived chrome set (MIT) | INTERIM source, redrawn paths | v2 bespoke — INTERIM-ASSETS.md |
| Empty-state illustrations (geometric) | ORIGINAL | — |
