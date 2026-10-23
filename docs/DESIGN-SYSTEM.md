# DESIGN-SYSTEM.md — rda-wizard Visual Language (v1.0)

> Clean-room design system for a WPF (Fluent-inspired, light/dark) data recovery wizard.
> This document reconstructs the *equivalent visual language* of EaseUS Data Recovery Wizard (DRW)
> from the RE corpus at `/home/z/my-project/easeus-drw-research/` — **without copying any
> proprietary asset**. Every claim about DRW's shipped look cites the artifact file. Extensions
> beyond the corpus (modern Fluent adaptation) are marked `[ADAPTATION]`.
>
> Corpus key (all under `re-workspace/artifacts/UIUX/` unless noted):
> - `A01` = 01-visual-inventory.md · `A05` = 05-styling-theming-layout.md · `A21` = 21-qss-property-census.md
> - `A35` = 35-redesign-reference-atlas.md · `A34` = 34-navigatebar-bottomarea-chrome.md
> - `A07`/`A08`/`A09` = home/scan/results deep dives · `A14` = cdetailpage-slots-viewmodes.md
> - `DYN-*` = dynamic-ground-truth--{landing,scan-phase,splash}-*.md
> - `QSS` = `og-codebase/style.css` (20,802 lines) · `TDI` = `og-codebase/config/themdark.ini` (118-slot palette)
> - `IRX` = `og-codebase/Resource/ImageResource.xml` · `ARCH05` = `architecture/05-uiux-architecture.md`
>
> Implementation target: WPF resource dictionaries (`Theme.Light.xaml` / `Theme.Dark.xaml`).
> All dimensions in DIPs (device-independent pixels) unless stated.

---

## 1. Design principles

**P1 — The user is in a panic moment.** Data loss is an emergency for non-technical users. The UI
must reassure, never scold: plain language ("lost files", not "orphaned MFT records"), one obvious
safe action per screen, and honest status at all times. DRW's own coaching copy validates this
posture — "Remaining data can be rescanned and recovered anytime later" on stop (A08 §3),
per-branch explainers like "Here are some files whose original name cannot be found, but are more
likely to be your desired files" (A09 §2). We keep the honesty, drop the fear-marketing
(see §12: DRW's periodic "Wait! You are at risk of losing data!" nag fired every 30–60 s, A35 §12.15).

**P2 — Progressive disclosure, two densities.** Home is *comfortable*: large tiles, generous
whitespace, few choices (DRW home: one page title + grouped tiles, A35 §5). Results is *dense*:
a 3-pane explorer that comfortably renders 1.1 M rows (DYN-scan §3: "Windows (C:) — 1.11M files"
verified live). Never mix the two registers on one screen.

**P3 — Premium-modern, Fluent-inspired chrome.** Flat surfaces, hairline borders, layered
elevation, restrained brand blue. DRW's 2021+ language is already close (4-step neutral ladder +
blue tint ladder, A21 §4); we snap it to a Fluent scale and add what DRW lacked: visible focus
rings (DRW explicitly set `outline:none` on buttons — QSS L18), consistent 4 px grid, and true
corner-radius discipline.

**P4 — Browse-while-scanning is sacred.** The scan never blocks the UI; results stream in and the
tree grows live (DYN-scan §7: "Reconstructed" branch grew 1→16 mid-scan). Every progress surface
must coexist with a fully interactive results explorer; Pause/Stop are always reachable
(A35 §6: pause at (104,698), stop at (127,698)).

**P5 — Reversible by default, destructive only on explicit acknowledge.** Stop asks before
discarding (A08 §3 stop-confirm); recovery destination blocks same-source writes with an
acknowledgment gate (A35 §10: "Data might be OVERWRITTEN and LOST PERMANENTLY…"). Destructive
actions are never the default button.

**P6 — Layout lives in tokens, not code.** DRW encoded 36 % of all QSS declarations as min/max
geometry (A21 §1: 4,240 of 11,929). We preserve that separation — but as *tokens* — so the whole
geometry can be re-skinned from resource dictionaries.

---

## 2. Color tokens

### 2.1 Palette derivation strategy

Anchors taken verbatim from the corpus, then harmonized:
- **Brand blue `#0560FD`** — the single most-used brand token in the shipped QSS (107 occurrences,
  A21 §4; TDI normal slot 6). Dark theme brand: `#5094FF` (TDI dark slot 6).
- **Interaction blue `#2978FF`** — a *second*, distinct blue DRW used for links/pressed text
  (59 occurrences, A21 §4). Kept as a separate token.
- **Neutral ladder** `#e7e7e7 → #dee1ed → #c1ccdf → #a0a6b1` (A21 §4, A05 §7) and text
  `#1a1a1a` (158×) / `#616777` (92×) / `#8794b3` / `#596680` (37×) (A21 §4).
- **Blue tint ladder** for hover/selected fills: `#f2f7ff → #e9edf5 → #e0eaff → #c4d7ff → #CCD8ED`
  (A21 §4 — a 5-step ladder we keep 1:1).
- **Dark surfaces** `#1b1d1f / #202428 / #32383e`, text `#f1f5ff`, borders `#404c63 / #596680`,
  glass tints `rgba(141,189,255,0.08–0.2)` (A05 §1 dark; TDI dark slots 1/2/15/3/40/96/24).
- Rendered CTA reads ≈ `#1B6DD0–#2C7BE5` family in pixel scans (A35 §1) because the fill is a
  gradient — we honor the *gradient*, not a flat mid-tone.
- Status hues: success `#30b373`, warning amber `#ffc10e` (+ gold `#FFB025` reserved for nothing —
  promo gold deliberately excluded, §12), danger `#f72c2c`/`#ff5a5a` (A05 §1, TDI slots 59/11/36).

### 2.2 Brand / accent

| Token | Light | Dark | DRW evidence |
|---|---|---|---|
| `Brand.Primary` | `#0560FD` | `#5094FF` | QSS census 107×; TDI n6/d6 (A21 §4, A05 §1) |
| `Brand.Hover` | `#3D84FF` | `#68B4FF` | DiskScanButton:hover QSS L1836; TDI d64 |
| `Brand.Pressed` | `#004DD1` | `#3874FF` | DiskScanButton:pressed QSS L1840; TDI d65 |
| `Brand.Deep` | `#0448BE` | `#284A7F` | CAutoExpendBtn:pressed QSS L3672; TDI d27 |
| `Brand.Interaction` | `#2978FF` | `#A8CAFF` | QSS census 59× (A21 §4); TDI d26 |
| `Brand.LinkPressed` | `#0F62F0` | `#669EFE` | DrwMessageBoxCancelBtn:pressed QSS L679; TDI d105 |
| `Brand.On` (text on brand) | `#FFFFFF` | `#FFFFFF` | QSS throughout |
| `Brand.Focus` | `#0560FD` | `#669EFE` | DRW had no focus token (outline:none, QSS L18) — TDI d109 `border:1px solid #669efe` is the dark closest cue `[ADAPTATION]` |
| `Brush.Cta` | 90° linear `#0C99FF → #0560FD` | 90° linear `#059AFD → #0CCDFF` | TDI n10/d10 (A05 §1) |
| `Brush.WindowHero` | 180° `#EFF5FF → #DEEAFF` | 180° `#1B1D1F → #18191B` | DRWMainFrame QSS L1454; TDI n1/d1 |
| `Brush.BrandTint` (hover ring) | `rgba(5,96,253,0.10)` | `rgba(198,222,255,0.10)` | TDI n23/d23; tree hover QSS L17405 |
| `Brush.BrandTintStrong` (selected) | `rgba(5,96,253,0.20)` | `rgba(141,189,255,0.20)` | TDI n24/d24; tree selected QSS L17409 |

### 2.3 Status (success / warning / danger / info)

| Token | Light | Dark | DRW evidence |
|---|---|---|---|
| `Status.Success` | `#30B373` | `#349A37` | TDI n59/d59 (A05 §1) |
| `Status.SuccessText` | `#1E8A56` | `#5BC98A` | `[ADAPTATION]` — `#30B373` on white is ~2.7:1, fails 4.5:1 |
| `Status.SuccessBg` | `#E6FFF3` | `#1F3A26` | TDI n83 (`#e6fff3` at α); dark `[ADAPTATION]` |
| `Status.Warning` | `#FFC10E` | `#FFC764` | TDI n11/d51; lost-partition capacity bar QSS L1774 |
| `Status.WarningText` | `#413B34` | `#FFC764` | TDI n49 (`rgb(65,59,52)`) — DRW's own amber-pairing |
| `Status.WarningBg` | `#FFF3D1` | `#47433E` | TDI n45/d50 |
| `Status.WarningStrong` | `#D4760B` | `#D6890B` | TDI n46/d46 (warning borders/labels) |
| `Status.Danger` | `#F72C2C` | `#FF5A5A` | TDI n36/d36 (A05 §1) |
| `Status.DangerAlt` | `#E74F4F` | `#FF8C8C` | QSS census alert (A21 §4); TDI d39 |
| `Status.DangerPressed` | `#D23636` | `#E5484D` | TDI n60; pressed `[ADAPTATION]` dark |
| `Status.DangerBg` | `#FDD5D5` | `#652C2D` | TDI n38/d38 |
| `Status.DangerText` | `#C21F1F` | `#FF8C8C` | `[ADAPTATION]` — `#F72C2C` on white ~3.9:1 |
| `Status.Info` | `#0560FD` | `#5094FF` | = Brand.Primary (DRW has no separate info hue) |

