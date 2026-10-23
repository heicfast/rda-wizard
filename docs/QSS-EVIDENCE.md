# QSS-EVIDENCE.md — Hard Visual Evidence from the Shipped Stylesheet & Theme Config

> **Status**: AUTHORITATIVE hard-evidence register (VD-B). Sources:
> - `QSS` = `easeus-drw-research/og-codebase/style.css` (20,802 lines, 2,805 rules, 11,929
>   declarations, 1,677 styled targets, 777 image assets — census in artifact A21)
> - `TDI` = `og-codebase/config/themdark.ini` — the 118-slot dual palette (`[normal]` / `[dark]`)
> - `CFG` = `og-codebase/config/config.ini` (`[windowsSize]`, `[style]`, `[languages]`)
> - `A05/A21/A35` = research artifacts (styling / QSS census / atlas)
>
> Where VLM estimates (VISUAL-EVIDENCE.md) disagree with values below, **these values win**
> (session protocol, cross-validation rule). All line references are into `style.css` unless
> marked TDI/CFG. Values are the *reference's* parameters — our token system reproduces the visual
> language, it does not copy assets (DESIGN-SYSTEM §12 exclusions still apply: promo golds,
> fear-marketing surfaces, Apower vestiges are recorded here for completeness but NOT carried).

---

## 1. Stylesheet census (shape of the evidence)

| Metric | Value | Note |
|---|---|---|
| rules / declarations | 2,805 / 11,929 | avg 4.3 props/rule — atomized |
| min/max geometry declarations | 4,240 (36%) | layout lives in the stylesheet |
| color+background declarations | 2,094 (18%) | |
| typography declarations | 1,516 (13%) | `SEMIBOLD_FONT_FAMILY` ×334 vs `NORMAL` ×36 |
| interaction-state rules | 453 targets (27%) | `:hover` 224 · `:pressed` 195 · `:disabled` 147 · `:checked` 22 |
| dynamic attr-states | `state=error` 13 · `state=selected` 13 · `state=hover` 20 · `state=dark` 7 … | C++ re-polish mechanism |
| border-radius declarations | 472 | dominant radii: 4 / 6 / 8 / 12 / 20 |
| most-referenced asset | `Resource/WndShadow/shadow.png` ×21 | universal window shadow |
| widget-class pyramid | QLabel 1141 · QPushButton 873 · QFrame 342 | custom-UI, restyled wholesale |

---

## 2. Color system (TDI 118-slot palette, both themes, mapped to our tokens)

### 2.1 Brand & interaction (light `[normal]` / dark `[dark]`)

| Our token | Light | Dark | TDI slot | QSS corroboration |
|---|---|---|---|---|
| Brand.Primary | `#0560FD` | `#5094FF` | 6 | census 107× (A21 §4) |
| Brand.Hover | `#3D84FF` | `#68B4FF` | — / d64 | DiskScanButton:hover L1836 |
| Brand.Pressed | `#004DD1` | `#3874FF` | — / d65 | DiskScanButton:pressed L1840 |
| Brand.Deep | `#0448BE` | `#284A7F` | 65 / d27 | CAutoExpendBtn:pressed L3672 |
| Brand.Interaction | `#2978FF` | `#A8CAFF` | — / d26 | census 59× |
| Brand.LinkPressed | `#0F62F0` | `#669EFE` | — / d105 | L679 |
| CTA gradient | `#0C99FF→#0560FD` (135°) | `#059AFD→#0CCDFF` | 10 | hero CTA family |
| WindowHero gradient | `#EFF5FF→#DEEAFF` (180°) | `#1B1D1F→#18191B` | 1 | DRWMainFrame L1454 |
| BrandTint (hover ring) | `rgba(5,96,253,.10)` | `rgba(198,222,255,.10)` | 23 | tree hover L17405 |
| BrandTintStrong (selected) | `rgba(5,96,253,.20)` | `rgba(141,189,255,.20)` | 24 | tree selected L17409 |

### 2.2 Surfaces, text, borders

| Our token | Light | Dark | TDI slot | QSS corroboration |
|---|---|---|---|---|
| Bg.Canvas | `#F6F9FD` | `#202428` | 21 / d2 | home page bg family |
| Bg.Surface | `#F0F2F6` | `#313539`/`#252B33` | 70 / d70·d32 | strips, table heads |
| Bg.SurfaceAlt (Tint2) | `#E9EDF5` | `#2F3847` | — / d102 | hover ladder step 2 |
| Bg.Sidebar | `#E5EEFB` | `#252B33` | 5 / d32 | panel tint (A05 §1) |
| Bg.WindowEdge | `#E8F0FF` | — | — | atlas pixel scan (A35 §1) |
| Bg.SearchStrip | `#ECF4FF` | `#31405C` | 32 / d91 | replaces DRW `#fff9e6` (L4414) |
| Bg.SelectedRow | `rgba(5,96,253,.10)` | `rgba(141,189,255,.08)` | 23/d30 | row select family |
| Bg.Overlay | `rgba(0,0,0,.40)` | `rgba(28,28,28,.5)` | 4 / d87 | modal scrim |
| Bg.Tooltip | `rgba(5,96,253,.90)` | same | — | QToolTip L20–30 |
| Text.Primary | `#1A1A1A` | `#F1F5FF` | 3 | census 158×; home title variant `#171e34` (L1659) |
| Text.Secondary | `#616777` | `#B0B5C0` | 17 | census 92× |
| Text.Tertiary | `#8794B3` | `#7B808A` | —/d4·d28 | section heads L1677 |
| Text.Disabled | `#A0A6B1` | `#596680` | 28 / d96 | census 50× |
| Border.Default | `#E7E7E7` | `#404C63` | 12 / d40 | census 82× |
| Border.Strong | `#C1CCDF` | `#596680` | — / d96 | neutral ladder step 3 |
| Border.Hairline | `#DFE6ED` | `#313237` | — / d67 | DiskScrollBarRightLine L1639 |
| Disabled fill | `#DEE1ED` | `#434851` | — / d9 | button disabled states |

### 2.3 Blue tint ladder (hover/selected fills — kept 1:1)

`#F2F7FF` → `#E9EDF5` → `#E0EAFF` → `#C4D7FF` → `#CCD8ED` (census A21 §4; TDI slots 27·32·33·68·90).
Dark equivalents: `#DAE7FF`→`#A8CAFF` family + glass `rgba(141,189,255,.08–.2)` (TDI d23/d24/d84/d85).

### 2.4 Status & semantic

| Our token | Light | Dark | Source |
|---|---|---|---|
| Status.Success | `#30B373` | `#349A37` | TDI 59 |
| Status.Warning | `#FFC10E` | `#FFC764` | TDI 11 / d51 |
| Status.WarningStrong | `#D4760B` | `#D6890B` | TDI 46 |
| Status.WarningBg / text | `#FFF3D1` / `#413B34` | `#47433E` / `#FFC764` | TDI 45·49 |
| Status.Danger | `#F72C2C` | `#FF5A5A` | TDI 36 |
| Status.DangerAlt | `#E74F4F` | `#FF8C8C` | census; TDI d39 |
| Status.DangerPressed | `#D23636` | — | TDI 60 |
| Status.DangerBg | `#FDD5D5` | `#652C2D` | TDI 38 |
| Danger gradient (gate) | `#FCE7DF→#FFEFEA→#FFB7AB` | `#533531→#4A2324` | TDI 63 (light: 135°) |
| Shadow hue | `rgba(4,5,43,…)` | `rgba(28,28,28,…)` | TDI 18 / d87 |

### 2.5 Dark-only accents (from TDI [dark])

CTA hover family `#669EFE` (d105) · glass border `#669EFE` (d109) · menu selected `#2F3847`…
`#3D000000` (d81) · accent orange `#FF7342` (d11) — **carried** as `Chart.2` (Videos) only.

---

## 3. Button design system (QSS L602–940 — verbatim metrics)