### 2.4 Background layers

| Token | Light | Dark | DRW evidence |
|---|---|---|---|
| `Bg.Window` | `#FFFFFF` | `#1B1D1F` | DrwHomePage white QSS L1596; TDI d1 |
| `Bg.WindowEdge` | `#E8F0FF` | `#141516` | pixel-sampled edges (A35 §1); dark `[ADAPTATION]` |
| `Bg.Canvas` (page under cards) | `#F6F9FD` | `#202428` | TDI n21/d2 |
| `Bg.Surface` (sub-panels) | `#F0F2F6` | `#252B33` | TDI n70/d32 |
| `Bg.SurfaceAlt` (strips, table head) | `#E9EDF4` | `#2F3847` | TDI n101/d102 |
| `Bg.Card` | `#FFFFFF` | `#202428` | TDI n2/d2 |
| `Bg.CardHover` | `#F2F7FF` | `#2A313A` | tint ladder step 1 (A21 §4) |
| `Bg.Elevated` (flyouts/menus) | `#FFFFFF` | `#32383E` | TDI n2/d15 |
| `Bg.Overlay` (modal scrim) | `rgba(0,0,0,0.40)` | `rgba(0,0,0,0.55)` | TDI n4; dark scrim `[ADAPTATION]` from d87 |
| `Bg.Sidebar` | `#E5EEFB` | `#252B33` | TDI n5/d32 (panel tint A05 §1) |
| `Bg.SearchStrip` | `#ECF4FF` | `#31405C` | TDI n32/d91 — replaces DRW's dated `#fff9e6` yellow search bar (QSS L4414) |
| `Bg.Tooltip` | `rgba(5,96,253,0.90)` | `rgba(5,96,253,0.92)` | QToolTip QSS L20–30 |

### 2.5 Text

| Token | Light | Dark | DRW evidence |
|---|---|---|---|
| `Text.Primary` | `#1A1A1A` | `#F1F5FF` | QSS census 158× (A21 §4); TDI n3/d3 |
| `Text.Secondary` | `#616777` | `#B0B5C0` | QSS census 92×; TDI n17/d17 |
| `Text.Tertiary` (placeholders, section heads) | `#8794B3` | `#7B808A` | QSS L1677 `#8794b3`; TDI d4/d28 |
| `Text.Disabled` | `#A0A6B1` | `#596680` | QSS census 50×; TDI n28/d96 |
| `Text.OnAccent` | `#FFFFFF` | `#FFFFFF` | QSS throughout |
| `Text.Link` | `#2978FF` | `#A8CAFF` | QSS census 59×; TDI d26 |
| `Text.LinkPressed` | `#0F62F0` | `#669EFE` | QSS L679 |
| `Text.Inverse` (on brand fills) | `#FFFFFF` | `#0B1220` | `[ADAPTATION]` dark inverse for contrast on light-blue fills |

### 2.6 Borders & dividers

| Token | Light | Dark | DRW evidence |
|---|---|---|---|
| `Border.Hairline` | `#DFE6ED` | `#313237` | DiskScrollBarRightLine QSS L1639; TDI d67 |
| `Border.Default` | `#E7E7E7` | `#404C63` | QSS census 82× (A21 §4); TDI d40 |
| `Border.Strong` | `#C1CCDF` | `#596680` | neutral ladder step 3; TDI d96 |
| `Border.Selected` | `#0560FD` | `#5094FF` | tile selection = blue border (A35 §5) |
| `Border.Focus` | `#0560FD` | `#669EFE` | `[ADAPTATION]` (DRW suppressed outlines, QSS L18) |
| `Border.Glass` (dark hover stroke) | `rgba(5,96,253,0.35)` | `rgba(141,189,255,0.20)` | TDI n25/d24 |
| `Border.Danger` | `#F72C2C` | `#FF5A5A` | destination-picker error state QSS family (A21 §3 `state=error`) |

### 2.7 Confidence badges (recovery-chance language)

DRW graded files ("Recovery Chances: High" circular gauge in preview, A35 §11; `CCheckNodeThread`
grading, A09 §5) but exposed only coarse copy. Our 4-tier badge vocabulary `[ADAPTATION]` maps to
corpus hues:

| Token | Light (fill / text) | Dark (fill / text) | Meaning |
|---|---|---|---|
| `Confidence.Excellent` | `#E6FFF3` / `#1E8A56` | `#1F3A26` / `#5BC98A` | File intact, high chance (DRW "High") |
| `Confidence.Partial` | `#FFF3D1` / `#8A5A00` | `#47433E` / `#FFC764` | Partially overwritten — recoverable fragments |
| `Confidence.Overwritten` | `#FDD5D5` / `#C21F1F` | `#652C2D` / `#FF8C8C` | Sectors overwritten — low chance |
| `Confidence.MetadataOnly` | `#E9EDF5` / `#616777` | `#2F3847` / `#B0B5C0` | Name/path only (raw-carved, "File Path Lost" bucket, A09 §2) |

### 2.8 Health-gauge bands (DiskHealth semantics)

States from the shipped DiskHealth companion: "Good" / "Caution!" / physical-failure warning
(A19 §3, `EuCircleProgress` component, A01 §6):

| Token | Light | Dark | Band |
|---|---|---|---|
| `Health.Good` | `#30B373` | `#349A37` | ≥ 90 % health |
| `Health.Caution` | `#FFC10E` (text `#413B34`) | `#FFC764` | 60–89 % |
| `Health.Bad` | `#F72C2C` | `#FF5A5A` | < 60 % or SMART failure |
| `Health.Unknown` | `#A0A6B1` | `#596680` | no SMART data |

### 2.9 Chart accents (8 result categories)

`[ADAPTATION]` Categorical hues anchored on corpus colors (DRW's category system is 8-way —
Pictures/Videos/Documents/Audio/Archive/Emails/Bookmarks/Others, A09 §4; FileFilter.xml):
`Chart.1 #2978FF` (Pictures) · `Chart.2 #FF7342` (Videos — dark accent orange, TDI d11) ·
`Chart.3 #0560FD` (Documents) · `Chart.4 #89C5DA` (Audio — TDI n114) · `Chart.5 #FFB025`
(Archive) · `Chart.6 #30B373` (Emails) · `Chart.7 #8794B3` (Bookmarks) · `Chart.8 #A0A6B1` (Others).
Dark theme: same hues at +20 % lightness (`#68B4FF, #FF9A73, #5094FF, #A8D8E4, #FFCE76, #5BC98A, #B0B5C0, #7B808A`).

---

## 3. Typography

### 3.1 Families

- **Primary**: `Segoe UI Variable` (Win 11) with fallback `Segoe UI` (Win 7–10) — DRW shipped
  Segoe UI as base with Semibold 600 for emphasis (A05 §3; `SEMIBOLD_FONT_FAMILY` ×334 vs
  `NORMAL_FONT_FAMILY` ×36 — the UI is predominantly semibold, A21 §4).