| Variant | Geometry | Fill (normal) | Hover | Pressed | Disabled | Text |
|---|---|---|---|---|---|---|
| **Primary (DiskScanButton)** L1823 | 155×32 min, r6, pad-x 20 | `#0560FD` | `#3D84FF` | `#004DD1` | `#CED2E3` | 14px semibold `#FDFDFE` |
| Hero CTA (CRecoveryBtn) L3617 | 150×36 | CTA gradient | — | — | — | white semibold |
| **Blue solid** (DrwMessageBoxCancelBtn) L676 | 28h × 70 min, r4 | `#2978FF` | `#5895FF` | `#0F62F0` | — | 13px semibold white |
| **Secondary** (CancelButton/Confirm) L603 | 28h × 70 min, r4 | `#E0EAFF` | `#C4D7FF` | `#84ACFF` | `#DEE1ED` + white text | 13px semibold `#2978FF` |
| **Outline** (Ok/Continue) L682 | 26h × 70 min, r4, border `#4B9CFF` | gradient `#E F2F4→#FFFFFF` (bottom→top) | text→`#3A7FFF` | text→`#3A7FFF` | — | 13px semibold `#4B9CFF` |
| Gradient upgrade (UpgradeVerify) L742 | 26h × 140 min, r4, border `#2A7DFF` | `#89BEFF→#3992FF(.74)→#4D95FF` | lighter stops | darker stops | border `#C1CCDF` | 13px semibold white |
| Gold promo (RecoverActivate) L631 | 26h, r12, border `#FF9426` | `#FFCD83→#FD8E43(.7)→#FF9826` | lighter | `#FFA959→#FF7E28→#FF8F13` | `#E8EAEE→#DBDDE5→#DFE2EA` + `#C1CCDF` border | 12px semibold white |
| Inner solid (InnerbuyAct) L758 | 32h × 80 min, r6 | `#0560FD` | `#8EC0FF` | `#2E87F6` | `#DCE1EB` + `#A5A8B0` | 14px semibold white |
| Icon buttons (DiskRefresh) L1695 | 24×24 | qproperty icon states nor/hov/pre/dis (SVG quadruples) | | | | |

Reading for our system: **two blue CTAs** (hero gradient vs flat `#0560FD`), one secondary family
(`#E0EAFF` tint fill), one outline family, and the gold promo (excluded by policy — recorded only).
Heights: 26 dialog / 28 dialog-strong / 32 standard / 36 hero. Radius: 4 dialog / 6 standard.

---

## 4. Typography (families, sizes, weights — with QSS lines)

| Role | Size | Weight | Color | QSS evidence |
|---|---|---|---|---|
| Global base | 14px | normal (placeholder `NORMAL_FONT_FAMILY`) | — | L6 |
| Splash title | 24px | semibold | `#1A1A1A` | StartWindowTitelLabel L2182 |
| Splash status | 12px | normal | `#47385A` (purple-gray) | SplashLoadTextLabel L2188 |
| Page title (H1) | 22px | semibold | `#171E34` | DrwHomePageTitleLabel L1661 |
| Section header (H2) | 16px | normal | `#8794B3` | DiskRefushTitleLabel L1676 |
| Dialog titles | 18px | semibold | — | L2068/L2438/L3230 |
| Landing headline | 22px | semibold | — | L2222 |
| Buttons | 12–14px | semibold | per-variant | L603–L940 |
| Tooltips | 14px | normal | white on `#0560FD` | QToolTip L20 |
| Table/list text | 13px | — | — | L17438 |

Font stacks (CFG `[style]`): **Segoe UI** (+Semibold 600) base; CJK per-locale: zh→`Microsoft YaHei`,
ja→`Meiryo UI`, ko→`Malgun Gothic`; display face **DIN-Black/DINCond-Black** (RCC OTFs — not
carried, [ADAPTATION] → Segoe UI Variable Display 700).

---

## 5. Geometry & spacing (window/page/control, with sources)

| Metric | Value | Source |
|---|---|---|
| Window normal / min | 1240×840 / 800×600, opens maximized | CFG `[windowsSize]`; QSS L1445 |
| Window radius | 8 | QSS L1444 |
| Title bar height | ~35 (icons y13–28) | atlas A35 §2 — ours: 40 [ADAPTATION] |
| Home content margins | 40·45·0·48 | DrwHomePage L1602 |
| Lost-disk page margins | 36·48·48·24 | DrwLostDiskPage L1607 |
| Home title block | 286×127 box | L1663–1666 |
| Section header height | 31 | L1679 |
| Drive tile padding | 24·12·12·12; tile gap 12 | DevicePageItem L1762, L1754–57 |
| Capacity bar | ~156×4, r2 | atlas A35 §5 |
| OS/USB/SSD badges | 22×18 | L1851–1867 |
| Results list row | 44 | LostRightTableView::item L17419 |
| Table header | 40 | L17436 |
| Tree scrollbar | 8 wide | CLeftTreeView L3418 |
| Results scrollbar | 6 wide (12 track, pad-top 40) | CRightListView L1622–27 |
| Home scrollbar | 12 wide, margin-top 12 | DrwHomePage L1611 |
| Sidebar row | ~42–48 tall (atlas y-deltas 51/52/46) | A35 §5 |
| Context menu item | ~43.5 (atlas-measured) | A35 §8 — ours: 36 [ADAPTATION] |
| DrwMessageBox | 540×240 fixed | decompiled geometry (A05 §2) |
| Large scenario dialogs | 636×433 / 672×627 | A05 §2 |
| Icon standards | 48/56/72/80; tiles 160 | A05 §2; IRX `preview=*160` |
| Splash logo | 80×80 | StartWindowIconLabel L2173 |
| Refresh icon button | 24×24 | L1695 |

---

## 6. Chrome components (verbatim QSS extractions)

- **Tooltip** (L20–30): bg `#0560FD`, padding 2, 14px white text, r8, max-w 400, opacity 0.9,
  min-h 25. Docked directional variants via `uilib_autoToolTips_win` (states normal/wordwrap,
  402×70 min, props direction/arrowOffset).
- **Scrollbars**: three widths (12 home / 6 results / 8 tree); transparent track; 1px hairline
  separator `#DFE6ED` (L1636).
- **Tree items** (LostLeftTreeView L17398–17410): r4; hover `rgba(5,96,253,.1)`; selected
  `rgba(5,96,253,.2)`; 8-state attr machine (A21 §5 — richest in QSS).
- **Context menu** (QMenu L358–387 + CPopupMenu L6387–6536): item height 30 (our comp observes
  43.5 rendered incl. margins), selected `rgba(255,255,255,.06)` on dark panel, pressed
  `#0A84FF`, separator 1px `#353639`; DRW shipped a DARK translucent menu on a LIGHT app
  (visual note — our system uses theme-consistent Elevated surface [ADAPTATION]).
- **Title bar** (L943–1440 + atlas §2): frameless custom chrome, icon strip y13–28; buttons
  min/max/close + buy/activate/save-session/hamburger; `outline:none` on ALL buttons (L18) —
  the corpus's focus-visibility gap.
- **Progress**: `RadiusProgressBar` transparent custom paint (splash); `ScanProgressBar`
  transparent (chunk painted); the circular gauge is `EuCircleProgress` custom widget (A19);
  frame-sequence animations shipped as PNG strips (`ScanComplete_00024.png`, 24 frames).
- **Drive card icon badges**: `drivetype_sign_os/usb/ssd.png` 22×18 (L1851–1867) — per-type
  QSS image swaps; TRIM chip + BitLocker overlays from ImageResource.xml.

---

## 7. Gradients (complete shipped inventory, both themes)

| Gradient | Stops | Direction | Used on |
|---|---|---|---|
| CTA hero | `#0C99FF → #0560FD` | 135° (x1:0→x2:1) | hero buttons (TDI 10) |
| CTA dark | `#059AFD → #0CCDFF` | 135° | dark hero (TDI d10) |
| WindowHero | `#EFF5FF → #DEEAFF` | 180° | main frame (TDI 1; L1454) |
| Upgrade blue | `#89BEFF → #3992FF(.74) → #4D95FF` | 90° | upgrade buttons (L742) |
| Outline btn | `#EEF2F4 → #FFFFFF` | bottom→top | outline buttons (L682) |
| Gold promo | `#FFCD83 → #FD8E43(.7) → #FF9826` | 90° | activate/upsell (L631) |
| Danger gate | `#FCE7DF → #FFEFEA(.43) → #FFB7AB` | 135° | error frames (TDI 63) |
| Purple sheen | `#DFE3FC → #EDEAFF(.43) → #ABB DFF` | 135° | feature frames (TDI 41) |
| Gold sheen | `#FFCE76 → #FFB025` | 90° | gift frames (TDI 14) — excluded |
| Dark hero purple | `#524C87 → #394E9E` | 90° | dark feature frames (TDI d35) |

---

## 8. Reconciliation with VLM extraction (summary)

15 parameter-level verdicts in VISUAL-EVIDENCE.md §4 (V-01…V-15). Headlines:
VLM independently re-derives 20+ core tokens within antialiasing noise; VLM sizes over-read
+4–8px on small controls (QSS wins); the CTA "blue" is a gradient (stops win over sampled
mid-tones); radius ladder and tree-state tints confirmed exactly. No contradiction remains
unresolved; every token in `VISUAL-SPEC.md` cites its winning source.

## 9. Open items

- 777 QSS-referenced assets vs ~7,400 RCC assets: which images are code-referenced only
  (census A21 §7) — affects icon inventory completeness, not token values.
- Per-objectName line-level index available in `re-workspace/reversed/ui-resources/
  qss_property_census.json` (by_target with line numbers) for any future micro-lookup.
- `ResourceDark/` RCC extraction (artifact 01) covers dark icon parity; dark golden-image
  verification happens on Windows runners (VISUAL-SPEC §Acceptance).