- **CJK fallbacks** (per-locale, exactly DRW's matrix, A05 §3): zh-CN/zh-TW → `Microsoft YaHei UI`;
  ja → `Meiryo UI`; ko → `Malgun Gothic`. Implement as a WPF composite-font fallback list.
- **Display**: DRW used proprietary **DIN-Black / DINCond-Black** OTFs bundled in the RCC (A01 §2)
  for hero/marketing titles. **We do not ship DIN.** `[ADAPTATION]` Display face = `Segoe UI
  Variable Display` (weight 700) — visually chunky, license-free.
- **Numerals in counts/progress**: `Segoe UI` with `Typography.NumeralAlignment="Tabular"` so
  live counters don't jitter; fall back to `Consolas` if tnum is unavailable.

### 3.2 Type ramp (96 DPI base; WPF DIPs scale automatically at 120/144)

| Role | Size/line | Weight | DRW evidence |
|---|---|---|---|
| `Font.Display` | 24 / 30 | 700 | splash title 24px semibold (QSS L2182); celebration headline (A35 §7) |
| `Font.DisplayLarge` | 28 / 34 | 700 | `[ADAPTATION]` (empty/hero states) |
| `Font.Title` (page H1) | 22 / 28 | 600 | `DrwHomePageTitleLabel` 22px semibold (QSS L1661) |
| `Font.Subtitle` (section H2) | 16 / 22 | 600 | `DiskRefushTitleLabel` 16px (QSS L1676) |
| `Font.Body` | 14 / 20 | 400 | global QSS base 14px (QSS L6); row line-height from 19px min-height (QSS L1788) |
| `Font.BodyStrong` | 14 / 20 | 600 | SEMIBOLD predominance (A21 §4) |
| `Font.Caption` (metadata, dates) | 12 / 16 | 400 | splash status 12px (QSS L2188); table text 13px (QSS L17438) |
| `Font.CaptionStrong` | 13 / 18 | 600 | list header 13px (QSS L17438); search label 13px (QSS L4421) |
| `Font.Micro` (chips, badges) | 11 / 14 | 600 | `[ADAPTATION]` (DRW floor was 12px; 11 only for ALL-CAPS short badges) |

DPI table (effective px for `Font.Body`): 96 DPI → 14 px · 120 DPI (125 %) → 17.5 px ·
144 DPI (150 %) → 21 px. DRW additionally shipped an explicit zoom menu 100/150/200 % (A35 §1);
in WPF this is redundant with per-monitor DPI — do not implement a manual zoom `[ADAPTATION]`.

### 3.3 Number formatting

- **Sizes**: binary prefixes with decimal rendering — DRW format "31.15 GB / 149.45 GB",
  "147.04 GB", "146.41 GB" (A35 §5/§10). Rule: `< 1 MB` → KB (integer); `< 1 GB` → MB (1 decimal);
  ≥ 1 GB → GB (2 decimals); ≥ 1 TB → TB (2 decimals). Locale decimal separator.
- **Counts**: thousands separators — "Files Found: 1,139,261 (147.04 GB)" verbatim (A34 §2).
- **Time**: elapsed `hh:mm:ss` (stop-dialog stat "used time 00:02:38", A35 §6); remaining
  "7m 43s" `[ADAPTATION]` of DRW's "07m43s" (DYN-scan §7).
- **Sector counters**: "Reading sector: 134,316,032 / 31,413,598" — DRW shipped unseparated
  (DYN-scan §7); we add separators for readability.
- **Progress %**: integer only, tabular numerals.
- **Dates**: locale-aware short form (DRW rendered "10/7/26 11:30 PM", DYN-scan §2).

---

## 4. Spacing & layout

### 4.1 Base grid & scale

4 px base grid `[ADAPTATION]` (DRW metrics were close but not strict: 6, 12, 20, 24, 31, 36, 40,
45, 48 — A05 §5). Scale: **2, 4, 8, 12, 16, 20, 24, 32, 40, 48** (`Space.2 … Space.48`).

### 4.2 Window & page geometry (observed, kept)

| Metric | Value | DRW evidence |
|---|---|---|
| Window normal | 1240 × 840, opens maximized | `config.ini [windowsSize]` (A05 §2) |
| Window minimum | 800 × 600 | QSS L1445 (`min-width:800px; min-height:600px`) |
| Window radius | 8 | QSS L1444 |
| Title bar height | 40 (DRW ~35, icons y13–28) | A35 §1/§2 — raised to 40 for 32px hit targets `[ADAPTATION]` |
| Page margins | 48 / 48 / 48 / 48 | DRW home 40/45/0/48 + lost-disk 36/48/48/24 (QSS L1602/1607); harmonized `[ADAPTATION]` |
| Content max width | 899 (list area) | `DrwHomePartListView` max-width 899, min 300 (QSS L1737) |
| Coaching text max width | 720 | `[ADAPTATION]` (readability cap) |
| Toolbar (NavigateBar) height | 48 | A35 §8 toolbar row y≈55–105 |
| Status bar (BottomArea) height | 88 | A35 §6 bottom cluster y≈660–714 |
| Sidebar width | 220 (collapsed 48) | A35 §5 rows at x≈135 center; `[ADAPTATION]` value |
| Splitter handle | 1 px line + 8 px grab area | QSplitter chrome (A09 §1) |

### 4.3 Control heights

| Token | Value | DRW evidence |
|---|---|---|
| `Height.XS` (icon buttons) | 32 | navigatebar_filter_button 32 (QSS L4141); DRW pause/stop were 24 (QSS L4449) — raised `[ADAPTATION]` |
| `Height.S` (secondary/dialog buttons) | 28 | Cancel/Confirm buttons 28 (QSS L604) |
| `Height.M` (standard buttons, chips) | 32 | DiskScanButton 32 (QSS L1827); CAutoExpendBtn 32 (QSS L3653) |
| `Height.L` (hero CTA) | 36 | CRecoveryBtn 36 (QSS L3620) |
| `Height.Header` (table header, toolbar rows) | 40 | LostRightTableViewHeader 40 (QSS L17436) |
| `Height.Row` (detail list row) | 44 | LostRightTableView::item 44 (QSS L17419) |
| `Height.Menu` (menu item) | 32–36 | QMenu item ≈31 effective (QSS L1465–1478); DRW_More_Menu items ≈42 (A35 §4) — 36 compromise |
| Safe click target | ≥ 32×32 (prefer 40×40) | `[ADAPTATION]` (WCAG 2.5.5; DRW shipped 24×24 icon buttons, QSS L1690) |

### 4.4 Card & list metrics

- Card padding: 24 (DRW DevicePageItem content margins "24px 12px 12px 12px", QSS L1762;
  standardized 24/20/20/20 `[ADAPTATION]`). Card gap: 12 (DRW tile margins 12, QSS L1754–1757).
- Icon-view tile: 160 × 160 (DRW preview icon standard 160, IRX `preview=*_160.png`; A05 §2 icon
  standards 48/56/72/80).
- Tree row height 28; tree indent 16 px/level `[ADAPTATION]` (DRW used Qt default ~20).
- List row (detail): 44 px; columns Name (flex) · Date Modified (140) · Type (110) · Size (90,
  right-aligned) — header chrome from A35 §8 / QSS L17434.
- Scrollbars: home 12 px track; dense results **6 px visual on 12 px track** (QSS L1611/L1622);
  12 px top padding under sticky header.

---

## 5. Elevation & shape

### 5.1 Corner radii

| Token | Value | DRW evidence |
|---|---|---|
| `Radius.Control` | 4 | buttons (QSS L607, L1828), context items (QSS L16540), tree items (QSS L17401) |
| `Radius.ControlL` | 6 | CAutoExpendBtn 6 (QSS L3662) |
| `Radius.Card` | 8 | 472 `border-radius` declarations, 8 px dominant (A21 §1; QSS passim) |
| `Radius.Dialog` | 12 | RecoverActivateButton 12 (QSS L631); NAS panels 10–12 (QSS L3707/L4838) |
| `Radius.Window` | 8 | DrwMainWindow 8 (QSS L1444) |
| `Radius.Splash` | 20 | SplashWindow 20 (QSS L2161) — used for celebration/hero modals |
| `Radius.Badge` | 999 (pill) | step pill "1/3" (A35 §3); `[ADAPTATION]` full-round standard |

### 5.2 Elevation levels

DRW drew *one* universal bitmap shadow (`Resource/WndShadow/shadow.png`, most-referenced asset,
21×, A21 §1; `dropShadowBlur:20` QSS L1448) and hover shadows with blur 12 (QSS L1761). We
systematize into WPF `DropShadowEffect` levels `[ADAPTATION]`, hue-anchored on DRW's
`rgba(4,5,43,…)` (TDI n18) / `rgba(28,28,28,…)` (TDI d87):

| Level | Light | Dark | Use |
|---|---|---|---|
| `Elevation.Card` | 0,2,8 @ rgba(4,5,43,0.08) | none + 1 px `#404C63` border | resting cards |
| `Elevation.CardHover` | 0,4,12 @ rgba(4,5,43,0.12) | 0,4,12 @ rgba(0,0,0,0.40) | hover/drive tiles (blur 12 from QSS L1761) |
| `Elevation.Flyout` | 0,8,24 @ rgba(4,5,43,0.16) | 0,8,24 @ rgba(0,0,0,0.50) | menus, filter dropdowns (QMenu, QSS L1458) |
| `Elevation.Dialog` | 0,16,48 @ rgba(4,5,43,0.24) | 0,16,48 @ rgba(0,0,0,0.55) | modals |
| `Elevation.Window` | 0,20,60 @ rgba(4,5,43,0.30) | 0,20,60 @ rgba(0,0,0,0.60) | frameless main window (blur 20, QSS L1448) |

### 5.3 Focus rings & dark border strategy

- Focus: 2 px `Border.Focus` ring at `Radius.Control + 2`, plus 1 px inner white gap
  `[ADAPTATION]` — DRW set `outline:none` (QSS L18), an accessibility gap we deliberately fix.
- Dark theme borders: always 1 px visible border (`Border.Default` `#404C63`, strong `#596680`)
  instead of shadows-only — shadows are near-invisible on `#1B1D1F`; DRW itself switched to
  border-driven dark chrome (TDI dark slots 40/96; A05 §1 "borders #404c63/#596680").
- Glass strokes: interactive dark surfaces add `Border.Glass` on hover (TDI d24/d57).

---

## 6. Iconography

### 6.1 Taxonomy (reconstructed from `ImageResource.xml` + RCC manifests — names only, no assets)

**A. Device/location icons** (50 type codes, IRX `<Partition>`; A01 §3):
disk · system (× lock/unlock) · USB existing/lost (× lock/unlock) · partition existing/lost
(× lock/unlock) · HD · camera (Sony + generic) · UAV/drone · SD card · tachograph/dashcam ·
recycle bin · folder · RAID · NAS · cloud drives (OneDrive/Dropbox/Google Drive, IRX types
62–64) · desktop.

**B. Result-tree state icons** (IRX `<Tree>`; A01 §3, A09 §2): the same device states as roots,
plus special branches — Deleted · Directory Intact · lostfiles · **Tags Extracted** ·
**File Name Lost** · **File Path Lost** (folder + question mark, DYN-scan §3) · **Reconstructed**
(folder + puzzle pieces, DYN-scan §7) · **Unimportant** (collapsed junk bucket) · Versions ·
Archive · Email · Unsaved · bookmark · category folders img/video/doc/audio/others.

**C. File-type icons** (IRX `<List>/<FileNode>/<Html>`; A01 §4): 8 categories with per-extension
tiles — audio (aif, ape, m4a, mid, mp3, others) · photo (bmp, gif, jpg/jpeg, png, svg, tif/tiff,
others) · video (avi, flv, mov, mp4, mpg/mpeg, others) · document (word, excel, pdf, ppt, mail,
txt, xml, csv, ai, ae, ps, pr, dll, exe, rar, zip, sys) · browser-profile (Edge, Firefox,
Chrome, Safari, 360, Liebao, Sogou, UC) · archive · email · bookmarks.
Priority display order per category from `PriorityDisplayType.xml` (Picture: jpg→png→jpeg→gif→tif→eps→psd→bmp→ico, A05 §6).

**D. Wizard/action icons** (QSS `qproperty-*Icon` families): back/go/up (QSS L4052–4124) ·
refresh (nor/hov/pre/dis, QSS L1695–1698) · **pause / stop / continue** (QSS L4455–4476) ·
search + clear (`state="clean"`, QSS L4406) · filter funnel (QSS L4139) · view toggle list⇄icon
(A34 §1) · recover / preview / open-location / select-unrecovered / smart-select wand /
clear-selection (context menu set, QSS L16566–16615) · titlebar min/max/close (nor/hov/pre/dis,
A01 §2) · hamburger menu items (about/language/help/import/save-scan/updates/share/settings/
theme/zoom, A35 §4).

**E. Status glyphs**: TRIM ON/OFF chip (A07 §2) · BitLocker lock/unlock overlays · warning ⚠
(A35 §6 paused strip) · OS/USB/SSD sign badges 22×18 (QSS L1851–1867) · red-dot notification
badge (A35 §4) · checkmark-circle completion (A35 §8) · confidence gauge arc (A35 §11).

### 6.2 Style & sizes

- DRW ships **dual-register** icons: colorful solid-ish SVG *file tiles* (24/48 px, rasterized
  on demand to 72/160 — A01 §3) and monochrome *line chrome icons* (nav/titlebar/pause-stop).
  We keep that split: **line 1.5 px stroke for chrome/actions; filled colorful tiles for file
  types and device classes.**
- Sizes: **16** (inline/rows) · **20** (buttons, QSS L3631 `iconSize:20px`) · **24** (tree,
  `*_24` assets) · **32** (drive cards) · **48/72** (icon-view tiles, IRX `icon=*72`) ·
  **160** (preview/empty-state art, IRX `preview=*160`). Every icon has 4 states:
  normal/hover/pressed/disabled (QSS `qproperty-…Icon` quadruples, e.g. L4457–4460).
- **v1 sourcing `[ADAPTATION]`**: map the taxonomy above to **Fluent System Icons (MIT)** for
  line/chrome icons (e.g. `arrow_left` = back, `search` = search, `pause`/`stop` = scan controls,
  `hard_drive`/`usb_stick`/`sd_card`/`camera` = device classes, `lock_closed`/`lock_open` =
  BitLocker states). File-type tiles: generate homegrown colored tiles with 2-letter ext label
  (v1) → bespoke art in v2. **Never** extract or redraw DRW's shipped SVG/PNG assets (§12).

---

## 7. Motion

| Pattern | Duration / easing | Notes |
|---|---|---|
| Page/phase transition | 300 ms, `CubicEase(0.1,0.9,0.2,1)` out | cross-fade + 12 px slide-up `[ADAPTATION]` — DRW's QStackedWidget switch was instant (ARCH05 §3); 250–350 ms band acceptable |
| Panel/dialog open | 250 ms out · 200 ms in | fade + scale 0.96→1.0 from origin |
| Hover state change | 100–150 ms | color/opacity only (instant in DRW) |
| Press feedback | 80 ms | scale 0.98 on primary buttons `[ADAPTATION]` |
| Indeterminate scan shimmer | 1600 ms loop, 40 % width band | replaces DRW's PNG frame sequences (`ScanComplete_00024.png`, 24 frames, QSS L17463; ScanProgress/ = 188 assets, A01 §2) |
| Determinate progress smoothing | animate to target over 300 ms ease-out | sector progress jumps (`Reading sector: X/Y`, DYN-scan §7) must not strobe |
| Count-up (files found) | 250 ms tick with tabular numerals | "1,139,157 files" growth (DYN-scan §7) |
| Tree branch expand/collapse | 200 ms, height + fade | `[ADAPTATION]` |
| Badge/status pulse | none by default; caution states breathe 2 s ×3 max | `[ADAPTATION]` — restraint rule |
| **Reduce motion** | all durations → 0 ms; shimmer → 2 Hz opacity dip 10 % | honor `SystemParameters.ClientAreaAnimation == false` / `PrefersReducedMotion` `[ADAPTATION]` |

---

## 8. Component specs

Common state matrix (applies to all interactive components): **default · hover · pressed ·
disabled · focus · error** (where meaningful). DRW shipped hover/pressed/disabled as triads
(`:hover` 224 · `:pressed` 195 · `:disabled` 147 — A21 §3); error via dynamic property
(`state="error"`, A21 §3). Focus is our addition (§5.3).

### 8.1 WizardStepper `[ADAPTATION]`
- **Why**: DRW has *no* stepper — the wizard is a bare QStackedWidget (ARCH05 §3: "the wizard is
  a stack, not a window manager"), and users got lost (hence DRW's AI assistant auto-opening at
  60–75 s to guide them, A35 §3). We add an explicit 3-step rail: **Select → Scan → Recover**.
- **Anatomy**: horizontal pill rail in the title-bar area; each step = number-in-circle (20 px) +
  label (CaptionStrong) + connector 24 px hairline. Current step: Brand.Primary fill; done steps:
  Status.Success check; future: Text.Tertiary outline.
- **Tokens**: `Brand.Primary`, `Status.Success`, `Text.Tertiary`, `Border.Hairline`,
  `Radius.Badge`. Height 28; clickable (navigates back only to completed, safe steps).
- **States**: done / current / upcoming / disabled (mid-scan back-navigation to Select is blocked
  while a scan writes — reversible-by-default rule P5).
- **A11y**: `AutomationProperties.Name` = "Step 2 of 3, Scan, current"; steps are toggle buttons
  in tab order after the title bar.

### 8.2 DriveCard (home tile)
- **Anatomy** (DRW ground truth, A35 §5 + QSS L1743–1764): drive icon 32 → name+letter
  (BodyStrong) → file system (Caption) → capacity bar (156×4, `Radius 2`) + "31.15 GB / 149.45 GB"
  (Caption, tabular) → badges row: OS/USB sign (22×18, QSS L1851), TRIM chip (Micro pill),
  BitLocker lock overlay on icon.
- **Metrics**: card 200×96 min, `Radius.Card`, padding 20; grid gap 12; `Elevation.Card`,
  hover → `Elevation.CardHover` + `Bg.CardHover` + 1 px `Border.Selected` (DRW: "selection = blue
  border + checkmark", A35 §5).
- **States**: default / hover / selected (border + top-right check circle 20 px, Brand fill) /
  scanning (capacity bar → animated shimmer, "Scanning…" chip) / lost-partition (capacity fill
  `Status.Warning`, A35 n/a → QSS L1774 amber for `existPartition=false`) / BitLocker-locked
  (lock glyph + "Unlock to scan" secondary action) / disconnected (grayscale 50 %, ErrorState
  actions).
- **Tokens**: `Bg.Card`, `Border.Selected`, `Brush.Cta` (capacity fill: light `#0C99FF→#0560FD`),
  `Status.Warning`, `Text.Secondary`.
- **KB/A11y**: Enter/Space selects + focuses the Scan action; card = single tab stop with
  internal arrow navigation; name = "Windows, C colon, NTFS, 31.15 of 149.45 gigabytes used,
  system drive, TRIM off".

### 8.3 ScanModeCard `[ADAPTATION]`
- **Why**: DRW never asked — it auto-runs Quick→Advanced and upsells afterwards ("Recover Found
  Deleted %1 files %2" + "Advanced Scan is trying to find more other lost files for you while
  recovering…", A08 §4). We surface the choice honestly *before* the scan (quick vs deep), with
  plain-language tradeoffs.
- **Anatomy**: icon 32 · title (Subtitle) · 2-line description (Body, Text.Secondary) ·
  meta row (expected duration, Caption) · radio-check corner. Card 260 min width, `Radius.Card`.
- **States**: default / hover / selected (radio dot Brand.Primary + `Border.Selected`) /
  disabled (during active scan).
- **Tokens**: `Bg.Card`, `Brand.Primary`, `Text.Secondary`.
- **A11y**: radio-group semantics (`AutomationProperties.SelectionContainer`); Space toggles.

### 8.4 FileTypeFilterChips
- **Ground truth**: DRW's category surface = the search "Optional Type" dropdown (Pictures ·
  Videos · Documents · Audio · Archive · Emails · Bookmarks · Unsaved File, A09 §4) + classify
  tabs (CFileTypeFilterTabBar, A09 §3). Chips are our Fluent rendering `[ADAPTATION]`.
- **Anatomy**: horizontal scroll-off chip strip under the toolbar; chip = category icon 16 +
  label (CaptionStrong) + live count "(12,408)" in tabular numerals + removable ✕ when active
  (DRW's captured search state shows "a removable filter chip with ✕", A35 §8 — pattern is
  corpus-verified).
- **Metrics**: chip height 28 (`Radius.Badge`), padding 8/12, gap 8; "All files" reset chip first.
- **States**: default (`Bg.Surface`, `Border.Default`) / hover (`Bg.CardHover`) / active
  (Brand.Primary fill, white text) / disabled(0 matches: count "0", chip disabled, tooltip
  explains — from DRW's `CFilterNoRlt` no-result state, A09 §4).
- **A11y**: toggle buttons; counts announced ("Pictures, 12,408 files, filter on").

### 8.5 ProgressSurface (scan-progress hero, bottom bar)
- **Ground truth**: A35 §6 + QSS L4445–4479 + L2190–2207 — circular % gauge bottom-left
  (≈64 px ring, center (48,687)), Pause ‖ and Stop ■ buttons, status lines, full-width
  "Searching:" ticker strip in light blue, "Recover All…" CTA bottom-right.
- **Anatomy**: [RingGauge 64: 4 px Brand arc, % in BodyStrong center] · [Pause / Stop icon
  buttons 32 (`Height.XS`) with tooltips] · [status line 1: "Advanced Scanning — Files Found:
  1,139,157 (147.04 GB)" BodyStrong tabular] · [status line 2: "Remaining: 7m 43s · Reading
  sector: 134,316,032 / 31,413,598" Caption] · [ticker strip: current file path, marquee on
  overflow, `Bg.SearchStrip`, height 28] · [contextual CTA `Recover All…` (`Height.L`)].
- **States**: running (gauge animates; results stream) · paused (gauge frozen at pause %, amber
  strip "Scan paused", Pause→Continue icon swap — DRW behavior, QSS L4463 `state="paused"`) ·
  finishing ("Organizing found files…" phase, A08 §3) · complete (ring→success check, strip→
  "Scan completed, Files Found: N (S)" — verbatim DRW bottom-bar copy, A34 §2) · stopped.
- **Tokens**: `Brand.Primary` arc, `Brush.Cta` linear fill, `Status.Warning` paused,
  `Status.Success` complete, `Bg.SearchStrip`, tabular numerals.
- **A11y**: gauge = `RangeValue` pattern ("Scanning, 34 percent"); Escape opens stop-confirm
  (never stops directly — P5); SR announcement throttled to 1 Hz.

### 8.6 ResultTree (left pane)
- **Ground truth**: 3 verified live roots — existing filesystem, **File Path Lost** (warning
  icon), **Reconstructed** (grows live) (DYN-scan §3/§7); 5 special branches with coaching
  tooltips (A09 §2); tree item chrome QSS L17366–17410; 6 px scrollbar (QSS L1622).
- **Anatomy**: root row = state icon 20 + name (BodyStrong) + live count "1.11M" (Caption,
  tabular) + per-branch tooltip (DRW's exact coaching pattern, A09 §2). Indent 16/level.
  Chevrons nor/hov/pre.
- **States**: row default / hover `Brush.BrandTint` / selected `Brush.BrandTintStrong`
  (`Radius 4`, QSS L17398–17410) / branch-collapsed (Unimportant defaults collapsed, A09 §2) /
  scanning-branch (pulse dot while growing) / disabled (offline source).
- **Tokens**: `Brush.BrandTint(Strong)`, `Border.Hairline` separators, `Status.Warning` (File
  Path Lost glyph), `Text.Secondary` counts.
- **KB/A11y**: ↑↓ move, →/← expand/collapse, Home/End, type-ahead find (§9); counts in SR name
  ("File Path Lost, 4,300 files").

### 8.7 FileListRow (detail + icon views)
- **Ground truth**: 44 px rows (QSS L17419), sortable header 40 px / 13 px / `#596680` with 1 px
  `#e7e7e7` rules (QSS L17434–17448), select-all tri-state checkbox, async thumbnails
  (CGetListThumbnailThread, A09 §5), double-click → preview, single-click → selection + details
  pane update (A35 §8), icon-view toggle at toolbar (667,80).
- **Detail row anatomy**: [checkbox 20] [type icon 20 or thumbnail 24] [Name, Body,
  ellipsis] [Date Modified, Caption, right-align? left] [Type, Caption] [Size, Caption tabular
  right] [confidence badge optional]. Height 44; separator 1 px `Border.Hairline`.
- **Icon view anatomy**: 160×160 tile — thumbnail 96 (or type icon 72, IRX `*_72`), name 2-line
  CaptionStrong centered, checkbox appears on hover/selection top-left; grid gap 12.
- **States**: default / hover (`Bg.CardHover`, DRW hover frame QSS L17458) / selected
  (`Brush.BrandTintStrong` full-row + checkbox filled Brand — DRW: "checkbox fills blue", A35 §8) /
  checked-unfocused (`Brush.BrandTint`) / partial (parent rows show half-filled tri-state) /
  lost-file variant (name shown as "IMG_0042.jpg" with MetadataOnly badge; path shown gray).
- **KB/A11y**: row = checkbox+label; Space toggles without activating; Ctrl+A honors visible
  scope; Enter opens preview; context menu key opens the 6-item menu (Recover/Preview/Open
  location/Select unrecovered/Smart select/Clear — DRW's exact set, A35 §8).

### 8.8 ConfidenceBadge `[ADAPTATION]`
- **Why**: DRW's grading existed (CCheckNodeThread, "Recovery Chances: High" gauge, A35 §11) but
  had no inline list affordance. We surface it per-row + as the preview gauge.
- **Anatomy**: pill (`Radius.Badge`) height 20, icon 12 + label Micro; colors §2.7. PreviewPane
  variant: 64 px arc gauge colored by band + label ("High / Moderate / Low / Metadata only").
- **States**: four bands (§2.7); tooltip per band with plain-language explanation (DRW-style
  coaching, A09 §2 register).
- **A11y**: label read as "Recovery chance: high" — never color-only.

### 8.9 PreviewPane (right rail + modal)
- **Ground truth**: right panel with Open button, Size/Date/Path fields (DYN-scan §2); modal
  `PreviewMainDialog` with per-format viewers (image/video/text/archive), loading→failure→content
  tri-states (A35 §11, A01 §2 Preview/ 340 assets).
- **Anatomy (rail)**: [thumbnail or type icon 160] [name BodyStrong 2-line] [field grid:
  Size / Date Modified / Path (Caption, label Text.Tertiary)] [ConfidenceBadge + gauge]
  [Existing/Lost chip] [Open + Recover secondary buttons].
- **Anatomy (modal)**: 900×640 default; toolbar (zoom ±, fit, hand-cursor pan, page controls for
  PDFs — DRW shipped zoom/hand cursors, A01 §2); footer = confidence gauge + Recover CTA.
- **States**: loading (spinner 250 ms before skeleton) / content / unsupported (icon 160 +
  "Preview not available for this type" + Open-with-app secondary) / damaged (Confidence.
  Overwritten banner + "Recover anyway") / empty selection.
- **Tokens**: `Bg.Canvas` rail, `Border.Hairline` divider, `Radius.Card` viewport.
- **A11y**: images get alt = filename; video controls standard player pattern; modal traps focus.

### 8.10 RecoveryQueueRow
- **Ground truth**: destination list rows "Windows (C:) 31.30 GB / 149.45 GB" + warning icons +
  "Choose folder…" (A35 §10); recovery progress via CGenericRecoveryProgress (A08 §1);
  insufficient-space gate with "Need an additional 146.41 GB…" (A35 §10).
- **Anatomy** (destination step): [drive icon 32] [name + free-space bar] [warning glyph +
  tooltip if destination is the scan source] [radio-select] [size-fit verdict chip: Fits /
  "Needs 146.41 GB more" (`Status.Warning`)].
- **Anatomy** (progress dialog): per-batch rows — [file/folder icon] [name] [mini progress 4 px]
  [state chip: Queued / Recovering / Repaired / Skipped-overwritten / Failed] + overall hero
  gauge + speed/ETA Caption line.
- **States**: source-drive error (row ring `Border.Danger` + acknowledgment copy — DRW's
  "Do not save data to this drive", A35 §10) / insufficient space / cloud re-auth / running /
  done (row check) / failed (retry affordance).
- **A11y**: progress = RangeValue; failures announced politely (status region), never modal.

### 8.11 HealthGauge
- **Ground truth**: DiskHealth companion: EuCircleProgress, states "Good / Caution! / physical
  failure risk", caution card with "View Solutions" (A19 §2–§3).
- **Anatomy**: 96 px ring (8 px track `Bg.Surface`, arc by band §2.8) + center label
  ("Good" BodyStrong / "Caution" `Status.WarningStrong`) + optional temperature Caption +
  expandable SMART table (rows: ID/Attribute/Current/Worst/Threshold/Status — A19 §3 schema).
- **States**: good / caution / bad (+ "View solutions" link — kept from DRW, useful & honest) /
  unknown.
- **A11y**: value pattern "Disk health 92 percent, Good"; SMART table = standard DataGrid.

### 8.12 PrimaryButton / SecondaryButton / DangerButton
- **Ground truth**: DiskScanButton 155×32 r6 `#0560fd` / hover `#3D84FF` / pressed `#004DD1` /
  disabled `#CED2E3` (QSS L1823–1844); CRecoveryBtn 150×36 (QSS L3617); dialog buttons 28 r4
  (QSS L603–626); gradient CTAs (TDI n10); disabled pattern bg `#dee1ed` + border `#c1ccdf`
  (QSS L3681).
- **Primary**: `Brush.Cta` gradient fill, `Brand.On` text BodyStrong, `Height.L` for hero CTAs
  ("Search for Lost Data", "Recover…"), `Height.M` inline; hover `Brand.Hover` (+2 % elevation),
  pressed `Brand.Pressed`, disabled `Bg.SurfaceAlt` + `Text.Disabled`, focus ring §5.3.
- **Secondary**: `Bg.Card` + 1 px `Border.Strong`, `Brand.Interaction` text; hover `Bg.CardHover`;
  pressed `Brand.LinkPressed` text; disabled `Text.Disabled` + `Border.Default`.
- **Danger**: `Status.Danger` fill, white text; hover `Status.DangerAlt`; pressed
  `Status.DangerPressed`; disabled as secondary-disabled. Used only for Stop-confirm "Stop",
  "Cancel scan", destructive destination overwrite acknowledgment.
- **Min width 120 (hero 155 — DRW value, QSS L1824)**; padding 20/20 (QSS L613); focus ring.
- **A11y**: default button = safe action; destructive is *never* default (DRW's stop dialog
  ordered Stop/Continue — we make Continue default, P5).

### 8.13 TextBox + Search
- **Ground truth**: search box placeholder "Search files or folders" (A35 §8); 30 px label
  height (QSS L4419); clear button with state-swapped icons (QSS L4406); DRW search bar family
  state machine onSwitch2Search/clearSearchEdit (A34 §1).
- **Spec**: height 32 (`Height.M`), `Radius.Control`, `Bg.Card`, 1 px `Border.Default` →
  `Border.Focus` 2 px on focus; placeholder `Text.Tertiary`; Search icon 16 left, ✕ clear right
  (visible when text present); error: `Border.Danger` + `Status.DangerText` helper line.
- **Search variant**: type-suggestions dropdown (category filter from A09 §4 "Optional Type"),
  Esc clears, Ctrl+F focuses from anywhere in results.
- **A11y**: label/placeholder both exposed; search announces result count on Enter
  ("1,204 matches for 'invoice'").

### 8.14 Dialog + AcknowledgmentGate
- **Ground truth**: DrwMessageBox 540×240 fixed; large scenario dialogs 636×433 / 672×627;
  icon standards 48/56/72/80 (A05 §2); 578×302 message box (QSS L821); stop-confirm with stat
  tiles + risk copy (A35 §6); activate dialog (A35 §4b) — the *chrome*, not the upsell, is what
  we reuse.
- **Spec**: modal width 540 (max 720), `Radius.Dialog`, `Elevation.Dialog`, scrim `Bg.Overlay`;
  header icon 48 + Title; body Body (max 720 text width); footer right-aligned buttons (primary
  = safe action); Esc closes *unless* AcknowledgmentGate.
- **AcknowledgmentGate** variant: requires an explicit checkbox ("I understand files on the
  source drive may be overwritten") before primary enables — our honest version of DRW's
  overwrite warning (A35 §10), replacing fear-copy with factual copy. Gate remembers nothing;
  it always re-asks for destructive actions (P5).
- **A11y**: focus trap, initial focus on primary or the checkbox for gates; Esc announced as
  "not available" on gates (DRW's dialog could reject ESC — A35 §12.4 — we do it only for gates).

### 8.15 Banner / Callout
- **Ground truth**: session-resume banner "Continue last scanning · Scan time · Scan files ·
  Load" (A07 §2); NAS floating tips CNasNetErrorTipsWnd (A08 §5); insufficient-space callout
  (A35 §10).
- **Spec**: full-width strip height 48 (collapsed 28), `Radius.Card`, left accent bar 4 px by
  severity (Info=Brand, Success, Warning, Danger); icon 20 + title BodyStrong + body Caption +
  action link/button right. Dismissible (✕) except critical; max 1 banner per region (DRW
  stacked promos — deliberately not repeated, §12).
- **A11y**: `AutomationLiveSetting="Polite"`; role = status.

### 8.16 EmptyState
- **Ground truth**: "Drive Not Show Up" dead-end with troubleshooting link (A07 §3); recycle-bin
  no-result window (A08 §6); search-empty coach "How to find files if it searched nothing" +
  3 strategies (A09 §4) — the *strategy coaching* is excellent and kept.
- **Spec**: centered block (max 480): illustration 160 `[ADAPTATION] generic, no mascot]` +
  Title + one-line Body (Text.Secondary) + up to 3 strategy rows (icon 20 + CaptionStrong +
  Caption) + primary action. e.g. empty search: "No files match 'invoice'" → strategies:
  filter by type/size/date · check File-Path-Lost branches · clear all filters.
- **A11y**: heading level 2; strategies are a list; focus moves to primary action.

### 8.17 ErrorState
- **Ground truth**: the disconnect matrix (A08 §5): "Drive Disconnected — Unable to continue
  scanning. Reconnect your drive and try again." / NAS / Linux / Cloud variants, each with
  Retry + Cancel (+ Disconnect).
- **Spec**: replaces the owning region (not a toast): icon 48 (`Status.DangerAlt`) + Title +
  plain-language cause (Body, Text.Secondary) + Retry (primary) + Cancel/Disconnect (secondary)
  + details expander (Caption, mono for error codes). Banner variant for non-fatal errors.
- **A11y**: role = alert; Retry is default; retry has 3 attempts with backoff note in details.

---

## 9. Density & virtualization aesthetics (1M-row readability)

Observed scale: 1,139,286 files / 147.12 GB enumerated in one session (DYN-scan §8); the right
list must stay smooth under that load (DRW used worker-thread tree building + incremental
filters so the UI never blocked, A09 §7).

1. **Separators over striping**: 1 px `Border.Hairline` row separators (DRW's approach — header
   rules + hover frames, QSS L17432/L17458). Zebra striping strobes at >60 fps scroll velocity;
   separators hold. `[ADAPTATION]` justification for keeping DRW's choice.
2. **Sticky column header**: 40 px, `Bg.SurfaceAlt`, sort glyph 12 (▲/▼) + sort-order number
   when multi-sort; never scrolls away (DRW: `padding-top:40px` under scrollbar, QSS L1627 —
   the header stayed put).
3. **Selection visuals that survive fast scroll**: selection is *part of the row template*
   (full-row `Brush.BrandTintStrong` + checkbox state baked into the container), never an
   overlay that lags repaint. Checked-parent tri-state derives from the model, not the view.
4. **Slim scrollbar**: 6 px visual / 12 px hit track (DRW results scrollbar, QSS L1622–1634);
   fade to 40 % opacity after 800 ms idle.
5. **Type-ahead find**: keyboard prefixes jump the tree/list (letters buffer 800 ms; matches
   name-start, then contains; overlay chip shows the buffer, Esc cancels) `[ADAPTATION]`.
6. **Deferred thumbnails**: rows render type icon instantly; thumbnails (CGetListThumbnailThread
   pattern, A09 §5) load in viewport priority LRU, 24→96 px upgrade, never blocking scroll.
7. **Count discipline**: tabular numerals + right-aligned size column so count-up doesn't reflow
   rows (§3.3); branch counts abbreviated ("1.11M" — DRW's own abbreviation, DYN-scan §3).
8. **Virtualization**: WPF `VirtualizingStackPanel` recycling mode; tree = lazy-load children
   with chevron affordance + count before expansion (so users know it's worth opening).
9. **Progressive fill**: new files appear with a 150 ms highlight flash (`Brush.BrandTint`)
   then settle — makes "live carving" visible (the Reconstructed branch growth, DYN-scan §7)
   without strobing.

---

## 10. Localization & RTL notes

- **Scale of the problem**: DRW shipped 22 languages / 25 .qm files, 1,556 strings each (A05 §4,
  A01 cross-ref). Plan for the same surface.
- **Expansion budget**: target-locale worst case vs en source — zh-CN typically *contracts*
  (−30 %); de/fr/es expand +10–30 %; ru/tr +20–40 %. Rule: **no fixed-width text slots** —
  containers use MinWidth/MaxWidth (DRW's own QSS is 1,036 min-width vs 847 max-width
  declarations, mostly elastic — A21 §1); buttons pad-to-fit; chips ellipsize at 2× en length.
- **Fonts**: CJK composite fallback (§3.1) must be in *every* text style — mixed-script strings
  (paths!) are common in results (DRW renders `Windows\…` paths with embedded CJK, DYN-scan §2).
- **Number/date**: locale separators and calendars (§3.3); file-size units localized ("GB" is
  universal, but decimal comma in de/fr).
- **RTL** (ar shipped in DRW's language list, A05 §4): mirror layout except: drive letters
  (C:) stay LTR; progress ring is direction-neutral; breadcrumbs reverse order; the back/forward
  chevrons flip; `FlowDirection=RightToLeft` at window level only — file *content* panes stay LTR.
- **Never localize**: file paths, extensions, SMART attribute IDs. DRW's shipped typos
  ("Hard Disk Drivers" [sic] A07 §2, "Whats's" DYN-scan §3.6, "Temprature" A19 §3) are *not*
  carried into our copy — clean-room strings only.

---

## 11. Accessibility

- **Contrast targets**: body text ≥ 4.5:1; large text (≥18 pt/14 pt bold) and UI components
  ≥ 3:1 (WCAG 1.4.3/1.4.11). Corpus audit driving our adjustments:
  - `#0560FD` on white ≈ **5.1:1** ✓ (brand on light is fine for text).
  - `#2978FF` on white ≈ **4.0:1** ✗ → link color promoted to `Brand.Primary`-adjacent usage at
    body size, `#2978FF` reserved for large/bold or UI accents.
  - `#30B373` ≈ 2.7:1, `#FFC10E` ≈ 1.6:1 → never text-on-white; use text variants (§2.3/§2.8).
  - `#616777` ≈ 5.9:1 ✓ secondary; `#8794B3` ≈ 3.1:1 → tertiary/placeholders only (large text).
  - `#A0A6B1` ≈ 2.5:1 → disabled-only (WCAG exempts disabled).
  - Dark: `#F1F5FF` on `#202428` ≈ 12.9:1 ✓; `#B0B5C0` ≈ 7.4:1 ✓; `#7B808A` ≈ 3.9:1 → large/UI.
- **Focus visibility**: 2 px ring (§5.3) on every interactive element — explicitly repairing
  DRW's `outline:none` (QSS L18). Focus never removed on click (`Focusable=true` discipline).
- **High-contrast themes**: map tokens to `SystemColors.*` (WindowTextColor, HighlightColor,
  HotTrackColor…) when `SystemParameters.HighContrast` — token *names* stay identical so
  component XAML never branches; gradients flatten to solid Highlight.
- **Focus order** (per screen): title bar → WizardStepper → content (drive grid / tree) →
  list → preview pane → primary CTA → status bar. Tab cycles toolbars left→right; F6 moves
  between panes (tree / list / preview); Ctrl+F search; Esc = one level of dismissal
  (dialog→filter→menu) — DRW's double-ESC exit trap (A35 §12.5) is deliberately not reproduced.
- **Screen-reader naming patterns**:
  - Drive: "{label} ({letter}:), {fs}, {used} of {total} used, {trim}, {type}, button Scan".
  - Progress: "Scanning {target}, {percent} percent, {count} files found, {size}, remaining
    {time}" — announce at ≤1 Hz.
  - Result row: "{type}, {name}, modified {date}, {size}, recovery chance {band},
    {checked/unchecked}".
  - Branch: "{branch name}, {count} items" + tooltip text on demand (help topic).
- **Keyboard parity for every mouse affordance**: hover tooltips → F1/help key on row;
  right-click menu → Menu key; drag-select → Shift+Arrow; tile hover states → focus equivalents.

---

## 12. What we deliberately do NOT carry over

With corpus evidence — each is a documented DRW behavior rejected on principle:

1. **All proprietary assets**: the ~7,400 RCC images/SVGs (A01 §1), DIN-Black/DINCond-Black OTFs
   (A01 §2), EaseUS logo, first-aid-kit icon, isometric illustrations, mascot art, custom
   `.cur` cursors (A01 §2). Only *names/taxonomy* inform our icon spec (§6).
2. **The periodic retention nag**: `RetentionActiveDlg` fires every 30–60 s on *any* page with
   "Wait! You are at risk of losing data!" (A35 §12.15) — a conversion-interruption pattern.
   Our upgrade surfaces are passive (a single footer chip) and never modal without user action.
3. **Fear-based copy**: "Valuable data might be LOST PERMANENTLY…" pressure framing (ARCH05 §5.4),
   the anti-piracy shaming dialog `PirateVictimsTipDlg` (A21 §2), exit-gating on double-ESC
   (A35 §12.5). We state overwrite risk factually, once, at the decision point (§8.14).
4. **Attention-funnel chrome**: the buy-guide window that *physically tracks the Recover CTA*
   (A34 §2.3 `sigMoveBuyGuideWnd`), the yellow "Buy Now" title-bar button (DYN-scan §3.7),
   promo gold gradients `#FFCE76/#FFB025` (A21 §4), gift/upsell frames (A01 §2 `Ad/`, `gift/`).
5. **The auto-opening AI assistant** at 60–75 s that swallows clicks in its bounds (A35 §3/§12.2)
   — assistance is on-request via a persistent, non-blocking entry point.
6. **Dated web-view chrome**: embedded-IE purchase webviews (`InnerWebWindow`/EUinApp, ARCH05 §1,
   artifact 23), Apower-vestige dialogs (AboutApowerdlg/ApowerActivatedlg, A21 §2), the yellow
   search strip `#fff9e6` (QSS L4414), and DRW's 20,802-line QSS layout-in-stylesheet pattern
   itself — we keep *tokens*, not the file.
7. **Free-tier preview caps as pressure** (15 s video, A35 §11): our preview is unlimited for
   recoverable formats; gating, if any, happens at save with a clear statement.
8. **Hidden statefulness**: windows that reject ESC without indication (A35 §12.4/§12.11),
   spontaneous minimize (A35 §12.13) — anti-patterns for a panic-moment tool (P1).

Kept from DRW *because they are honest and good*: TRIM chips (A07 §2), recovery-chance grading
(A35 §11), source-drive overwrite rails (A35 §10), per-branch coaching tooltips (A09 §2),
browse-while-scanning (DYN-scan §3), stop-confirm with rescan reassurance (A08 §3).

---

## 13. Token dictionary (machine-readable — transcribe to WPF resource dictionaries)

Format: `token | light | dark | usage`. Colors as hex/ARGB; geometry in DIP. Keys map 1:1 to
`SolidColorBrush`/`LinearGradientBrush`/`CornerRadius`/`Thickness`/`Double` resources in
`Theme.Light.xaml` / `Theme.Dark.xaml` (shared geometry in `Theme.Base.xaml`).

### Color — brand & interaction

```
Brand.Primary              | #FF0560FD | #FF5094FF | primary fills, selection, links
Brand.Hover                | #FF3D84FF | #FF68B4FF | primary-button hover
Brand.Pressed              | #FF004DD1 | #FF3874FF | primary-button pressed
Brand.Deep                 | #FF0448BE | #FF284A7F | pressed alt / emphasis border
Brand.Interaction          | #FF2978FF | #FFA8CAFF | secondary emphasis, chart.1, links (large)
Brand.LinkPressed          | #FF0F62F0 | #FF669EFE | link pressed
Brand.On                   | #FFFFFFFF | #FFFFFFFF | text/icons on brand fill
Brand.Focus                | #FF0560FD | #FF669EFE | focus rings
Brand.Tint                 | #190560FD | #1AC6DEFF | hover fills (≈10% alpha)
Brand.TintStrong           | #330560FD | #338DBDFF | selected fills (≈20% alpha)
Brush.Cta                  | grad #0C99FF→#0560FD 90° | grad #059AFD→#0CCDFF 90° | hero CTA fill
Brush.WindowHero           | grad #EFF5FF→#DEEAFF 180° | grad #1B1D1F→#18191B 180° | window bg
Brush.BrandBar             | grad #0C99FF→#0560FD | grad #059AFD→#0CCDFF | progress chunk/capacity
```

### Color — status

```
Status.Success             | #FF30B373 | #FF349A37 | success fill/icon
Status.SuccessText         | #FF1E8A56 | #FF5BC98A | success text on tint
Status.SuccessBg           | #FFE6FFF3 | #FF1F3A26 | success banner/badge bg
Status.Warning             | #FFC10E   | #FFC764   | warning fill (never text on white)
Status.WarningText         | #FF8A5A00 | #FFFFC764 | warning text
Status.WarningBg           | #FFFFF3D1 | #FF47433E | warning banner/badge bg
Status.WarningStrong       | #FFD4760B | #FFD6890B | warning border/label
Status.Danger              | #FFF72C2C | #FFFF5A5A | danger fill
Status.DangerAlt           | #FFE74F4F | #FFFF8C8C | danger hover / error icon
Status.DangerPressed       | #FFD23636 | #FFE5484D | danger pressed
Status.DangerBg            | #FFFDD5D5 | #FF652C2D | danger banner/badge bg
Status.DangerText          | #FFC21F1F | #FFFF8C8C | danger helper text
Status.Info                | #FF0560FD | #FF5094FF | info (= brand)
```

### Color — backgrounds, text, borders

```
Bg.Window                  | #FFFFFFFF | #FF1B1D1F | window/page base
Bg.WindowEdge              | #FFE8F0FF | #FF141516 | frameless window edge
Bg.Canvas                  | #FFF6F9FD | #FF202428 | page canvas under cards
Bg.Surface                 | #FFF0F2F6 | #FF252B33 | sub-panels
Bg.SurfaceAlt              | #FFE9EDF4 | #FF2F3847 | table head/strips
Bg.Card                    | #FFFFFFFF | #FF202428 | cards/tiles
Bg.CardHover               | #FFF2F7FF | #FF2A313A | card/list hover
Bg.Elevated                | #FFFFFFFF | #FF32383E | menus/flyouts
Bg.Overlay                 | #66000000 | #8C000000 | modal scrim (40%/55%)
Bg.Sidebar                 | #FFE5EEFB | #FF252B33 | nav sidebar
Bg.SearchStrip             | #FFECF4FF | #FF31405C | ticker/search strips
Bg.Tooltip                 | #E60560FD | #EB0560FD | tooltip fill (90%)
Text.Primary               | #FF1A1A1A | #FFF1F5FF | body text
Text.Secondary             | #FF616777 | #FFB0B5C0 | captions/labels
Text.Tertiary              | #FF8794B3 | #FF7B808A | placeholders/section heads
Text.Disabled              | #FFA0A6B1 | #FF596680 | disabled text
Text.OnAccent              | #FFFFFFFF | #FFFFFFFF | on-brand text
Text.Link                  | #FF2978FF | #FFA8CAFF | inline links
Text.Inverse               | #FFFFFFFF | #FF0B1220 | on light-blue fills (dark)
Border.Hairline            | #FFDFE6ED | #FF313237 | row separators
Border.Default             | #FFE7E7E7 | #FF404C63 | card/input borders
Border.Strong              | #FFC1CCDF | #FF596680 | secondary-button border
Border.Selected            | #FF0560FD | #FF5094FF | tile selection
Border.Focus               | #FF0560FD | #FF669EFE | focus rings
Border.Glass               | #590560FD | #338DBDFF | dark hover strokes
Border.Danger              | #FFF72C2C | #FFFF5A5A | error inputs
```

### Color — confidence, health, charts

```
Confidence.Excellent       | bg #FFE6FFF3 / tx #FF1E8A56 | bg #FF1F3A26 / tx #FF5BC98A
Confidence.Partial         | bg #FFFFF3D1 / tx #FF8A5A00 | bg #FF47433E / tx #FFFFC764
Confidence.Overwritten     | bg #FFFDD5D5 / tx #FFC21F1F | bg #FF652C2D / tx #FFFF8C8C
Confidence.MetadataOnly    | bg #FFE9EDF5 / tx #FF616777 | bg #FF2F3847 / tx #FFB0B5C0
Health.Good                | #FF30B373 | #FF349A37
Health.Caution             | #FFC10E   | #FFC764
Health.Bad                 | #FFF72C2C | #FFFF5A5A
Health.Unknown             | #FFA0A6B1 | #FF596680
Chart.1..8 (light)         | #2978FF #FF7342 #0560FD #89C5DA #FFB025 #30B373 #8794B3 #A0A6B1
Chart.1..8 (dark)          | #68B4FF #FF9A73 #5094FF #A8D8E4 #FFCE76 #5BC98A #B0B5C0 #7B808A
```

### Typography

```
Font.Family                | Segoe UI Variable, Segoe UI, Microsoft YaHei UI, Meiryo UI, Malgun Gothic
Font.FamilyDisplay         | Segoe UI Variable Display, Segoe UI, (CJK fallbacks)
Font.Display               | 24/30 w700 | splash/celebration headlines
Font.DisplayLarge          | 28/34 w700 | hero/empty states
Font.Title                 | 22/28 w600 | page H1 ("Select a location…")
Font.Subtitle              | 16/22 w600 | section headers ("Internal drives (2)")
Font.Body                  | 14/20 w400 | body/list text
Font.BodyStrong            | 14/20 w600 | emphasized rows/CTA labels
Font.Caption               | 12/16 w400 | metadata/dates/sizes
Font.CaptionStrong         | 13/18 w600 | table headers/chips
Font.Micro                 | 11/14 w600 | badges (ALL-CAPS short only)
Font.Numeral               | Tabular numerals ON (Typography.NumeralAlignment=Tabular)
```

### Spacing / radius / elevation / motion / sizing

```
Space.2 .4 .8 .12 .16 .20 .24 .32 .40 .48        | 4px-grid spacing scale
Radius.Control=4  ControlL=6  Card=8  Dialog=12  Window=8  Splash=20  Badge=999
Height.XS=32  S=28  M=32  L=36  Header=40  Row=44  Menu=36
Icon.16 .20 .24 .32 .48 .72 .160
Shadow.Card   = 0,2,8  rgba(4,5,43,.08)  | dark: none + 1px #404C63
Shadow.Hover  = 0,4,12 rgba(4,5,43,.12)  | dark: 0,4,12 rgba(0,0,0,.40)
Shadow.Flyout = 0,8,24 rgba(4,5,43,.16)  | dark: 0,8,24 rgba(0,0,0,.50)
Shadow.Dialog = 0,16,48 rgba(4,5,43,.24) | dark: 0,16,48 rgba(0,0,0,.55)
Shadow.Window = 0,20,60 rgba(4,5,43,.30) | dark: 0,20,60 rgba(0,0,0,.60)
Motion.Fast=100ms  Page=300ms  Panel=250ms  Shimmer=1600ms  CountUp=250ms
Motion.Easing = CubicBezier(0.1,0.9,0.2,1) out; ReduceMotion → all 0ms
Grid.Window = 1240x840 normal / 800x600 min / opens maximized
Grid.PageMargin=48  Grid.CardPadding=20,24  Grid.CardGap=12
Grid.Toolbar=48  Grid.StatusBar=88  Grid.Sidebar=220/48  Grid.Splitter=1+8
List.RowDetail=44  List.IconTile=160x160  Tree.Row=28  Tree.Indent=16
```

---

*End of DESIGN-SYSTEM.md v1.0 — Task 2-b. Evidence corpus:
`easeus-drw-research/re-workspace/artifacts/UIUX/` (A01–A35, DYN-*), `og-codebase/style.css`,
`og-codebase/config/themdark.ini`, `og-codebase/Resource{,Dark}/ImageResource.xml`,
`architecture/05-uiux-architecture.md`.*

---

## 14. v0.1.0-design addendum — systemization by the visual-design session

The v1.0 spec above is now **systematized as code** (session 2, visual-design agent):

- **Token source of truth**: `design/tokens.json` → `design/generate-tokens.mjs` →
  `Tokens.xaml` + `Theme.Light/Dark/HighContrast.xaml` + `Metrics.cs` (byte-stable,
  CI-checked; 106 theme keys × 3 themes, parity enforced). §2/§3/§4/§5/§7 tables above
  remain the human-readable rationale; tokens.json carries the same values with
  per-key evidence citations.
- **High-contrast theme** (§18 parity): SystemColors dynamic references for core roles;
  high-visibility literals for semantics; gradients → solid; elevation OFF. ThemeService
  auto-engages on OS high-contrast.
- **Contrast remediations** ([ADV], all matrix-verified in docs/ACCESSIBILITY.md):
  Text.Link light #256CE7 (was #2978FF 4.02:1), SuccessText #1D8352, DangerText #C01F1F,
  secondary-button fg #2264D5 on T3, danger fill/on pair, Brand.On dark = inverse
  (#0B1220 on #5094FF = 6.2:1). DRW's own palette shipped below AA in these pairs —
  our system repairs them (design principle: trust requires legibility).
- **Component library**: §8 specs are now implemented — Controls.Base/Buttons/Navigation/
  Surfaces.xaml + 14 custom controls (ToggleSwitch, Banner, Toast, Segmented,
  ProgressSurface, CircularGauge, FileTypeFilterChip, DriveCard, SearchField, Breadcrumb,
  RecoveryQueueRow, SessionBanner, Icon, Arc). State matrices per §8 common grid.
- **Icon set**: 70+ original geometries (chrome 24-grid stroke / tiles 20-grid filled /
  art register) — `Icons.xaml`; inventory from §6 taxonomy, art original per §12.
- **GAP FIX**: `Text.CaptionStrong` (referenced since v0.1.0-ui, never defined — silent
  runtime fallback) + full Text.* family completion.
- **Design gallery** (dev-only): `src/RdaWizard.Gallery` — every token/component/state/
  theme rendered live; swatches enumerate the dictionaries themselves.
- **Design-spec comps**: `src/RdaWizard.App/DesignSpecs/` (Home, ScanProgress, Results,
  RecoveryDialog, CompactSpecs: splash/modes/celebration/health).
- **Evidence chain**: VISUAL-EVIDENCE.md (63-shot VLM extraction + 15 verdicts) ·
  QSS-EVIDENCE.md (hard values, line refs) · VISUAL-SPEC.md (authority order, acceptance
  envelope) · ACCESSIBILITY.md (CI-enforced contrast matrix) · INTERIM-ASSETS.md.
