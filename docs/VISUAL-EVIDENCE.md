# VISUAL-EVIDENCE.md — VLM Extraction Manifest from the Screenshot Corpus

> **Status**: AUTHORITATIVE VLM extraction record (VD-A). Method: every one of the 63 screenshots in
> `easeus-drw-research/re-workspace/raw-evidence/ui-screenshots/` was analyzed with a fixed
> structured-extraction template (identity / layout / palette hex / gradients / typography /
> spacing-sizing / elevation / iconography / states / motion / ambiguities). Raw per-shot JSON:
> session working artifact `scripts/output/vlm-extraction.ndjson` (63/63 extracted, 0 parse
> errors); aggregate: `vlm-aggregate.json`.
>
> **Cross-validation rule (from the session protocol): VLM output is a hypothesis.** Hard evidence
> (`og-codebase/style.css` QSS + `config/themdark.ini` 118-slot palette + decompiled geometry)
> wins wherever both exist. Reconciliation verdicts live in §4 and resolve into
> `docs/VISUAL-SPEC.md`. Every VLM value below that hard evidence contradicts is marked
> `RESOLVED→<value>` in §4; agreements are marked `CONFIRMED`.
>
> Screenshot corpus coordinate convention: maximized 1024×735 client on a 1024×768 runner
> (atlas A35 §1). Pixel sizes from VLM are at that resolution.

---

## 1. Manifest — 63 screenshots, 7 areas

The corpus README identifies each capture; the atlas (A35) pixel-verified most of them. VLM identity
columns below are the model's independent reading (blind to the README) — agreement between the
README/atlas mapping and the VLM identity is itself a validation signal.

| # | Screenshot | Area → Page (atlas) | VLM state | Theme | Density |
|---|---|---|---|---|---|
| 1 | `01-splash/splash-reading-disk-info.png` | P00 Splash (CStartWindow) | Initializing/Scanning | light | comfortable |
| 2 | `01-splash/splash-t35.png` | P00 Splash (CStartWindow) | Initializing / Scanning Disk | light | comfortable |
| 3 | `02-landing/landing-activate-input-focus.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Modal Overlay | light | dialog |
| 4 | `02-landing/landing-after-activate-close.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Selection Ready | light | comfortable |
| 5 | `02-landing/landing-ai-assistant-panel.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Default (with helper popup) | light | comfortable |
| 6 | `02-landing/landing-ai-opt1-solution-wizard-1of3.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Active with Contextual Tip Overlay | light | comfortable |
| 7 | `02-landing/landing-ai-opt1-solution-wizard.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Active with Overlay (Solution/Tip Modal) | light | comfortable |
| 8 | `02-landing/landing-baseline-v2.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Default/Ready | light | comfortable |
| 9 | `02-landing/landing-baseline.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Default/Ready (with overlay) | light | comfortable |
| 10 | `02-landing/landing-buy-activate-dialog.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Modal Overlay (Blocking) | light | dialog |
| 11 | `02-landing/landing-desktop-tile-clicked.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Selection Made (Folder Selected) | light | comfortable |
| 12 | `02-landing/landing-early.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Ready to Search | light | comfortable |
| 13 | `02-landing/landing-hamburger-menu-open.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Default/Ready (Hardware Disk selected) | light | comfortable |
| 14 | `02-landing/landing-sidebar-chronosnap-page.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Idle / Ready to Start | light | comfortable |
| 15 | `02-landing/landing-sidebar-filerepair-page.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Idle / Ready to Start | light | comfortable |
| 16 | `02-landing/landing-sidebar-lostpartition-page.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Selection/Configuration | light | comfortable |
| 17 | `02-landing/landing-sidebar-moretools-confirm.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Default/Idle | light | comfortable |
| 18 | `02-landing/landing-sidebar-moretools-page.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Default/Idle | light | comfortable |
| 19 | `02-landing/landing-sidebar-sdcard-page.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Waiting for Device / Idle | light | comfortable |
| 20 | `02-landing/landing-solution-wizard-1of3.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Active with Overlay (Help/Tip) | light | comfortable |
| 21 | `02-landing/landing-solution-wizard-2of3.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Modal Overlay (Wizard Step 2/3) | light | comfortable |
| 22 | `02-landing/landing-solution-wizard-3of3.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Modal Overlay (Solution 3) | light | comfortable |
| 23 | `02-landing/landing-solution-wizard-closed.png` | P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36 | Modal Overlay (Help/Device Detection) | light | comfortable |
| 24 | `03-scan-phase/scan-after-stop-attempt.png` | P06 Scan+Results (CDetailPage) — scan states | Active Scanning (Paused/Interrupted by Modal) | light | comfortable |
| 25 | `03-scan-phase/scan-midscan-210s.png` | P06 Scan+Results (CDetailPage) — scan states | Scanning in Progress | light | comfortable |
| 26 | `03-scan-phase/scan-midscan-21pct.png` | P06 Scan+Results (CDetailPage) — scan states | Scanning in Progress | light | comfortable |
| 27 | `03-scan-phase/scan-midscan-34pct-prior.png` | P06 Scan+Results (CDetailPage) — scan states | Scanning in Progress | light | comfortable |
| 28 | `03-scan-phase/scan-paused.png` | P06 Scan+Results (CDetailPage) — scan states | Scanning (Paused) | light | comfortable |
| 29 | `03-scan-phase/scan-resumed.png` | P06 Scan+Results (CDetailPage) — scan states | Scanning in Progress | light | comfortable |
| 30 | `03-scan-phase/scan-stop-confirm-dialog.png` | P06 Scan+Results (CDetailPage) — scan states | Modal Dialog (Pause/Stop Confirmation) | light | comfortable |
| 31 | `04-scan-complete/complete-celebration-modal-v2.png` | P08a Celebration modal (standalone 540×380) | Success / Modal Overlay | light | dialog |
| 32 | `04-scan-complete/complete-celebration-modal.png` | P08a Celebration modal (standalone 540×380) | Scan Completed (Modal Overlay) | light | comfortable |
| 33 | `05-results/results-baseline-v2.png` | P06 Results explorer — 3-pane | Post-scan, selection in progress | light | comfortable |
| 34 | `05-results/results-baseline.png` | P06 Results explorer — 3-pane | Post-scan, selection in progress | light | comfortable |
| 35 | `05-results/results-context-menu-open-v2.png` | P06 Results explorer — 3-pane | Scan Completed, Context Menu Open | light | comfortable |
| 36 | `05-results/results-context-menu-open.png` | P06 Results explorer — 3-pane | Post-scan, context menu open | light | comfortable |
| 37 | `05-results/results-context-menu-round4.png` | P06 Results explorer — 3-pane | Post-scan, selection in progress | light | comfortable |
| 38 | `05-results/results-filter-bar-open-clean.png` | P06 Results explorer — 3-pane | Post-scan, item selected | light | comfortable |
| 39 | `05-results/results-filter-bar-open.png` | P06 Results explorer — 3-pane | Post-scan, selection active, AI recommendation overlay visible | light | comfortable |
| 40 | `05-results/results-filter-path-dropdown.png` | P06 Results explorer — 3-pane | Post-scan, selection active | light | comfortable |
| 41 | `05-results/results-path-tab.png` | P06 Results explorer — 3-pane | Post-scan, selection mode | light | comfortable |
| 42 | `05-results/results-row-selected-gate-dismissed.png` | P06 Results explorer — 3-pane | Post-Scan (Selection Mode) | light | comfortable |
| 43 | `05-results/results-row-selected-round4.png` | P06 Results explorer — 3-pane | Scan Completed, Item Selected | light | comfortable |
| 44 | `05-results/results-row-selected.png` | P06 Results explorer — 3-pane | Scan Completed, Item Selected | light | comfortable |
| 45 | `05-results/results-search-active.png` | P06 Results explorer — 3-pane | Completed Scan, Item Selected | light | comfortable |
| 46 | `05-results/results-type-tab-midtransition.png` | P06 Results explorer — 3-pane | Post-scan, selection ready | light | comfortable |
| 47 | `06-preview/preview-panel-html-content.png` | P18a Live preview panel | Selection / Preview | light | comfortable |
| 48 | `06-preview/preview-panel-live.png` | P18a Live preview panel | Post-scan, file selection | light | comfortable |
| 49 | `07-recovery/recovery-after-esc.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Post-scan, selection active | light | comfortable |
| 50 | `07-recovery/recovery-destdlg-choosefolder.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Destination Selection | light | comfortable |
| 51 | `07-recovery/recovery-destdlg-cloud-tab.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Dialog (Cloud Tab Active) | light | comfortable |
| 52 | `07-recovery/recovery-destdlg-early-state.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Active Modal (Save Path Selection) | light | comfortable |
| 53 | `07-recovery/recovery-destdlg-local-selected.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Active Modal / Selection Required | light | comfortable |
| 54 | `07-recovery/recovery-destdlg-repair-off.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Ready to Save (Destination Selected) | light | comfortable |
| 55 | `07-recovery/recovery-destination-dialog-clean.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Dialog (Save Location) | light | comfortable |
| 56 | `07-recovery/recovery-destination-dialog.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Dialog (Save Path) | light | comfortable |
| 57 | `07-recovery/recovery-entry-results.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Completed Scan with AI Recommendation | light | comfortable |
| 58 | `07-recovery/recovery-exit-retention-gate.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Overlay (Upsell/Warning) | light | comfortable |
| 59 | `07-recovery/recovery-insufficient-space-gate.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Error/Warning Dialog (Insufficient Disk Space) | light | comfortable |
| 60 | `07-recovery/recovery-retention-gate-recurrent.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Overlay (Risk Warning / Upsell) | light | comfortable |
| 61 | `07-recovery/recovery-retention-gate-v2.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Overlay (Upsell/Warning) | light | comfortable |
| 62 | `07-recovery/recovery-retention-gate.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Overlay (Risk Warning / Upsell) | light | dialog |
| 63 | `07-recovery/retention-gate-landing-nag.png` | P23 destination picker + P24/P25/P28 funnel dialogs | Modal Overlay (Risk Warning / Upsell) | light | dialog |

**Coverage vs the atlas §13 matrix**: all 7 areas captured; known corpus gaps (dark theme, icon-view
grid, some dialog families) are flagged in §5 and closed from QSS/`themdark.ini` hard evidence
instead (see `QSS-EVIDENCE.md`).

---

## 2. Aggregate findings (corpus-wide)

### 2.1 VLM hex estimates → QSS anchors

Every VLM-sampled hex (595 samples across 63 shots) was quantized to the nearest QSS/themdark.ini
anchor (RGB Euclidean distance ≤ 28). The distribution — which VLM estimates cluster on which
shipped tokens — is the core reconciliation evidence:

| QSS anchor (hard) | Token role | VLM votes | Typical VLM estimate variants | Verdict |
|---|---|---|---|---|
| `#FFFFFF` | Surface/Card | 76 | `#ffffff` exact | CONFIRMED |
| `#2978FF` | Brand.Interaction (CTA fills render here) | 43 | `#1677ff #2d7ff9 #2b7fff #2d7ff6` | CONFIRMED (renders as gradient mid-tone; see 2.2) |
| `#616777` | Text.Secondary | 52 | `#666666 #5e6268 #6b7785` | CONFIRMED |
| `#F6F9FD` | Bg.Canvas | 56 | `#f4f9fd #f4f8fb #f5f8fc` | CONFIRMED |
| `#E7E7E7` | Border.Default | 49 | `#e0e0e0 #e5e6eb #e8e8e8` | CONFIRMED |
| `#1A1A1A` | Text.Primary | 39 | `#1f2329 #1f1f1f #1e2329` | CONFIRMED |
| `#F0F2F6` | Bg.Surface | 32 | `#f0f4fa #f0f4f8 #f0f2f5` | CONFIRMED |
| `#DEE1ED` | Disabled fill | 27 | `#e2e8f0 #dde1e6 #d9dde8` | CONFIRMED |
| `#3D84FF` | Brand.Hover (CTA read) | 24 | `#2d8cf0 #3b82f6 #3b9aff` | CONFIRMED as rendered CTA body |
| `#E8F0FF` | Bg.WindowEdge | 17 | `#e6f2ff #ebf3ff #e8f0fe` | CONFIRMED (atlas A35 §1 pixel scan) |
| `#E9EDF5` | Tint2 / SurfaceAlt | 18 | `#e8ecf0 #eeeeee #e8ebf4` | CONFIRMED |
| `#E0EAFF` | Tint3 (secondary-button fill) | 5 | `#dbeafe #dcebff` | CONFIRMED |
| `#8794B3` | Text.Tertiary | 5 | `#7f8c9a #8a919f` | CONFIRMED |
| `#30B373` | Status.Success | 5 | `#27ae60 #2ecc71 #28c76f` | CONFIRMED |
| `#F2F7FF` / `#E5EEFB` / `#C1CCDF` / `#C4D7FF` | Tint1 / Sidebar / Border.Strong / Tint4 | 5/3/4/1 | — | CONFIRMED (low sample counts — small UI regions) |
| `#E74F4F` / `#F72C2C` / `#FFC10E` | Danger / DangerAlt / Warning | 5/1/3 | `#ff4d4f #e74c3c #ef4444` | CONFIRMED |
| `#FF9826`/`#FFB025` | Gold CTA | 2 | `#ffb020 #ffa117` | CONFIRMED (activate-dialog gold button; **deliberately not carried** — promo exclusion, DESIGN-SYSTEM §12) |

Reading: the VLM independently re-derives the shipped palette to within antialiasing noise. The
systematic drift — brand blues reading `#1677ff–#3d84ff` instead of `#0560fd` — is explained by the
CTA **gradient** fill (`#0C99FF→#0560FD`, TDI n10): rendered mid-tones sit exactly in the band the
VLM samples. The atlas noted the same effect ("rendered CTA reads ≈ `#1B6DD0`–`#2C7BE5`", A35 §1).
**Resolution: honor the gradient stops, not sampled mid-tones.**

### 2.2 Gradients (VLM-observed, reconciled)

VLM gradient observations cluster into exactly the families the QSS defines:

| VLM observation (stops as sampled) | Reconciled spec | Hard source |
|---|---|---|
| `#2d8cfe→#6ba3ff`, `#1677ff→#40a9ff`, `#2d7ff9→#5ca0ff` (CTA buttons, horizontal/vertical) | CTA gradient `#0C99FF → #0560FD` (135° in QSS: x1:0,y1:0→x2:1,y2:0) | TDI n10 |
| `#ffffff→#f4f9fd`, `#f0f4f9→#ffffff` (window/hero surfaces, diagonal) | WindowHero `#EFF5FF → #DEEAFF` vertical | TDI n1; QSS L1454 |
| `#dce6f2→#bccfda` (sidebar area) | Sidebar tint `#E5EEFB` + panel tints | TDI n5 |
| `#2b3240→#1a1f29` (dark evidence — scan-phase deep-scan strip) | Dark surfaces `#1B1D1F/#202428` family | TDI d1/d2 |
| `#2d8cf0→#e1e4e8` (progress track at boundary) | Progress fill brand-blue on `Tint2` track | QSS progress family |

### 2.3 Corner radii histogram (VLM, 63 shots)

`r8` ×52 · `r6` ×45 · `r4` ×36 · `r12` ×8 · `r10` ×6 · `r20` ×6 (splash/celebration) —
a clean 5-step discipline that matches the QSS radius census exactly (DESIGN-SYSTEM §5.1):
4 = controls/tree items (QSS L607/L17401), 6 = large CTAs (QSS L1828), 8 = cards/window (QSS
L1444 + 472 radius declarations), 12 = dialogs/activation (QSS L631), 20 = splash/hero modals
(QSS L2161). **CONFIRMED — radius ladder carried 1:1.**

### 2.4 Shadow recipes (VLM, 63 shots)

Top recipes: `blur8/y2/10%` ×10 · `blur12/y4/10%` ×8 · `blur24/y8/15–20%` ×12 · `blur30/y10/15%` ×4 —
consistent with QSS hard numbers: card hover blur 12 (QSS L1761), window shadow blur 20 (QSS L1448,
`shadow.png` 21×), dialog/flyout 24–30. **CONFIRMED — systematized into the 5-level elevation
ladder** (DESIGN-SYSTEM §5.2): Card(0,2,8@8%) · CardHover(0,4,12@12%) · Flyout(0,8,24@16%) ·
Dialog(0,16,48@24%) · Window(0,20,60@30%), hue `#04052B` from TDI n18 `rgba(4,5,43,…)`.
VLM color estimates for shadows read "black" — the corpus value is the blue-black `#04052B`.

### 2.5 Control heights (VLM vs QSS)

VLM: 44px list rows ×13 · 36px inputs/secondary ×20 · 48px sidebar items ×10 · 40–44px primary
CTAs ×20 · 72px list-item icon rows ×3 · 110px location cards ×3.
QSS hard: list row 44 (QSS L17419 — **exact match**) · secondary buttons 28 (QSS L604) ·
primary 32 (QSS L1827) / hero 36 (QSS L3620) · header 40 (QSS L17436).
VLM over-reads button heights by ~4–8px (border + focus padding + JPEG-scale blur at 1024px
maximized). **Resolution: QSS values win; VLM confirms proportions** (primary > secondary,
row = 44, header = 40, sidebar rows are the tallest chrome element at 48 with 10px gaps).

### 2.6 Typography observations

VLM consistently reads: page titles ~20–22px/600 (QSS: 22 semibold, L1661 — **match**); body 13–14
(QSS global 14, L6 — **match**); captions 12 (QSS L2188 — **match**); dialog titles 16–18; a
predominance of weight-600 rendering (`SEMIBOLD_FONT_FAMILY` ×334 vs ×36 in QSS — **match**).
Numerals in counters render monospace-ish in scan stats — consistent with DRW using tabular
figures for live counters; carried as `Typography.NumeralAlignment=Tabular` + `Consolas` fallback.

### 2.7 Iconography

VLM: dual-register confirmed — monochrome line chrome icons (titlebar, nav, pause/stop) vs colorful
filled tiles (device/drive/file-type). Stroke discipline on chrome icons ~1.5px equivalent;
file tiles carry per-type color coding (photo=blue/purple, video=orange-red, doc=blue, archive=amber
families). Sizes observed: 16 (rows) · 20 (buttons) · 24 (tree) · 32–48 (drive cards) · 64–72
(splash/empty-state art). Matches the IRX/QSS size system 16/20/24/32/48/72/160 (DESIGN-SYSTEM
§6.2). **CONFIRMED.**

### 2.8 States observed across the corpus

Hover (tint fills + border tighten), selected (blue border + checkbox fill + `rgba(5,96,253,0.2)`
fills), disabled (`#DEE1ED` fill, white/tertiary text), loading (splash progress, scan gauge,
"Organizing" states), paused (frozen gauge + red strip), error (destination `state=error` red
borders), empty (coaching pages with illustration + strategy rows). Focus rings: **absent
throughout** (`outline:none`, QSS L18) — the corpus's own a11y gap, deliberately fixed in our
system ([ADAPTATION], DESIGN-SYSTEM §5.3).

---

## 3. Per-screenshot extraction

One entry per capture. These are the VLM's raw structured readings (lightly trimmed to the
most-diagnostic fields) — **hypotheses per the protocol**, reconciled in §4/`VISUAL-SPEC.md`.
Hex values are as-sampled (see §2.1 for the systematic gradient-mid-tone drift).


### 3.1 01-splash — P00 Splash (CStartWindow)

#### `01-splash/splash-reading-disk-info.png`

- **Identity**: Splash/Loading Screen / Initializing/Scanning — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F4F9FD` background (Main window background) · `#FFFFFF` surface (Subtle geometric shapes in illustration) · `#333333` text-primary ('EaseUS Data Recovery Wizard') · `#666666` text-secondary ('Reading the disk information...') · `#2D8CF0` accent (App icon background and progress bar fill) · `#2D8CF0` semantic (Progress bar active state) · `#E0E0E0` border (Window border/edge) · `#E6F2FF` gradient-stop (Top-left corner gradient fade)
- **Gradients**: diagonal Window Background: #FFFFFF → #F4F9FD · horizontal Progress Bar: #2D8CF0 → #FFFFFF
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~20px/600 #333333 · "Reading the disk information..." ~13px/400 #666666
- **Heights**: App Icon 64px · Progress Bar Track 4px
- **Radii**: Window container r12 · App icon r14
- **Shadows**: blur24/y8/10%
- **Iconography**: filled; stroke: minimal, mostly solid fills; corners: rounded; notable: medical-kit-plus
- **States visible**: loading
- **Motion hints**: Horizontal progress bar at the bottom indicating ongoing process.
- **Empty/loading/error visuals**: Loading state visible via progress bar and 'please wait' text.
- **Ambiguities**: Exact pixel dimensions depend on assumed display scaling (likely 100% or 125%). | Background contains complex 3D-style illustrations which are decorative.

#### `01-splash/splash-t35.png`

- **Identity**: Splash/Loading Screen / Initializing / Scanning Disk — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F4F8FB` background (Window background) · `#FFFFFF` surface (Subtle card-like background behind illustration) · `#2C3E50` text-primary ('EaseUS Data Recovery Wizard') · `#7F8C9A` text-secondary ('Reading the disk information...') · `#3B82F6` accent (App icon background and progress bar fill) · `#E2E8F0` border (Window outer border) · `#E0F2FE` gradient-stop (Top-left gradient highlight)
- **Gradients**: diagonal Window Background: #FFFFFF → #E0F2FE
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~20px/600 #2C3E50 · "Reading the disk information..." ~13px/400 #7F8C9A
- **Heights**: App Icon 64px · Progress Bar Area 6px
- **Radii**: Window Container r12 · App Icon r14
- **Shadows**: blur30/y10/15%
- **Iconography**: filled; stroke: Minimal stroke, mostly solid fills with rounded geometry.; corners: Rounded squares and circles.; notable: Medical Kit / First Aid Box
- **States visible**: loading
- **Motion hints**: Horizontal progress bar at bottom edge indicating active loading state.
- **Empty/loading/error visuals**: Loading state visualized via progress bar and status text.
- **Ambiguities**: Exact pixel dimensions of window are estimated based on standard UI scaling. | Background illustration is a complex 3D render, treated as a single visual element.


### 3.2 02-landing — P02 Landing (PeripheralHomeView) + sidebar pages P02a–P02e + dialogs P30/P36

#### `02-landing/landing-activate-input-focus.png`

- **Identity**: Activation Dialog / Modal Overlay — theme light, density dialog, VLM confidence high
- **Palette (VLM sampled)**: `#F0F4FA` background (Main window background) · `#FFFFFF` surface (Modal background) · `#1F2329` text-primary (Activate EaseUS title) · `#5E6268` text-secondary (Enter your license text) · `#1677FF` accent (Activate button fill) · `#FFB020` semantic (Buy License button fill) · `#D9D9D9` border (Input field border) · `#E5E6EB` divider (Sidebar section separators)
- **Typography (samples)**: "Activate EaseUS Data Recovery Wizard" ~18px/600 #1F2329 · "Enter your license code" ~14px/400 #5E6268 · "Select a location to search" ~24px/600 #1F2329
- **Heights**: Primary Button (Activate) 42px · Secondary Button (Buy) 42px · Input Field 42px
- **Radii**: Modal container r8 · Buttons/Input r6
- **Shadows**: blur24/y8/15%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke weight for line icons; filled style for brand mark.; corners: Slight rounding on outer corners of icons.; notable: Hardware Disk, SD Card, First Aid Kit (Brand)
- **States visible**: selected
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and screen capture compression. | Some text in the background is obscured by the modal overlay.

#### `02-landing/landing-after-activate-close.png`

- **Identity**: Select Location / Selection Ready — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main window background) · `#FFFFFF` surface (Drive selection card background) · `#F0F4F8` sidebar-background (Left navigation pane) · `#1F2329` text-primary (Main heading text) · `#6B7785` text-secondary (Storage capacity text) · `#2D8CF0` accent (Primary button and selected border) · `#52C41A` semantic-success (Checkmark icon in sidebar) · `#DDE1E6` border (Unselected drive card border) · `#E8ECF0` divider (Section separators in sidebar)
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2329 · "Internal drives (2)" ~14px/400 #6B7785 · "Windows(C:)" ~16px/600 #1F2329 · "31.16 GB/149.45 GB" ~12px/400 #6B7785 · "Search for Lost Data" ~15px/600 #FFFFFF
- **Heights**: Primary Action Button 44px · Drive Selection Card 110px · Location Shortcut Item 90px
- **Radii**: Drive Cards / Buttons r8
- **Shadows**: blur8/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled style for decorative/brand icons.; corners: Slight rounding on outer corners of square icons.; notable: Windows Logo, Hard Drive, Recycle Bin, Desktop Monitor, Folder
- **States visible**: selected
- **Ambiguities**: Exact hex values may vary slightly due to image compression and anti-aliasing.

#### `02-landing/landing-ai-assistant-panel.png`

- **Identity**: Select Location / Default (with helper popup) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FD` background (App background) · `#FFFFFF` surface (Main content area) · `#FFFFFF` card (Windows(C:) selection box) · `#1F2329` text-primary (Main heading text) · `#6B7785` text-secondary (Sub-labels like Internal drives) · `#2D8CFE` accent (Search button and checkmark) · `#52C41A` semantic (Green checkmark on Hardware Disk) · `#D9D9D9` border (Selection box border) · `#EEEEEE` divider (Horizontal line in selection box) · `#4A90E2` gradient-stop (Progress bar fill)
- **Gradients**: horizontal Windows(C:) usage bar: #2D8CFE → #6BA3FF
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2329 · "Internal drives (2)" ~14px/400 #6B7785 · "Windows(C:)" ~16px/600 #1F2329 · "31.15 GB/149.45 GB" ~12px/400 #6B7785 · "Search for Lost Data" ~15px/600 #FFFFFF
- **Heights**: Primary Button 44px · Selection Card 100px · Sidebar Item 48px
- **Radii**: Primary Button r6 · Selection Card r8 · Helper Popup r10
- **Shadows**: blur20/y8/15% · blur8/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width, rounded caps; corners: Rounded (approx 2-3px radius on inner corners); notable: Hardware Disk, SD Card, Recycle Bin, Desktop Folder, Search
- **States visible**: selected
- **Ambiguities**: Exact pixel values are estimates based on standard 1080p scaling. | Some text in the header is obscured by the popup.

#### `02-landing/landing-ai-opt1-solution-wizard-1of3.png`

- **Identity**: Select Location / Drive Selection / Active with Contextual Tip Overlay — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F2F5` background (Main window background) · `#FFFFFF` surface (Sidebar and card backgrounds) · `#FFFFFF` card (Windows(C:) drive box) · `#1F1F1F` text-primary (Main heading 'Select a location...') · `#666666` text-secondary (Sidebar labels like 'SD Card') · `#1677FF` accent ('Search for Lost Data' button and progress bar) · `#52C41A` semantic (Checkmark icon next to 'Hardware Disk') · `#D9D9D9` border (Outline of the Windows(C:) card) · `#E8E8E8` divider (Separator lines in sidebar) · `#40A9FF` gradient-stop (Lighter blue in popup gradient)
- **Gradients**: vertical Contextual Popup Background: #1677FF → #40A9FF
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F1F1F · "Internal drives (2)" ~14px/400 #666666 · "Windows(C:)" ~16px/600 #1F1F1F · "Solution 1:" ~15px/600 #FFFFFF
- **Heights**: Primary Action Button (Search) 44px · Secondary Action Button (Next) 36px · Sidebar Navigation Item 48px · Drive Selection Card 100px
- **Radii**: Buttons and Cards r6 · Popup Modal r8
- **Shadows**: blur24/y8/20% · blur8/y2/10%
- **Iconography**: dual-register; stroke: consistent 1.5px-2px stroke for line icons; filled for brand/logo icons; corners: rounded; notable: Hardware Disk, SD Card, Recycle Bin, Desktop, Folder
- **States visible**: selected, hover
- **Motion hints**: Progress bar fill on Windows(C:) drive indicates static state of a process or capacity.
- **Empty/loading/error visuals**: None explicitly shown as an empty state, though the popup offers a 'Solution' implying a potential detection issue.
- **Ambiguities**: Exact pixel dimensions depend on assumed DPI scaling (likely 100% or 125%). | The '1/3' badge in the popup suggests a multi-step tip sequence.

#### `02-landing/landing-ai-opt1-solution-wizard.png`

- **Identity**: Select Location / Drive Selection / Active with Overlay (Solution/Tip Modal) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F6FA` background (Main window background) · `#FFFFFF` surface (Selected drive card background) · `#E8EBF4` card (Sidebar selected item) · `#1F2329` text-primary (Main heading text) · `#5E6673` text-secondary (Sub-labels like 'Internal drives') · `#1677FF` accent (Primary button 'Search for Lost Data') · `#28A745` semantic (Checkmark icon on Hardware Disk) · `#D9DDE8` border (Drive card border) · `#3B9AFF` gradient-stop (Top of modal background) · `#1677FF` gradient-stop (Bottom of modal background)
- **Gradients**: vertical Solution Modal Background: #3B9AFF → #1677FF
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2329 · "Internal drives (2)" ~14px/400 #5E6673 · "Windows(C:)" ~16px/600 #1F2329 · "Solution 1:" ~15px/600 #FFFFFF · "31.15 GB/149.45 GB" ~12px/400 #5E6673
- **Heights**: Primary Button (Search) 48px · Secondary Button (Next in modal) 36px · Drive Card 100px
- **Radii**: Modal r8 · Buttons r6 · Drive Card r8
- **Shadows**: blur24/y10/20% · blur8/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke for outlines; flat fills for some symbols.; corners: Slight rounding on outer corners of icons.; notable: Hardware Disk, SD Card, Recycle Bin, Desktop Monitor, Folder
- **States visible**: selected, hover (implied by modal context)
- **Motion hints**: Progress bar visible inside Windows(C:) drive card indicating used space.
- **Empty/loading/error visuals**: Error/Help state triggered via 'Unable to find your hard drive?' link leading to the Solution modal.
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but not verifiable from raster. | Some text in the modal is partially obscured by the mascot graphic.

#### `02-landing/landing-baseline-v2.png`

- **Identity**: Select Location (Step 1) / Default/Ready — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main content background) · `#FFFFFF` surface (Card background (Windows C:)) · `#F0F4FA` sidebar-background (Left navigation pane) · `#2C3E50` text-primary (Main heading text) · `#6B7C93` text-secondary (Storage capacity text) · `#2D7FF9` accent (Selected card border and 'Search' button) · `#27AE60` semantic-success (Checkmark icon in sidebar) · `#E2E8F0` border (Unselected card borders)
- **Typography (samples)**: "Select a location to search" ~22px/600 #2C3E50 · "Internal drives (2)" ~14px/400 #6B7C93 · "Windows(C:)" ~16px/600 #2C3E50 · "Search for Lost Data" ~15px/600 #FFFFFF
- **Heights**: Primary Button 44px · Card Height 110px
- **Radii**: Cards and Buttons r8
- **Shadows**: blur12/y4/10%
- **Iconography**: dual-register; stroke: Consistent 2px stroke for line icons; filled for UI elements like the checkmark.; corners: Slightly rounded corners on square icons.; notable: Hardware Disk, SD Card, Recycle Bin, Desktop, Folder
- **States visible**: selected
- **Ambiguities**: Exact font family is sans-serif (likely Segoe UI or similar system font).

#### `02-landing/landing-baseline.png`

- **Identity**: Select Location / Default/Ready (with overlay) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main content background) · `#FFFFFF` surface (Sidebar and Card backgrounds) · `#2B7FFF` accent (Primary button and selection checkmark) · `#1F2329` text-primary (Main heading text) · `#6A707E` text-secondary (Section labels and subtitles) · `#DDE1E6` border (Card borders and dividers) · `#00C853` semantic-success (Checkmark icon in sidebar)
- **Gradients**: horizontal Windows(C:) drive usage bar: #2B7FFF → #5CA0FF
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2329 · "Internal drives (2)" ~14px/400 #6A707E · "Hardware Disk" ~15px/500 #1F2329 · "Search for Lost Data" ~16px/600 #FFFFFF
- **Heights**: Primary Action Button 48px · Sidebar Item 48px · Location Card 110px
- **Radii**: Buttons and Cards r8 · Overlay menu r12
- **Shadows**: blur24/y8/12% · blur8/y2/8%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width for line icons; filled style for brand/logo icons.; corners: rounded (slight radius on outer corners of strokes); notable: folder-grid, sd-card, tools, recycle-bin, monitor
- **States visible**: selected
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but cannot be confirmed without inspection. | The overlay menu appears to be a 'help' or 'assistant' widget that may be non-native to the core Qt layout.

#### `02-landing/landing-buy-activate-dialog.png`

- **Identity**: Activation / License Entry / Modal Overlay (Blocking) — theme light, density dialog, VLM confidence high
- **Palette (VLM sampled)**: `#E8EBF0` background (Main window background) · `#FFFFFF` surface (Modal dialog background) · `#F5F6F8` card (Sidebar item background (Hardware Disk)) · `#1F2329` text-primary (Modal title 'Activate EaseUS...') · `#5E6673` text-secondary (Modal subtitle text) · `#1677FF` accent ('Activate' button background) · `#FAAD14` semantic ('Buy a License Now' button) · `#D8DCE4` border (Input field border) · `#E5E6EB` divider (Separators in sidebar)
- **Typography (samples)**: "Activate EaseUS Data Recovery Wizard" ~18px/600 #1F2329 · "Enter your license code..." ~13px/400 #5E6673 · "Activate" ~14px/500 #FFFFFF · "Buy a License Now" ~14px/600 #1F2329
- **Heights**: Primary Button (Activate) 40px · Secondary Button (Buy License) 40px · Text Input Field 44px
- **Radii**: Modal dialog r8 · Buttons r4 · Input field r4
- **Shadows**: blur30/y10/15%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke width for line icons; filled style for the app logo and some status indicators.; corners: Slight rounding on outer corners of icon paths.; notable: Medical Kit (Logo), Hardware Disk, SD Card, Tools, File Repair
- **States visible**: selected
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and screenshot compression. | Some text in the background is obscured by the modal overlay.

#### `02-landing/landing-desktop-tile-clicked.png`

- **Identity**: Select Location (Step 1) / Selection Made (Folder Selected) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F9FF` background (Main window background) · `#FFFFFF` surface (Right content panel) · `#FFFFFF` card (Select Folder card background) · `#1F2329` text-primary (Main heading text) · `#6B7785` text-secondary (Section labels like 'Internal drives') · `#2D8CF0` accent (Primary button and selection border) · `#52C41A` semantic (Checkmark icon on Hardware Disk) · `#E1E4E8` border (Unselected card borders) · `#EFF2F5` divider (Subtle separators between sections)
- **Gradients**: horizontal Windows(C:) usage bar: #2D8CF0 → #E1E4E8
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2329 · "Internal drives (2)" ~14px/400 #6B7785 · "Search for Lost Data" ~15px/600 #FFFFFF · "31.34 GB/149.45 GB" ~12px/400 #6B7785
- **Heights**: Primary Button 44px · Sidebar Item 48px · Location Card 110px
- **Radii**: Cards and Buttons r8 · Window/Container r12
- **Shadows**: blur12/y4/10% · blur6/y2/15%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled style for brand icons (Windows logo).; corners: Slightly rounded corners on square icons.; notable: Hardware Disk, SD Card, Recycle Bin, Desktop, Folder
- **States visible**: selected
- **Ambiguities**: Exact font family is sans-serif (likely Segoe UI or similar system font).

#### `02-landing/landing-early.png`

- **Identity**: Select Location / Ready to Search — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main window background) · `#FFFFFF` surface (Card backgrounds (Windows C:)) · `#1E2329` text-primary (Header 'Select a location...') · `#6B7785` text-secondary (Subtext '30.96 GB/149.45 GB') · `#2D7FF9` accent ('Search for Lost Data' button) · `#27AE60` semantic (Green checkmark on Hardware Disk) · `#DDE4EC` border (Unselected card borders) · `#EBF3FF` card (Selected 'Hardware Disk' sidebar item background)
- **Gradients**: horizontal Progress bar in Windows(C:) card: #2D7FF9 → #5CA0FF
- **Typography (samples)**: "Select a location to search for lost dat" ~22px/600 #1E2329 · "Internal drives (2)" ~14px/400 #6B7785 · "Windows(C:)" ~16px/600 #1E2329 · "Search for Lost Data" ~15px/600 #FFFFFF
- **Heights**: Primary Button 44px · Location Card 110px · Sidebar Item 48px
- **Radii**: Cards and Buttons r8 · Progress bar r4
- **Shadows**: blur12/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled style for UI elements like the folder and drive icons.; corners: Slight rounding on inner corners of line icons.; notable: Hardware Disk, SD Card, Recycle Bin, Refresh
- **States visible**: selected, hover
- **Motion hints**: Rotating refresh icon next to the main header suggests an async loading state or 'refresh' capability.
- **Empty/loading/error visuals**: None

#### `02-landing/landing-hamburger-menu-open.png`

- **Identity**: Select Location / Default/Ready (Hardware Disk selected) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main app background) · `#FFFFFF` surface (Menu drawer and main card area) · `#E8F0FE` card (Selected 'Hardware Disk' item background) · `#1F1F1F` text-primary (Main heading text) · `#5F6368` text-secondary (Sidebar labels and subtext) · `#1677FF` accent ('Search for Lost Data' button and selection border) · `#34A853` semantic (Checkmark icon on Hardware Disk) · `#DADCE0` border (Dividers in the menu and card borders) · `#E8EAED` divider (Section separators in sidebar)
- **Gradients**: horizontal Windows(C:) progress bar: #1677FF → #40A9FF
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F1F1F · "Internal drives (2)" ~14px/400 #5F6368 · "Windows(C:)" ~15px/600 #1F1F1F · "31.16 GB/149.45 GB" ~12px/400 #5F6368 · "Search for Lost Data" ~15px/600 #FFFFFF
- **Heights**: Primary Button 44px · Sidebar Item 48px · Drive Card 110px
- **Radii**: Primary Button r6 · Drive Selection Card r8 · Menu Drawer r0
- **Shadows**: blur16/y0/15% · blur8/y2/8%
- **Iconography**: dual-register (filled for selections, line for navigation); stroke: consistent 1.5px-2px stroke weight for line icons; corners: slightly rounded corners on square icons; notable: Hardware Disk (Grid), SD Card, Recycle Bin, Refresh (circular arrow)
- **States visible**: selected, hover-implied (menu items)
- **Ambiguities**: Exact pixel dimensions of the menu drawer width are estimated based on visual proportion. | Some icons in the menu (EaseUS products) are brand-specific colored fills.

#### `02-landing/landing-sidebar-chronosnap-page.png`

- **Identity**: ChronoSnap Backup Feature Landing / Idle / Ready to Start — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main app background) · `#FFFFFF` surface (Main content white card) · `#E8F0FE` card (Selected 'ChronoSnap Backup' sidebar item) · `#1F2329` text-primary ('ChronoSnap Backup Free' title) · `#5F6673` text-secondary (Description text under title) · `#1677FF` accent ('Start' button background) · `#52C41A` semantic (Green checkmark next to Hardware Disk) · `#E4E7ED` border (Subtle dividers or edges of the white card)
- **Gradients**: vertical Illustration background (right side): #2B3240 → #1A1F29
- **Typography (samples)**: "ChronoSnap Backup Free" ~26px/600 #1F2329 · "Your free & effortless Time Machine" ~15px/400 #5F6673 · "Automatic Protection" ~17px/600 #1F2329 · "Start" ~15px/600 #FFFFFF
- **Heights**: Start Button 44px · Sidebar Item 48px
- **Radii**: Start Button / Illustration Card r8 · Sidebar Selection r6
- **Shadows**: blur16/y4/8% · blur24/y8/15%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke weight for line icons; filled style for brand/logo icons.; corners: Slightly rounded corners on square icon containers.; notable: Hardware Disk, SD Card, File Repair, ChronoSnap Logo
- **States visible**: selected
- **Ambiguities**: Exact pixel dimensions are estimates based on standard 1080p scaling. | Some text is truncated by the illustration overlay.

#### `02-landing/landing-sidebar-filerepair-page.png`

- **Identity**: File Repair Selection / Idle / Ready to Start — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main window background) · `#FFFFFF` surface (Right content panel) · `#E8F1FD` card (Selected sidebar item (File Repair)) · `#1F2329` text-primary (Main heading 'Repair Corrupted...') · `#5E6673` text-secondary (Subtext 'Add corrupted files...') · `#1677FF` accent ('Start' button background) · `#28A745` semantic (Green checkmark next to Hardware Disk) · `#E4E7ED` border (Divider lines or subtle borders)
- **Gradients**: vertical Folder illustration in main area: #DCE6F2 → #BCCFDA
- **Typography (samples)**: "Repair Corrupted Videos..." ~26px/600 #1F2329 · "Add corrupted files to easily repair the" ~14px/400 #5E6673 · "Start" ~15px/600 #FFFFFF · "Hardware Disk" ~14px/500 #1F2329
- **Heights**: Start Button 44px · Sidebar Item 48px
- **Radii**: Start Button r6 · Sidebar selection highlight r8 · Main content card r10
- **Shadows**: blur16/y2/8% · blur20/y8/12%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke for line icons; filled style for UI elements like the mascot.; corners: Slightly rounded corners on icon paths.; notable: hardware-disk, sd-card, tools, file-repair, partition-rescue
- **States visible**: selected
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and image compression. | The 'ChronoSnap Backup' icon is partially obscured/greyed out, suggesting a possible disabled or external state.

#### `02-landing/landing-sidebar-lostpartition-page.png`

- **Identity**: Lost Partition Rescue / Selection/Configuration — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F9FF` background (Main window background) · `#FFFFFF` surface (White card/container) · `#1A1A1A` text-primary ('Lost Partition Rescue' title) · `#666666` text-secondary (Description text under title) · `#1677FF` accent ('Start' button background and selected sidebar text) · `#52C41A` semantic (Green checkmark next to Hardware Disk) · `#E8E8E8` border (Subtle borders around list items or containers) · `#F0F0F0` divider (Separators between sidebar sections)
- **Gradients**: radial Illustration background (disk): #D9D9E5 → #BFBFD4
- **Typography (samples)**: "Lost Partition Rescue" ~26px/600 #1A1A1A · "Instantly recover missing..." ~14px/400 #666666 · "Partition Missing" ~16px/600 #1A1A1A · "Start" ~15px/600 #FFFFFF
- **Heights**: Start Button 44px · Sidebar Item 48px
- **Radii**: Start Button r6 · Main Content Card r12 · Sidebar Selection Highlight r6
- **Shadows**: blur16/y4/8%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled style for decorative illustration elements.; corners: Slightly rounded corners on square icons.; notable: Hardware Disk, SD Card, File Repair, Lost Partition Rescue (selected)
- **States visible**: selected
- **Ambiguities**: Exact pixel dimensions of the illustration are estimated due to its complex vector nature. | Some text in descriptions is truncated ('Format Requi...').

#### `02-landing/landing-sidebar-moretools-confirm.png`

- **Identity**: More Recovery Tools / Default/Idle — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main window background) · `#FFFFFF` surface (Card backgrounds and main content panel) · `#1F2329` text-primary (Main heading 'More Recovery Tools') · `#5E6673` text-secondary (Card descriptions) · `#2B7EF6` accent (Selected sidebar item background) · `#2ECC71` semantic (Green checkmark on Hardware Disk) · `#E4E7ED` border (Card borders) · `#EFF2F7` divider (Subtle dividers between sidebar sections)
- **Typography (samples)**: "More Recovery Tools" ~26px/600 #1F2329 · "Tackle complex data loss" ~14px/400 #5E6673 · "Crashed PC Rescue" ~17px/600 #1F2329 · "Rescue data from an unbootable" ~13px/400 #5E6673 · "Hardware Disk" ~14px/500 #1F2329
- **Heights**: Sidebar Item 44px · Recovery Card 165px
- **Radii**: Recovery Cards r10 · Selected Sidebar Item r6 · Search Bar r22
- **Iconography**: dual-register (filled icons inside outlined containers); stroke: Consistent 1.5-2px stroke weight for outlines; filled areas are solid color.; corners: Slightly rounded corners on icon containers.; notable: Hardware Disk, SD Card, Puzzle Piece, File Repair, RAID Array
- **States visible**: selected
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but cannot be confirmed without metadata.

#### `02-landing/landing-sidebar-moretools-page.png`

- **Identity**: More Recovery Tools / Default/Idle — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (Card backgrounds) · `#FFFFFF` card (Recovery tool cards) · `#1F2329` text-primary (Page title 'More Recovery Tools') · `#5E6673` text-secondary (Card descriptions) · `#2B7BF4` accent (Selected sidebar item background/text) · `#28C76F` semantic (Green checkmark next to Hardware Disk) · `#E8ECF1` border (Card borders) · `#E8ECF1` divider (Subtle lines between sidebar groups)
- **Gradients**: diagonal Card icon backgrounds (top-left): #F0F4F9 → #FFFFFF
- **Typography (samples)**: "More Recovery Tools" ~26px/600 #1F2329 · "Crashed PC Rescue" ~16px/600 #1F2329 · "Rescue data from an unbootable..." ~13px/400 #5E6673 · "Data Recovery" ~13px/600 #8C95A3
- **Heights**: Sidebar list item 44px · Recovery Tool Card 160px
- **Radii**: Cards r8 · Selected sidebar item r6 · Icon background container r12
- **Shadows**: blur8/y2/5%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width, rounded caps.; corners: Rounded (approx 2-3px radius on inner corners).; notable: Hardware Disk, SD Card, Wrench/Tools, File Repair, RAID Array
- **States visible**: selected
- **Ambiguities**: Exact font family is sans-serif (likely Segoe UI or similar system font); some icon details are small.

#### `02-landing/landing-sidebar-sdcard-page.png`

- **Identity**: SD Card Recovery Wizard / Waiting for Device / Idle — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main content background) · `#FFFFFF` surface (Sidebar and header background) · `#E8F0FE` card (Selected 'SD Card' sidebar item) · `#1F1F1F` text-primary (Main heading text) · `#5F6368` text-secondary (Description paragraph text) · `#1A73E8` accent (Links and primary icons) · `#34A853` semantic (Success checkmark on Hardware Disk) · `#E0E0E0` border (Right edge of sidebar)
- **Typography (samples)**: "Connect Your Memory Card" ~26px/600 #1F1F1F · "Connect your SD card to PC/Laptop" ~14px/400 #5F6368 · "Hardware Disk" ~14px/500 #1F1F1F · "SD card connected but not detected?" ~13px/400 #1F1F1F
- **Heights**: Sidebar Item 48px · Header Bar 42px
- **Radii**: Sidebar Selection r8 · Buttons/Tags (SSR/SDR) r4
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width for line icons; filled style for decorative illustrations.; corners: Rounded corners on all icon paths.; notable: hardware-disk, sd-card, tools, file-repair, partition-rescue
- **States visible**: selected
- **Motion hints**: Loading spinner visible next to the main heading title.
- **Empty/loading/error visuals**: Empty state design present: 'SD card connected but not detected' with help links and feature cards (SDR, SSR, DVR).
- **Ambiguities**: Exact pixel dimensions of the window are estimated based on standard 1080p scaling. | Some text in the bottom right status area is truncated.

#### `02-landing/landing-solution-wizard-1of3.png`

- **Identity**: Select Location / Scan Options / Active with Overlay (Help/Tip) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F6FA` background (Main window background) · `#FFFFFF` surface (Drive selection card) · `#2B7FFF` card (Blue tip overlay background) · `#1A1A1A` text-primary (Main heading text) · `#666666` text-secondary (Sub-labels like 'Internal drives') · `#0078D4` accent (Primary 'Search' button) · `#28A745` semantic (Checkmark icon on 'Hardware Disk') · `#E0E0E0` border (Outline of drive card) · `#EEEEEE` divider (Separator lines in sidebar)
- **Gradients**: horizontal Progress bar inside Windows(C:) card: #0078D4 → #005BB5
- **Typography (samples)**: "Select a location to search" ~22px/600 #1A1A1A · "Windows(C:)" ~16px/600 #1A1A1A · "28.28 GB/149.45 GB" ~12px/400 #666666 · "Solution 1:" ~15px/600 #FFFFFF
- **Heights**: Primary Button (Search) 44px · Secondary Button (Next) 36px · Sidebar Item 48px
- **Radii**: Buttons/Cards r6 · Overlay r8
- **Shadows**: blur24/y8/20% · blur8/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled for decorative/mascot.; corners: Slight rounding on outer corners of icons.; notable: Hardware Disk, SD Card, Recycle Bin, Desktop, Folder
- **States visible**: selected, hover (implied by overlay context)
- **Motion hints**: None (static screenshot)
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and compression. | The blue overlay's exact classification (tooltip vs modal) is ambiguous without interaction.

#### `02-landing/landing-solution-wizard-2of3.png`

- **Identity**: Data Recovery - Select Location / Modal Overlay (Wizard Step 2/3) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (Modal card background) · `#E8EDF4` card (Background modal surface) · `#2B2F36` text-primary (Main heading text) · `#6B7280` text-secondary (Description text in modals) · `#007AFF` accent (Primary action button background) · `#10B981` semantic (Checkmark icon on Hardware Disk) · `#D1D5DB` border (Input field border) · `#E5E7EB` divider (Section dividers in sidebar)
- **Gradients**: vertical Foreground Modal Background: #3B82F6 → #2563EB
- **Typography (samples)**: "Select a location to search" ~22px/600 #2B2F36 · "Solution 2:" ~16px/700 #FFFFFF · "Scan the entire disk." ~14px/400 #FFFFFF · "Hardware Disk" ~14px/600 #2B2F36
- **Heights**: Primary Button (Next) 38px · Input Field (Select Location) 48px · Sidebar Item 44px
- **Radii**: Modals and Buttons r8 · Input Fields r6
- **Shadows**: blur25/y10/15% · blur10/y4/10%
- **Iconography**: dual-register; stroke: consistent 1.5px or 2px stroke width; corners: slightly rounded; notable: Hardware Disk, SD Card, File Repair, Windows Drive
- **States visible**: selected
- **Ambiguities**: Text behind the foreground modal is partially obscured. | Exact hex values for gradients are estimated from the blue overlay.

#### `02-landing/landing-solution-wizard-3of3.png`

- **Identity**: Data Recovery - Hardware Disk / Modal Overlay (Solution 3) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F4F8` background (Main window background behind modal) · `#FFFFFF` surface (Sidebar and Modal background) · `#FFFFFF` card (Drive selection cards) · `#1F2937` text-primary (Main heading 'Select a location...') · `#6B7280` text-secondary (Subtext 'Internal drives (2)') · `#2563EB` accent ('Search Entire Disk' button background) · `#3B82F6` semantic (Modal dialog background) · `#E5E7EB` border (Input field borders) · `#E5E7EB` divider (Separators in sidebar)
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2937 · "Solution 3:" ~18px/600 #FFFFFF · "Reconnect your drive and try again." ~14px/400 #EFF6FF · "Hardware Disk" ~15px/600 #1F2937
- **Heights**: Primary Button (Search Entire Disk) 40px · Secondary Button (Got it) 36px · Input Field 44px
- **Radii**: Modal / Cards / Buttons r8 · Input fields r4
- **Shadows**: blur25/y10/20% · blur8/y2/8%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width; corners: slightly rounded; notable: Hardware Disk, SD Card, File Repair, Cloud/Sync, Info circle
- **States visible**: selected, disabled, loading
- **Motion hints**: Progress bars on drive cards indicate scanning/recovery status.
- **Empty/loading/error visuals**: Error state visible in modal ('Unable to find your hard drive?').
- **Ambiguities**: Exact hex values for the blue gradient/overlay on the modal due to anti-aliasing. | Some text obscured by the central modal overlay.

#### `02-landing/landing-solution-wizard-closed.png`

- **Identity**: Data Recovery - Hardware Disk Selection / Modal Overlay (Help/Device Detection) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (Modal dialog background) · `#F8F9FC` card (Selected sidebar item background) · `#1F2329` text-primary (Main heading text) · `#6B7785` text-secondary (Description text and labels) · `#2D6CFF` accent (Primary buttons and progress bars) · `#FFA500` semantic (Warning icon and recovery progress bar) · `#E4E7ED` border (Modal border and dividers)
- **Typography (samples)**: "Select a location to search" ~22px/600 #1F2329 · "Hardware Disk" ~15px/600 #1F2329 · "Virtual_Disk NVME Pre..." ~16px/700 #1F2329 · "150 GB" ~13px/400 #6B7785
- **Heights**: Primary Button 42px · Sidebar Item 50px
- **Radii**: Buttons and Cards r6 · Modal Container r10
- **Shadows**: blur30/y10/15%
- **Iconography**: dual-register; stroke: Consistent 2px stroke for line icons; filled style for brand/application icons.; corners: Slightly rounded corners on square icons.; notable: Hardware Disk, SD Card, File Repair, Windows Drive, Recovery Warning
- **States visible**: selected, loading
- **Motion hints**: Progress bars indicate active scanning or loading state.
- **Empty/loading/error visuals**: None (Content is populated with drive data)
- **Ambiguities**: Exact pixel dimensions are estimates based on standard Windows DPI scaling. | Some text is truncated with ellipses.


### 3.3 03-scan-phase — P06 Scan+Results (CDetailPage) — scan states

#### `03-scan-phase/scan-after-stop-attempt.png`

- **Identity**: Scanning Progress / Pause Dialog / Active Scanning (Paused/Interrupted by Modal) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F2F5` background (Main app background behind file list) · `#FFFFFF` surface (Dialog box background and file rows) · `#333333` text-primary (Dialog title 'Stop Searching...') · `#666666` text-secondary (Dialog description text) · `#1677FF` accent ('Continue' button background) · `#FFCCC7` semantic ('Stop' button background) · `#D9D9D9` border (Dialog border or input fields) · `#E8E8E8` divider (Separators in the sidebar)
- **Gradients**: none (conical) Progress circle in status bar: #1677FF → #E8E8E8
- **Typography (samples)**: "Stop Searching for Lost Data?" ~18px/600 #333333 · "1139164" ~28px/700 #333333 · "Files found" ~13px/400 #666666 · "00:02:38" ~24px/600 #333333
- **Heights**: Primary Button (Continue) 40px · Secondary Button (Stop) 40px · Search Input 36px · List Item Row 44px
- **Radii**: Dialog container r8 · Buttons r4
- **Shadows**: blur24/y8/20% · blur12/y4/10%
- **Iconography**: dual-register (filled and line mixed); stroke: consistent 1.5px-2px stroke width for line icons; corners: slightly rounded corners on geometric shapes; notable: folder, pause-circle, search, filter, menu
- **States visible**: selected, loading
- **Motion hints**: Circular progress indicator at 22%, scanning animation implied.
- **Empty/loading/error visuals**: Loading state visible via progress bar and 'Searching...' text in status bar.
- **Ambiguities**: Exact hex codes are estimates due to image compression. | Some text in the background is obscured by the modal overlay.

#### `03-scan-phase/scan-midscan-210s.png`

- **Identity**: File Browser / Scan Results / Scanning in Progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F9FF` background (Main window background) · `#FFFFFF` surface (List items and sidebar background) · `#3B82F6` accent ('Buy Now' button and 'Open' button) · `#1F2937` text-primary (Folder names in list) · `#6B7280` text-secondary (Date modified and file type text) · `#DBEAFE` semantic (Selected row highlight) · `#E5E7EB` border (Dividers between sidebar and list) · `#60A5FA` gradient-stop (Progress ring fill)
- **Gradients**: angular Progress Ring: #3B82F6 → #93C5FD
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~14px/600 #1F2937 · "actionarchivecache" ~14px/400 #1F2937 · "10/7/2026 11:30 PM" ~13px/400 #4B5563 · "Advanced Scanning" ~15px/700 #111827
- **Heights**: Primary Button (Recover All) 42px · Secondary Button (Open) 36px · List Item Row 44px
- **Radii**: Buttons r6 · Search bar r20
- **Shadows**: blur8/y4/15% · blur4/y2/5%
- **Iconography**: dual-register; stroke: consistent 1.5px-2px stroke width for line icons; filled style for folder icons.; corners: rounded; notable: folder, search, filter, home, arrow-left
- **States visible**: selected, loading, hover
- **Motion hints**: Circular progress indicator at 21% with animated gradient; progress bar in status bar partially filled.
- **Empty/loading/error visuals**: Loading state visible via 'Searching...' text and progress indicators.
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and image compression. | Some text in the status bar ('Searching...') is truncated.

#### `03-scan-phase/scan-midscan-21pct.png`

- **Identity**: File Browser / Scan Results / Scanning in Progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (List view rows and sidebar) · `#FFFFFF` card (Details panel container) · `#2B2F36` text-primary (Folder names in list) · `#6B7280` text-secondary (Date modified and file type text) · `#3B82F6` accent ('Buy Now' button, 'Open' button, progress ring) · `#DBEAFE` semantic (Selected row highlight (actionarchivecache)) · `#E5E7EB` border (Dividers between regions) · `#E5E7EB` divider (Horizontal lines between list rows) · `#60A5FA` gradient-stop (Progress circle fill)
- **Gradients**: circular Progress Ring: #3B82F6 → #93C5FD
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~13px/400 #1F2937 · "Windows (C:)" ~14px/600 #111827 · "actionarchivecache" ~14px/400 #1F2937 · "10/7/2026 11:30 PM" ~13px/400 #4B5563 · "Advanced Scanning" ~15px/700 #111827
- **Heights**: Primary Button (Recover All) 40px · Secondary Button (Open) 36px · List Row 44px · Search Input 36px
- **Radii**: Buttons r6 · Search/Input fields r4 · Progress Circle Container r50
- **Shadows**: blur8/y4/15% · blur4/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke width, clean geometric paths.; corners: Slightly rounded corners on square icons.; notable: folder-blue, home, arrow-left, arrow-up, filter
- **States visible**: selected, loading
- **Motion hints**: Circular progress indicator at 21%, progress bar at bottom of list.
- **Empty/loading/error visuals**: Loading state visible via progress indicators and 'Searching...' status text.
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but not verifiable from raster. | Some text in the status bar is truncated.

#### `03-scan-phase/scan-midscan-34pct-prior.png`

- **Identity**: Scan Results / File Browser / Scanning in Progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F9FF` background (Main window background) · `#FFFFFF` surface (List row background / Cards) · `#3B82F6` accent (Primary buttons / Progress ring) · `#1F2937` text-primary (Folder names in list) · `#6B7280` text-secondary (Date modified text) · `#DBEAFE` semantic-info (Selected row highlight) · `#E5E7EB` border (Divider lines / Input borders) · `#EFF6FF` gradient-stop (Top of window gradient)
- **Gradients**: vertical Window Background: #FFFFFF → #F0F7FF
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/400 #1F2937 · "actionarchivecache" ~14px/400 #111827 · "10/7/2026 11:30 PM" ~13px/400 #4B5563 · "Advanced Scanning" ~14px/600 #111827
- **Heights**: Recover All button 40px · Search input 36px · List Row 44px
- **Radii**: Buttons / Inputs r6 · Preview Card r8
- **Shadows**: blur10/y4/15% · blur4/y2/5%
- **Iconography**: filled (folders) and line (actions); stroke: consistent 1.5-2px stroke for line icons; corners: rounded; notable: folder-blue, search, filter, home, refresh
- **States visible**: selected, loading
- **Motion hints**: Progress bar at bottom (34% complete), circular progress indicator in status bar.
- **Empty/loading/error visuals**: Loading state visible via progress bar and 'Searching...' text.
- **Ambiguities**: Exact hex codes may vary slightly due to anti-aliasing and compression. | Some text in the status bar path is truncated.

#### `03-scan-phase/scan-paused.png`

- **Identity**: Scan Results / File Browser / Scanning (Paused) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main app background) · `#FFFFFF` surface (File list rows and sidebar) · `#FFFFFF` card (Right-side preview panel) · `#1F2329` text-primary (Folder names in list) · `#6B7785` text-secondary (Date modified and file type text) · `#2D8CF0` accent ('Buy Now' button and 'Open' button) · `#E6A23C` semantic ('Buy Now' button background) · `#DCDFE6` border (Input field borders and dividers) · `#EBEEF5` divider (Horizontal lines between list rows) · `#409EFF` gradient-stop (Progress circle fill)
- **Gradients**: horizontal Progress bar at bottom status area: #409EFF → #409EFF
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~14px/400 #1F2329 · "actionarchivecache" ~14px/400 #1F2329 · "10/7/2026 11:30 PM" ~13px/400 #6B7785 · "Recover All ..." ~14px/600 #FFFFFF
- **Heights**: List Row 48px · Primary Button (Recover All) 40px · Search Input 36px
- **Radii**: Buttons and Inputs r4 · Preview Card r8
- **Shadows**: blur8/y2/10% · blur4/y1/5%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke for line icons; filled for folder icons.; corners: slightly rounded on filled icons; sharp on line icons.; notable: folder-blue, home, arrow-left, arrow-up, filter
- **States visible**: selected, loading/paused, hover (implied by cursor or focus)
- **Motion hints**: Circular progress indicator (21%), horizontal progress bar, 'Scanning Paused' blinking/text state.
- **Empty/loading/error visuals**: Loading/Scanning state is active with progress metrics displayed.
- **Ambiguities**: Exact hex values may vary slightly due to image compression. | Some text in the status bar path is truncated.

#### `03-scan-phase/scan-resumed.png`

- **Identity**: File Browser / Scan Results / Scanning in Progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F5F5` background (Main window background) · `#FFFFFF` surface (List view rows and sidebar) · `#3B82F6` accent (Primary 'Recover All' button) · `#1F2937` text-primary (Folder names in list) · `#6B7280` text-secondary (Date modified and file type text) · `#DBEAFE` semantic (Selected/Hovered row background) · `#E5E7EB` border (Divider lines between regions) · `#60A5FA` gradient-stop (Progress ring fill)
- **Gradients**: horizontal Progress bar at bottom: #3B82F6 → #60A5FA
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~14px/600 #1F2937 · "actionarchivecache" ~14px/400 #1F2937 · "10/7/2026 11:30 PM" ~13px/400 #4B5563 · "Advanced Scanning" ~14px/600 #1F2937
- **Heights**: Primary Button (Recover All) 40px · List View Row 44px · Search Input 36px
- **Radii**: Buttons and Inputs r6 · Progress Ring container r24
- **Shadows**: blur8/y4/15% · blur3/y1/5%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke for line icons; filled style for folder icons and brand logo.; corners: Slightly rounded corners on square icons; circular containers for status icons.; notable: folder-blue, search, filter, home, arrow-left
- **States visible**: selected, loading, hover
- **Motion hints**: Progress bar animation (blue fill moving right), progress ring at 22%.
- **Empty/loading/error visuals**: Loading state visible via progress bar and 'Searching...' text.
- **Ambiguities**: Exact pixel measurements are estimates based on standard UI scaling. | Some text in the status bar is truncated.

#### `03-scan-phase/scan-stop-confirm-dialog.png`

- **Identity**: Scanning Progress / Search Results / Modal Dialog (Pause/Stop Confirmation) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#E8EBF0` background (Main window background behind modal) · `#FFFFFF` surface (Modal dialog background) · `#1F2329` text-primary (Modal title 'Stop Searching...') · `#5F6672` text-secondary (Description text in modal) · `#1677FF` accent ('Continue' button background) · `#FF4D4F` semantic ('Stop' button text/border) · `#D9D9D9` border (Input field borders) · `#F0F0F0` divider (List row separators)
- **Typography (samples)**: "Stop Searching for Lost Data?" ~18px/600 #1F2329 · "1139164" ~28px/700 #1F2329 · "Files found" ~13px/400 #5F6672 · "Advanced Scanning" ~14px/600 #1F2329
- **Heights**: Primary Button (Continue) 38px · Secondary Button (Stop) 38px · Search Input 36px
- **Radii**: Modal dialog r8 · Buttons r4 · Search input r4
- **Shadows**: blur24/y8/20% · blur4/y2/10%
- **Iconography**: dual-register; stroke: consistent 1.5px-2px stroke for outlines, filled for accents; corners: rounded; notable: folder, pause-circle, search, filter, close
- **States visible**: selected, loading, disabled-background
- **Motion hints**: Progress bar at 22%, timer counting up (00:02:38), spinner implied by pause icon context.
- **Empty/loading/error visuals**: Loading state visible via progress bar and scanning status text.
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and screenshot compression. | Some text in the background list is obscured by the modal overlay.


### 3.4 04-scan-complete — P08a Celebration modal (standalone 540×380)

#### `04-scan-complete/complete-celebration-modal-v2.png`

- **Identity**: Scan Completion / Results Summary / Success / Modal Overlay — theme light, density dialog, VLM confidence high
- **Palette (VLM sampled)**: `#EAEFF5` background (Main window background) · `#FFFFFF` surface (Modal dialog background) · `#F5F7FA` card (Stats boxes in modal) · `#1F2329` text-primary (Modal title text) · `#6B7280` text-secondary (Labels like 'Files found') · `#2563EB` accent ('Go to Recover' button) · `#10B981` semantic (Checkmark icon in success banner) · `#D1D5DB` border (Modal outer border) · `#E5E7EB` divider (Line between stats in modal) · `#3B82F6` gradient-stop (Shield icon gradient top)
- **Gradients**: vertical Shield/Star icon in modal center: #60A5FA → #2563EB
- **Typography (samples)**: "Congratulations! Scan Completed!" ~18px/600 #1F2329 · "1139229" ~28px/700 #111827 · "Files found" ~13px/400 #6B7280 · "Go to Recover" ~14px/600 #FFFFFF · "Windows (C:)" ~14px/500 #374151
- **Heights**: Primary Button (Go to Recover) 38px · List Item Row 44px · Search Bar (Header) 34px
- **Radii**: Modal dialog r8 · Buttons r6 · Input fields r4
- **Shadows**: blur25/y10/15% · blur3/y1/5%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width for line icons; filled style for folder icons and brand logo.; corners: Slightly rounded corners on geometric shapes.; notable: shield with star, folder, checkmark circle, shopping cart
- **States visible**: selected, disabled (background)
- **Motion hints**: None (static success state)
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and screen capture compression. | Some text in the background list is obscured by the modal.

#### `04-scan-complete/complete-celebration-modal.png`

- **Identity**: Scan Results / Recovery Wizard / Scan Completed (Modal Overlay) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (main app background behind modal) · `#FFFFFF` surface (modal dialog box) · `#F8F9FB` card (stats boxes inside modal) · `#333333` text-primary (modal title 'Congratulations') · `#666666` text-secondary ('Files found' label) · `#3B82F6` accent ('Go to Recover' button) · `#E0F2FE` semantic (success message background) · `#E5E7EB` border (modal outer border) · `#E5E7EB` divider (line between stats in modal)
- **Gradients**: horizontal header bar: #A0AEC0 → #CBD5E0
- **Typography (samples)**: "Congratulations! Scan Completed!" ~18px/600 #1F2937 · "1139261" ~28px/700 #111827 · "Great news: Your files are..." ~14px/600 #2563EB
- **Heights**: primary button 40px · search input 36px · list row 44px
- **Radii**: modal dialog r8 · buttons r6 · input fields r4
- **Shadows**: blur24/y12/15% · blur8/y4/10%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width; corners: slightly rounded; notable: shield-check, folder-blue, check-circle-green
- **States visible**: selected
- **Ambiguities**: Exact hex values for the header gradient are estimates due to anti-aliasing. | Some text in the background list is obscured by the modal overlay.


### 3.5 05-results — P06 Results explorer — 3-pane

#### `05-results/results-baseline-v2.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection in progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (main window background) · `#FFFFFF` surface (file list rows and cards) · `#2B7EF6` accent ('Recover All' button and 'Open' button) · `#1F2329` text-primary (folder names in the list) · `#6B7785` text-secondary (dates and file types) · `#00B578` semantic-success (checkmark icon in status bar) · `#E4E7ED` border (dividers between regions) · `#E8F1FF` selection (selected 'inetpub' row background)
- **Typography (samples)**: "actionarchivecache" ~14px/400 #1F2329 · "10/7/2026 11:30 PM" ~13px/400 #6B7785 · "Scan Completed" ~15px/600 #1F2329 · "1139229" ~16px/700 #2B7EF6
- **Heights**: Primary Button (Recover All) 42px · Secondary Button (Open) 36px · List Row 44px · Search Input 36px
- **Radii**: Buttons and Cards r6 · AI Popup/Toast r10 · Search/Input fields r4
- **Shadows**: blur24/y8/15% · blur8/y2/5%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width for line icons; filled style for folder icons.; corners: rounded; notable: folder, checkbox, filter, search, checkmark-circle
- **States visible**: selected, hover-implied
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling. | Some text in the right panel is partially obscured by the AI popup.

#### `05-results/results-baseline.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection in progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (Sidebar and Preview panels) · `#2B7EF6` accent ('Buy Now' button and 'Recover All' button) · `#1F2329` text-primary (Folder names in the list) · `#8A919F` text-secondary (Date modified and file type text) · `#2B7EF6` semantic-success (Checkmark icon in status bar) · `#E4E7ED` border (Dividers between regions) · `#EBF2FF` selection (Selected 'inetpub' row background) · `#FFFFFF` card (AI Recommend popup background)
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~14px/400 #1F2329 · "actionarchivecache" ~14px/400 #1F2329 · "10/7/2026 11:30 PM" ~13px/400 #8A919F · "Scan Completed, Files Found:" ~15px/600 #1F2329 · "1139250" ~18px/700 #2B7EF6
- **Heights**: Primary Button (Recover All) 40px · List Row 44px · Search Input 36px
- **Radii**: Buttons r6 · AI Popup r8 · Search/Input fields r4
- **Shadows**: blur16/y4/15% · blur8/y2/10%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke for line icons; flat filled for folder icons.; corners: rounded; notable: folder, checkbox, filter, search, checkmark-circle
- **States visible**: selected, hover-implied
- **Motion hints**: Progress bar visible at bottom of list (scan completion indicator).
- **Ambiguities**: Exact pixel values for shadows are estimated based on visual blur. | Some text in the list is truncated.

#### `05-results/results-context-menu-open-v2.png`

- **Identity**: File Browser / Scan Results / Scan Completed, Context Menu Open — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (main window background) · `#FFFFFF` surface (list rows and panels) · `#E8F0FE` card (selected list row) · `#1F1F1F` text-primary (folder names) · `#5F6368` text-secondary (dates and file types) · `#1677FF` accent ('Open' button and links) · `#1677FF` semantic ('AI Recommend' floating button) · `#E0E0E0` border (window borders and dividers) · `#F0F0F0` divider (horizontal lines between list rows) · `#FFD700` gradient-stop ('Buy Now' button background)
- **Gradients**: vertical Title bar background: #FFFFFF → #F0F4FA
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/400 #1F1F1F · "actionarchivecache" ~14px/400 #1F1F1F · "10/7/2026 11:30 PM" ~13px/400 #5F6368 · "Scan Completed" ~14px/600 #1F1F1F · "1139229" ~16px/700 #1677FF
- **Heights**: List Row 44px · Primary Button (Recover) 40px · Secondary Button (Open) 36px · Context Menu Item 36px
- **Radii**: Buttons r6 · Context Menu r8 · Search Bar r20
- **Shadows**: blur16/y4/15% · blur12/y4/20%
- **Iconography**: dual-register; stroke: consistent 1.5px-2px stroke width for line icons; filled style for folder icons.; corners: slightly rounded on filled icons; sharp or slightly rounded on line icons.; notable: Folder, Checkbox, Filter, Search, Menu (Hamburger)
- **States visible**: selected, hover
- **Motion hints**: Progress bar visible at bottom of the file list indicating scroll position or loading state.
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and compression. | Some text in the context menu is obscured by the 'AI Recommend' button.

#### `05-results/results-context-menu-open.png`

- **Identity**: Scan Results / File Explorer / Post-scan, context menu open — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (main app background) · `#FFFFFF` surface (file list rows and panels) · `#E8F4FD` card (selected row 'ghcup') · `#1F2329` text-primary (file names) · `#6B7785` text-secondary (dates and file sizes) · `#2E7CF6` accent ('Open' button and 'Recover All' button) · `#00A86B` semantic (checkmark icon in status bar) · `#DFE4EA` border (context menu border and dividers) · `#EBEFF3` divider (horizontal lines in list)
- **Gradients**: none none visible: 
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/400 #1F2329 · "ghcup" ~14px/400 #1F2329 · "Recover" ~13px/400 #1F2329 · "Scan Completed" ~15px/600 #1F2329
- **Heights**: list row 48px · primary button 40px · search bar 36px
- **Radii**: buttons r6 · context menu r8 · search bar r4
- **Shadows**: blur12/y4/15% · blur6/y2/10%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width; corners: slightly rounded; notable: folder-blue, refresh-arrow, eye-preview, check-circle
- **States visible**: selected, hover-context-menu
- **Motion hints**: Progress bar visible at bottom of file list (grey track).
- **Empty/loading/error visuals**: None (data is populated).
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif. | Some text in the right panel is obscured by the context menu.

#### `05-results/results-context-menu-round4.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection in progress — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (Main window background) · `#FFFFFF` surface (List item background) · `#E8F4FD` card (Selected list row) · `#333333` text-primary (Folder names) · `#666666` text-secondary (Date/Type columns) · `#2B7EF6` accent (Recover All button) · `#28A745` semantic (Checkmark icon in status bar) · `#E0E0E0` border (Divider lines between panes) · `#F0F0F0` divider (Horizontal line under header) · `#1E60E0` gradient-stop (AI Recommend popup top)
- **Gradients**: vertical AI Recommend Popup: #2B85FF → #1866EB
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/400 #000000 · "actionarchivecache" ~14px/400 #000000 · "10/7/2026 11:30 PM" ~13px/400 #555555 · "Recover All ..." ~15px/600 #FFFFFF · "Scan Completed" ~14px/600 #000000
- **Heights**: Primary Button (Recover) 42px · Secondary Button (Open) 36px · List Row 44px · Tab/Button Group (Path/Type) 32px
- **Radii**: Buttons r6 · AI Popup r10 · Search/Filter fields r4
- **Shadows**: blur20/y8/25% · blur8/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for outlines; solid fills for folder icons.; corners: Slightly rounded corners on rectangular icons.; notable: folder-blue, checkbox-square, filter-funnel, checkmark-circle
- **States visible**: selected, hover-implied
- **Motion hints**: Progress bar visible at bottom of file list (scan completion indicator).
- **Ambiguities**: Exact pixel dimensions depend on assumed DPI scaling (likely 100% or 125%). | Some text in the Date Modified column is partially obscured by the AI popup.

#### `05-results/results-filter-bar-open-clean.png`

- **Identity**: Scan Results / File Browser / Post-scan, item selected — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FC` background (Main window background) · `#FFFFFF` surface (Sidebar and List headers) · `#FFFFFF` card (Details panel container) · `#1F2329` text-primary (Folder names in list) · `#8A919F` text-secondary (Date modified text) · `#2B7EF6` accent ('Open' button and 'Recover All' button) · `#E6F0FF` semantic (Selected row background) · `#E4E7ED` border (Divider lines and input borders) · `#EBEDF0` divider (Horizontal lines in list) · `#FFD666` gradient-stop ('Buy Now' button background)
- **Gradients**: horizontal Buy Now button: #FFD666 → #FFAA00
- **Typography (samples)**: "actionarchivecache" ~14px/400 #1F2329 · "10/7/2026 11:30 PM" ~13px/400 #8A919F · "Scan Completed" ~16px/600 #1F2329 · "1139234" ~18px/700 #2B7EF6 · "Windows (C:)" ~14px/600 #1F2329
- **Heights**: Filter Button 36px · Search Input 36px · Recover All Button 44px · List Row 44px
- **Radii**: Buttons (Open/Filter) r6 · Search Input r18 · Details Panel Card r8
- **Shadows**: blur8/y2/8% · blur12/y4/12%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke for outlines; solid fills for folder icons.; corners: Slightly rounded corners on rectangular icons; circular backgrounds for status icons.; notable: folder-blue, folder-yellow, check-circle, filter-funnel, ai-sparkle
- **States visible**: selected, hover (implied by cursor proximity), active-tab
- **Motion hints**: None (static screenshot, though progress bar implies recent motion).
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and compression. | Some icons in the system tray are partially obscured.

#### `05-results/results-filter-bar-open.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection active, AI recommendation overlay visible — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (Main window background) · `#FFFFFF` surface (List view row background) · `#E8F4FD` card (Selected list row) · `#333333` text-primary (Folder names in list) · `#666666` text-secondary (Date modified text) · `#2D8CF0` accent ('Recover All' button background) · `#1677FF` semantic (AI Recommendation popup background) · `#E0E0E0` border (Window borders and dividers) · `#F0F0F0` divider (Horizontal lines between list items) · `#FFA500` gradient-stop ('Buy Now' button background)
- **Gradients**: linear AI Recommend icon background: #9B59B6 → #3498DB
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~13px/400 #000000 · "actionarchivecache" ~14px/400 #000000 · "10/7/2026 11:30 PM" ~13px/400 #666666 · "Scan Completed, Files Found:" ~15px/600 #000000 · "1139250" ~18px/700 #2D8CF0
- **Heights**: Recover All Button 40px · Open Button (Preview) 36px · View Details Button 36px · List Row 44px
- **Radii**: Buttons (Recover All) r6 · AI Popup r8 · Search Bar r4
- **Shadows**: blur24/y8/15% · blur12/y4/10%
- **Iconography**: dual-register (filled folders, line-based UI icons); stroke: consistent 1.5-2px stroke width for line icons; corners: rounded (approx 2px radius on inner corners); notable: folder-filled, checkbox-square, filter-funnel, search-magnifier, ai-sparkle-bot
- **States visible**: selected, hover (implied by cursor or focus), loading (progress bar visible at bottom of list)
- **Motion hints**: Progress bar at the bottom of the file list indicates ongoing background activity or finalization of scan.
- **Empty/loading/error visuals**: Progress bar visible; 'Scan Completed' status indicates transition from loading to ready state.
- **Ambiguities**: Exact pixel dimensions depend on assumed DPI scaling (likely 100% or 125%). | Some text in the list is truncated ('11:25 PM'). | The 'AI Recommend' bubble obscures part of the preview pane.

#### `05-results/results-filter-path-dropdown.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection active — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F4F7FA` background (main window background) · `#FFFFFF` surface (file list rows and panels) · `#F0F6FF` card (selected file row background) · `#333333` text-primary (folder names in list) · `#888888` text-secondary (dates and sizes in list) · `#3B82F6` accent ('Open' button and 'Recover All' button) · `#10B981` semantic (checkmark icon in status bar) · `#E5E7EB` border (dividers between regions) · `#F3F4F6` divider (horizontal lines between list items)
- **Gradients**: vertical Window Title Bar: #EBF2FF → #F4F7FA
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~14px/400 #000000 · "actionarchivecache" ~14px/500 #111827 · "File folder" ~13px/400 #666666 · "Scan Completed" ~15px/600 #111827
- **Heights**: Primary Button (Recover) 42px · Secondary Button (Open) 36px · List Row Height 44px
- **Radii**: Buttons r6 · Search/Filter inputs r20 · Right panel container r8
- **Shadows**: blur8/y2/15% · blur4/y0/5%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke for outlines, solid fills for specific semantic icons; corners: slightly rounded corners on rectangular icons; notable: Folder (blue filled), Checkmark (green circle), Filter funnel, Home/Hard Drive
- **States visible**: selected, hover (implied by cursor or focus)
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling. | Some text in the far-right panel is partially obscured or truncated.

#### `05-results/results-path-tab.png`

- **Identity**: File Selection / Scan Results / Post-scan, selection mode — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (Card backgrounds and header) · `#F0F4F9` card (Folder grid item background) · `#333333` text-primary (File names and menu text) · `#888888` text-secondary (File sizes in sidebar) · `#3B82F6` accent (Recover All button and selected border) · `#10B981` semantic (Checkmark icon in status bar) · `#E2E8F0` border (Divider lines and card borders) · `#8B5CF6` gradient-stop (Assistant icon gradient end)
- **Gradients**: diagonal Bottom-right assistant icon: #3B82F6 → #8B5CF6
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~14px/600 #000000 · "Pictures" ~14px/600 #1E40AF · "jpg" ~13px/400 #333333 · "Scan Completed" ~14px/600 #333333 · "Recover All ..." ~14px/600 #FFFFFF
- **Heights**: Recover All Button 44px · Search Bar 36px · Grid Item Card 140px
- **Radii**: Buttons and Cards r8 · Search Bar r20
- **Shadows**: blur12/y4/10% · blur6/y2/20%
- **Iconography**: filled; stroke: Consistent 1.5-2px stroke where outlines exist (e.g., checkmarks).; corners: Rounded corners on all icons.; notable: Folder (Blue), Checkmark (Green), Filter Funnel, Grid View
- **States visible**: selected
- **Ambiguities**: Exact pixel dimensions depend on display scaling (likely 100% or 125%).

#### `05-results/results-row-selected-gate-dismissed.png`

- **Identity**: Scan Results / File Browser / Post-Scan (Selection Mode) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (Main window background) · `#FFFFFF` surface (Sidebar and List area background) · `#FFFFFF` card (Preview panel container) · `#1F2329` text-primary (Folder names in list) · `#8A9199` text-secondary (Date modified and file type text) · `#2B7BFF` accent ('Recover All' button and 'Open' button) · `#E6F0FF` semantic (Selected row 'ghcup' background) · `#E4E7ED` border (Divider lines between header and content) · `#EFF2F5` divider (Horizontal lines between list rows)
- **Gradients**: vertical Header/Toolbar area: #FFFFFF → #F5F8FA
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~14px/400 #1F2329 · "ghcup" ~14px/600 #1F2329 · "10/8/2026 12:32 AM" ~13px/400 #8A9199 · "Recover All ..." ~14px/600 #FFFFFF
- **Heights**: Primary Button (Recover) 42px · List Row 44px · Search Input 36px
- **Radii**: Buttons r6 · Search/Input fields r20 · Preview card r8
- **Shadows**: blur8/y2/8% · blur4/y1/5%
- **Iconography**: dual-register (filled for folders, line for UI actions); stroke: consistent 1.5px or 2px weight, rounded caps; corners: rounded (approx 2px radius on inner corners); notable: folder-blue-filled, checkbox-square, search-magnifier, filter-funnel, home-house
- **States visible**: selected, hover (implied by cursor), expanded (tree view)
- **Motion hints**: Progress bar visible at bottom of list indicating loading/pagination of results.
- **Empty/loading/error visuals**: Loading state indicated by progress bar at the bottom of the file list; no empty/error states currently visible as data is present.
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but cannot be confirmed without font inspection. | The 'Buy Now' button color is a custom orange/gold gradient, hex is an approximation.

#### `05-results/results-row-selected-round4.png`

- **Identity**: File Browser / Scan Results / Scan Completed, Item Selected — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (Main window background) · `#FFFFFF` surface (List view rows and cards) · `#333333` text-primary (Folder names in list) · `#666666` text-secondary (Date modified and file type text) · `#2F80ED` accent ('Recover All' button and 'Open' button) · `#27AE60` semantic (Checkmark icon in status bar) · `#E3F2FD` selection (Selected 'ghcup' row background) · `#E0E0E0` border (Divider lines and input fields)
- **Gradients**: vertical Folder icon in preview pane: #90CAF9 → #42A5F5
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/400 #000000 · "ghcup" ~14px/400 #333333 · "Scan Completed" ~15px/600 #333333 · "1139246" ~15px/700 #2F80ED
- **Heights**: Recover All button 44px · List row height 44px · Search bar 36px
- **Radii**: Buttons r6 · Search/Filter inputs r4
- **Shadows**: blur6/y2/15% · blur4/y1/5%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width for line icons; filled style for folder icons.; corners: Slightly rounded corners on square icons; organic shapes for folders.; notable: folder-blue, checkmark-circle, filter-funnel, magnifying-glass, menu-list
- **States visible**: selected, hover (implied by cursor)
- **Motion hints**: Progress bar visible at bottom of list view indicating loading or remaining scan data.
- **Empty/loading/error visuals**: Progress bar present at the bottom of the file list.
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but not verifiable from raster. | Some icons in the top right toolbar are partially obscured or generic.

#### `05-results/results-row-selected.png`

- **Identity**: File Browser / Results View / Scan Completed, Item Selected — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F9FF` background (main window background) · `#FFFFFF` surface (folder cards and sidebar background) · `#F0F6FF` card (selected folder card background) · `#333333` text-primary (sidebar folder names) · `#888888` text-secondary (file counts in sidebar) · `#3B82F6` accent ('Recover All' button and selection border) · `#10B981` semantic (checkmark icon in status bar) · `#E5E7EB` border (divider between sidebar and main view)
- **Gradients**: vertical Folder icons: #93C5FD → #DBEAFE
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/600 #000000 · "Pictures" ~15px/600 #111827 · "140.1K" ~13px/400 #9CA3AF · "Recover All ..." ~14px/600 #FFFFFF
- **Heights**: Recover Button 44px · Open Button 36px · Buy Now Badge 32px
- **Radii**: buttons/cards r8 · folder icons r4
- **Shadows**: blur12/y4/10% · blur6/y2/20%
- **Iconography**: filled/dual-register; stroke: consistent 1.5-2px stroke for outlines, solid fills for primary icons; corners: slightly rounded squares/circles; notable: folder-blue, check-circle, filter, grid-view, home
- **States visible**: selected, hover
- **Ambiguities**: Exact pixel dimensions of the window are estimated based on standard DPI scaling. | Some text in the bottom right corner is partially obscured by a floating widget.

#### `05-results/results-search-active.png`

- **Identity**: Scan Results / File List / Completed Scan, Item Selected — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background behind list) · `#FFFFFF` surface (List row background and preview panel) · `#DCEBFF` card (Selected file row highlight) · `#333333` text-primary (File names and header text) · `#666666` text-secondary (Date modified and size values) · `#2D8CF0` accent ('Recover All' button and 'Open' button) · `#E74C3C` semantic (Search clear 'x' icon) · `#DCDFE6` border (Search input border and column dividers)
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~14px/400 #000000 · "-file-system-location" ~14px/400 #333333 · "2/1/1980 12:00 AM" ~13px/400 #666666 · "Recover All ..." ~15px/600 #FFFFFF
- **Heights**: Recover All Button 44px · Search Input 36px · List Row 44px
- **Radii**: Buttons and Search r4 · Preview folder icon container r8
- **Shadows**: blur6/y2/15% · blur10/y4/20%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width for line icons; corners: slightly rounded caps and joins; notable: folder-blue, checkmark-circle, search-magnifier, filter-funnel, back-arrow
- **States visible**: selected, focus
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but not verifiable from raster. | Some text in the path field is truncated with ellipsis.

#### `05-results/results-type-tab-midtransition.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection ready — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (Main window background) · `#FFFFFF` surface (Sidebar and List headers) · `#FFFFFF` card (Details panel background) · `#1F2937` text-primary (Folder names in list) · `#6B7280` text-secondary (Date modified text) · `#2B7EF6` accent ('Recover All' button bg) · `#E6F0FF` semantic (Selected row highlight) · `#E5E7EB` border (Panel dividers) · `#F3F4F6` divider (Row separators in list)
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~13px/400 #111827 · "actionarchivecache" ~14px/400 #1F2937 · "Scan Completed, Files Found:" ~14px/600 #111827 · "1139250" ~18px/700 #2B7EF6
- **Heights**: Primary Button (Recover) 42px · Search Input 36px · List Row 44px
- **Radii**: Buttons r6 · Search/Inputs r4 · Details Card r8
- **Shadows**: blur8/y2/15% · blur4/y1/10%
- **Iconography**: dual-register; stroke: consistent 1.5px stroke for line icons; flat filled for folder icons; corners: slight rounding on outer corners of strokes; notable: folder-blue, check-circle, filter, search, menu
- **States visible**: selected, hover
- **Motion hints**: Progress bar visible at bottom of list view indicating loading or scan progress.
- **Empty/loading/error visuals**: Progress bar present in main list footer.
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but not verifiable from raster. | Some icons in the top right toolbar are partially obscured or generic.


### 3.6 06-preview — P18a Live preview panel

#### `06-preview/preview-panel-html-content.png`

- **Identity**: Scan Results / File List / Selection / Preview — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (main window background) · `#FFFFFF` surface (list row background) · `#E8F0FE` card (selected row 'index.html') · `#1F1F1F` text-primary (file name 'index.html') · `#5F6368` text-secondary (date modified text) · `#1A73E8` accent ('Recover All' button) · `#D9E8F5` semantic (Preview button background) · `#DADCE0` border (divider between list and preview) · `#D4EDDA` semantic-success ('High' recovery chance badge)
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~14px/600 #1F1F1F · "index.html" ~14px/400 #1F1F1F · "2/1/1980 12:00 AM" ~13px/400 #5F6368 · "Recover All ..." ~14px/600 #FFFFFF
- **Heights**: Recover All button 40px · Preview button 36px · List Row 48px
- **Radii**: Buttons r6 · Selected Row r4 · Status Badge r12
- **Shadows**: blur4/y2/15% · blur0/y1/10%
- **Iconography**: dual-register; stroke: consistent 1.5px-2px stroke width, rounded caps; corners: rounded; notable: check-circle, folder-arrow-up, filter-funnel, html-file-icon
- **States visible**: selected, hover (implied by cursor or focus)
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and compression artifacts. | Some text in the preview pane is truncated.

#### `06-preview/preview-panel-live.png`

- **Identity**: File Selection / Recovery Results / Post-scan, file selection — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background) · `#FFFFFF` surface (Card backgrounds and sidebar) · `#FFFFFF` card (Image thumbnail cards) · `#2B2D42` text-primary (File names and labels) · `#8D99AE` text-secondary (File counts and metadata values) · `#3B82F6` accent ('Buy Now' button and 'Preview' button) · `#10B981` semantic ('High' recovery chance label) · `#E2E8F0` border (Card borders and dividers) · `#F1F5F9` divider (Horizontal line above status bar) · `#EFF6FF` gradient-stop (Selected 'jpeg' row in sidebar)
- **Gradients**: horizontal Selected sidebar item background: #FFFFFF → #DBEAFE
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~14px/600 #000000 · "video-00...212.jpeg" ~13px/400 #333333 · "Recovery Chances" ~13px/600 #666666 · "Scan Completed" ~14px/600 #111827
- **Heights**: Primary Button (Recover All) 44px · Secondary Button (Preview) 36px · Search Bar 36px · Thumbnail Card 140px
- **Radii**: Buttons r6 · Cards/Search Bar r4 · Status bar container r20
- **Shadows**: blur8/y2/10% · blur10/y-2/8%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width for outlines; filled style for brand icons.; corners: Slightly rounded corners on rectangular icons.; notable: Home, Arrow Left, Arrow Up, Folder (Pictures), Grid View
- **States visible**: selected, hover (implied by cursor position on first card)
- **Motion hints**: Spinning icon in the 'Recovery Chances' section indicates active calculation/loading state.
- **Empty/loading/error visuals**: None (grid is populated with results).
- **Ambiguities**: Exact font family is sans-serif (likely Segoe UI or similar system font). | Some text in the Path field is truncated ('wind...'). | The spinning icon suggests the 'Recovery Chances' value is being calculated in real-time.


### 3.7 07-recovery — P23 destination picker + P24/P25/P28 funnel dialogs

#### `07-recovery/recovery-after-esc.png`

- **Identity**: Scan Results / File Browser / Post-scan, selection active — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (main window background) · `#FFFFFF` surface (sidebar and list areas) · `#E8F2FF` card (selected list row 'actionarchivecache') · `#1F1F1F` text-primary (folder names in list) · `#6B7280` text-secondary (dates and file types) · `#1D78F9` accent ('Recover All' button and 'Open' button) · `#10B981` semantic (checkmark icon in status bar) · `#E5E7EB` border (divider between sidebar and list) · `#F3F4F6` divider (horizontal lines between list rows)
- **Gradients**: vertical Recover All button: #3B82F6 → #1D4ED8
- **Typography (samples)**: "EaseUS Data Recovery Wizard" ~13px/600 #111827 · "actionarchivecache" ~14px/400 #1F2937 · "10/7/2026 11:30 PM" ~13px/400 #6B7280 · "Scan Completed" ~15px/600 #111827
- **Heights**: Recover All button 40px · Search input 36px · List Row 44px
- **Radii**: Buttons r6 · Search/Inputs r4 · Main Window r8
- **Shadows**: blur4/y0/8% · blur12/y4/15%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke weight, rounded caps; corners: rounded (approx 2-3px radius on outer corners); notable: folder-blue, check-circle-green, search-magnifier, filter-funnel
- **States visible**: selected, hover (implied by cursor), loading (progress bar visible)
- **Motion hints**: Horizontal progress bar in the list view indicates ongoing or recent scanning activity.
- **Empty/loading/error visuals**: Progress bar visible at bottom of list view; 'Scan Completed' status message present.
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and OS-level rendering. | Some icons in the system tray are partially obscured.

#### `07-recovery/recovery-destdlg-choosefolder.png`

- **Identity**: Recovery Destination Dialog / Destination Selection — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F2F5` background (App background behind dialog) · `#FFFFFF` surface (Dialog body) · `#F5F7FA` card (List container background) · `#1F2329` text-primary (Dialog title text) · `#6B7785` text-secondary (Subtitle 'You have...') · `#1677FF` accent (Save button, Selected radio) · `#FF4D4F` semantic (Warning triangle icon) · `#E5E6EB` border (Dialog outer border / Tab container) · `#E5E6EB` divider (Separator lines in list) · `#40A9FF` gradient-stop (Top of folder icon gradient)
- **Gradients**: vertical Folder icon in header: #40A9FF → #1677FF
- **Typography (samples)**: "Choose a destination path" ~16px/600 #1F2329 · "You have 1139246 file(s)" ~13px/400 #6B7785 · "Windows (C:)" ~14px/500 #1F2329 · "31.32 GB/149.45 GB" ~12px/400 #8C8C8C · "Save" ~14px/600 #FFFFFF
- **Heights**: Save Button 36px · List Item Row 72px · Tab Header 36px
- **Radii**: Dialog container r8 · Buttons r4 · Tab container r4
- **Shadows**: blur24/y8/15% · blur12/y4/20%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke for outlines; solid fills for selected states.; corners: Slightly rounded corners on rectangular icons.; notable: folder-blue-gradient, warning-triangle-red, radio-selected, toggle-switch
- **States visible**: selected, disabled-opacity-background
- **Ambiguities**: Exact font family is sans-serif (likely Segoe UI or Inter). | Background window details are obscured by modal overlay dimming.

#### `07-recovery/recovery-destdlg-cloud-tab.png`

- **Identity**: Recovery Destination Selection / Modal Dialog (Cloud Tab Active) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F0F0` background (Main app background behind modal) · `#FFFFFF` surface (Modal dialog background) · `#1F1F1F` text-primary ('Choose a destination path...') · `#666666` text-secondary ('You have 1139246 file(s)...') · `#1677FF` accent (Toggle switch ON state) · `#1890FF` semantic ('Enable file repair' text) · `#D9D9D9` border (Modal outer border / Divider lines) · `#FAFAFA` card (Inactive 'Local' tab background)
- **Gradients**: vertical Header Cloud Icon: #69C0FF → #1890FF
- **Typography (samples)**: "Choose a destination path" ~16px/600 #1F1F1F · "You have 1139246 file(s)" ~13px/400 #666666 · "OneDrive" ~14px/400 #333333 · "Enable file repair" ~14px/600 #1890FF
- **Heights**: Modal Header Area 120px · Tab Bar 40px · Radio List Item 50px · Save Button 36px · Toggle Switch 22px
- **Radii**: Modal container r8 · Tab buttons r4 · Save button r4 · Toggle switch r11
- **Shadows**: blur24/y8/20%
- **Iconography**: dual-register; stroke: Consistent 2px stroke for UI icons; filled/gradient for brand icons.; corners: Rounded corners on all icons.; notable: Cloud, OneDrive, Google Drive, Dropbox, iCloud
- **States visible**: selected, disabled
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling (likely 100% or 125%). | Background application details are obscured by the modal overlay dimming.

#### `07-recovery/recovery-destdlg-early-state.png`

- **Identity**: Recovery Destination Selection Dialog / Active Modal (Save Path Selection) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F6FA` background (Main app background) · `#FFFFFF` surface (Modal background) · `#F3F4F9` card (List container background) · `#1F1F1F` text-primary (Dialog title text) · `#666666` text-secondary (Subtitle 'You have...') · `#1677FF` accent (Save button / Selected radio) · `#FFF7E6` semantic (Warning banner background) · `#FAAD14` semantic (Warning icon/text color) · `#E0E0E0` border (Modal border / Divider lines) · `#EEEEEE` divider (Line between list items)
- **Gradients**: vertical Header Icon (File): #BAE7FF → #1890FF
- **Typography (samples)**: "Choose a destination path..." ~16px/600 #1F1F1F · "You have 1139165 file(s)..." ~13px/400 #666666 · "Windows (C:)" ~14px/600 #000000 · "31.30 GB/149.45 GB" ~12px/400 #888888 · "Save" ~14px/600 #FFFFFF
- **Heights**: Save Button 40px · List Item Row 72px · Tab Header 36px
- **Radii**: Modal Container r8 · Buttons r6 · List Container r8
- **Shadows**: blur30/y10/15%
- **Iconography**: dual-register; stroke: Consistent 2px stroke for line icons; filled for decorative header icon.; corners: Standard rounded corners on rectangles; circles for status indicators.; notable: Folder/File (Blue), Hard Drive, Warning Triangle, Cloud (Tab)
- **States visible**: selected, warning
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling. | Background app content is partially obscured by overlay dimming.

#### `07-recovery/recovery-destdlg-local-selected.png`

- **Identity**: Save Destination Dialog / Active Modal / Selection Required — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F2F5` background (Main app background behind modal) · `#FFFFFF` surface (Modal dialog background) · `#F5F7FA` card (List container background) · `#1F2329` text-primary (Main instruction text) · `#6B7785` text-secondary (File count subtext) · `#1677FF` accent (Save button and selected radio) · `#FFF7E6` semantic (Warning banner background) · `#D9D9D9` border (Modal outer border) · `#E8E8E8` divider (Separator between list items)
- **Gradients**: vertical Header Icon (Document): #BAE0FF → #1677FF
- **Typography (samples)**: "Choose a destination path" ~16px/600 #1F2329 · "You have 1139246 file(s)" ~13px/400 #6B7785 · "Windows (C:)" ~14px/600 #1F2329 · "31.32 GB/149.45 GB" ~12px/400 #1677FF
- **Heights**: Save Button 36px · Tab Buttons (Local/Cloud) 32px · List Item Row 64px
- **Radii**: Modal Container r8 · Buttons r4 · List Container r6
- **Shadows**: blur24/y8/20%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled style for brand/logo icons.; corners: Slight rounding on stroke ends.; notable: folder-blue, drive-windows, warning-triangle, cloud-upload
- **States visible**: selected, hover-implied, disabled-not-visible
- **Empty/loading/error visuals**: Warning state visible (saving to source drive).
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling. | Background window content is partially obscured by overlay.

#### `07-recovery/recovery-destdlg-repair-off.png`

- **Identity**: Recovery Destination Selection / Ready to Save (Destination Selected) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F2F5` background (App background behind dialog) · `#FFFFFF` surface (Dialog body) · `#F5F6F8` card (List item container background) · `#1F2329` text-primary (Choose a destination path...) · `#6B7280` text-secondary (You have 1139246 file(s)...) · `#1677FF` accent (Save button, selected radio) · `#FF4D4F` semantic (Red warning triangle icon) · `#E5E6EB` border (Dialog outer border / list dividers) · `#E5E6EB` divider (Separator lines in list)
- **Gradients**: vertical Blue folder icon at top of dialog: #4DA1FF → #1677FF
- **Typography (samples)**: "Choose a destination path..." ~16px/600 #1F2329 · "Windows (C:)" ~14px/500 #1F2329 · "31.32 GB/149.45 GB" ~12px/400 #6B7280
- **Heights**: Save button 36px · List item row 56px · Tab button (Local) 36px
- **Radii**: Dialog corners r8 · Buttons r4 · List container r8
- **Shadows**: blur24/y10/15%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for line icons; filled for primary actions/folders.; corners: Slight rounding on stroke ends.; notable: folder-blue, disk-drive, warning-triangle, check-circle
- **States visible**: selected, hover (implied by blue highlight on D:)
- **Ambiguities**: Exact pixel values are estimates based on standard 96dpi/100% scaling. | Background window content is partially obscured by overlay.

#### `07-recovery/recovery-destination-dialog-clean.png`

- **Identity**: Recovery Destination Selection / Modal Dialog (Save Location) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F6F8` background (App background behind modal) · `#FFFFFF` surface (Modal background) · `#F9FAFB` card (Tab content area background) · `#1F2937` text-primary (Choose a destination path...) · `#6B7280` text-secondary (You have 1139246 file(s)...) · `#2563EB` accent (Enable file repair text / Toggle) · `#EF4444` semantic (Warning triangle icon) · `#E5E7EB` border (Modal outer border / Tab container) · `#E5E7EB` divider (Line between storage options) · `#60A5FA` gradient-stop (Top of folder icon in header)
- **Gradients**: vertical Header Folder Icon: #93C5FD → #2563EB
- **Typography (samples)**: "Choose a destination path" ~18px/600 #1F2937 · "You have 1139246 file(s)" ~13px/400 #6B7280 · "Windows (C:)" ~14px/600 #111827 · "31.32 GB/149.45 GB" ~12px/400 #EF4444
- **Heights**: Tab Button 36px · List Item Row 64px · Primary Action Button (Recover) 44px · Secondary Action Button (Save) 36px
- **Radii**: Modal Container r8 · Tab Bar / Content Area r6 · Buttons r4
- **Shadows**: blur30/y10/25%
- **Iconography**: dual-register; stroke: Consistent 1.5px-2px stroke width for outlines; filled areas for primary icons.; corners: Slight rounding on rectangular elements.; notable: folder-blue-gradient, warning-triangle-red, warning-triangle-yellow, radio-button-unselected, toggle-switch-on
- **States visible**: selected-toggle
- **Ambiguities**: Exact font family is sans-serif (likely Segoe UI or similar system font); Background window is dimmed/obscured.

#### `07-recovery/recovery-destination-dialog.png`

- **Identity**: Recover Files - Destination Selection / Modal Dialog (Save Path) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F0F0` background (Main app background behind modal) · `#FFFFFF` surface (Modal background and list items) · `#000000` text-primary (Main instruction text) · `#666666` text-secondary (Sub-instruction text) · `#0078D4` accent (Toggle switch, 'Choose folder...' link, 'Recover' button) · `#FF3B30` semantic (Warning triangle icon for low space) · `#E0E0E0` border (List item dividers and tab borders) · `#F9F9F9` card (Tab background area)
- **Typography (samples)**: "Choose a destination path" ~16px/400 #000000 · "You have 1139165 file(s)" ~13px/400 #666666 · "Windows (C:)" ~14px/600 #000000 · "31.30 GB/149.45 GB" ~12px/400 #FF3B30
- **Heights**: List Item Row 72px · Primary Button (Recover) 44px · Secondary Button (Save) 38px
- **Radii**: Modal Window r8 · Buttons/Tabs r4
- **Shadows**: blur30/y10/25%
- **Iconography**: dual-register; stroke: Consistent 2px stroke for line icons (warnings, UI chrome). Filled icons used for file/folder representations.; corners: Rounded corners on filled icons; standard geometric for line icons.; notable: Folder, Hard Drive, Warning Triangle, Cloud, Search
- **States visible**: disabled
- **Ambiguities**: Exact hex values for gradients on the main header icon are estimated. | Some text in the background window is obscured by the modal overlay.

#### `07-recovery/recovery-entry-results.png`

- **Identity**: Scan Results / File Browser / Completed Scan with AI Recommendation — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F8FA` background (main window background) · `#FFFFFF` surface (sidebar and list headers) · `#FFFFFF` card (preview panel background) · `#333333` text-primary (file names in list) · `#666666` text-secondary (dates and file types) · `#2D7FF9` accent ('Recover All' button) · `#10B981` semantic (checkmark icon in status bar) · `#E5E7EB` border (divider lines between regions) · `#2563EB` gradient-stop (AI Recommend toast top) · `#3B82F6` gradient-stop (AI Recommend toast bottom)
- **Gradients**: vertical AI Recommendation Toast: #2563EB → #3B82F6
- **Typography (samples)**: "EaseUS Data Recovery Wizard Free" ~13px/400 #000000 · "Windows (C:)" ~14px/600 #333333 · "actionarchivecache" ~13px/400 #111111 · "AI Recommend available." ~14px/600 #FFFFFF · "Scan Completed, Files Found:" ~15px/600 #111111
- **Heights**: Primary Button (Recover All) 42px · Secondary Button (Open) 36px · List Row 44px · Breadcrumb/Nav Bar 48px
- **Radii**: buttons r6 · toast notification r12 · search bar r4
- **Shadows**: blur16/y8/25% · blur12/y4/10%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke width for line icons; filled style for folder icons.; corners: slightly rounded on filled icons; sharp on UI chrome icons.; notable: folder-blue, check-circle-green, cart-buy-now, ai-sparkle-badge
- **States visible**: selected, loading-indicator
- **Motion hints**: Progress bar in the footer indicates a completed process (static at 100%). AI badge has a subtle glow/shadow suggesting a 'pulse' or 'new' state.
- **Ambiguities**: Exact font family is likely Segoe UI or similar system sans-serif but cannot be confirmed without inspection. | The 'AI Recommend' toast appears to be an overlay that might dismiss or expand.

#### `07-recovery/recovery-exit-retention-gate.png`

- **Identity**: Data Recovery Results / Scan Complete / Modal Overlay (Upsell/Warning) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F4F9` background (Main app background behind modal) · `#FFFFFF` surface (Modal dialog background) · `#1F2329` text-primary (Modal title 'Wait! You are...') · `#5E6268` text-secondary (Modal body text '1139165 files...') · `#337EFF` accent ('Open' button and 'Online Service' text) · `#FFA117` semantic ('Get Up to 40% Off' button background) · `#DDE2EA` border (Modal outer border or input fields) · `#EEF3FA` card (Background of the 3 option rows in modal)
- **Typography (samples)**: "Wait! You are at risk..." ~18px/600 #1F2329 · "1139165 files (146.58 GB)" ~13px/400 #337EFF · "Get Up to 40% Off" ~14px/600 #FFFFFF · "Scan Completed" ~16px/600 #1F2329
- **Heights**: Modal Action Buttons 38px · Recover All Button 44px · Search Input 36px
- **Radii**: Modal container r8 · Buttons r6 · Option row backgrounds r6
- **Shadows**: blur30/y10/20%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke for line icons; flat filled for folder icons.; corners: slightly rounded on standard UI icons, sharp on file-type icons.; notable: hourglass, folder, check-circle, filter, tag-percent
- **States visible**: selected, disabled-dimmed-background
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and image compression. | Some text in the background list is partially obscured by the modal.

#### `07-recovery/recovery-insufficient-space-gate.png`

- **Identity**: Recovery Results / File Selection / Error/Warning Dialog (Insufficient Disk Space) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F0F3F6` background (Main window background) · `#FFFFFF` surface (Dialog background) · `#202124` text-primary (Dialog title 'Insufficient...') · `#5F6368` text-secondary (Dialog body text) · `#1677FF` accent ('Reselect' button background) · `#FFA940` semantic (Warning icon in dialog) · `#D9D9D9` border (Dialog border / input fields) · `#E8E8E8` divider (Horizontal lines between list items)
- **Typography (samples)**: "Insufficient Disk Space" ~18px/600 #202124 · "Need an additional 146.41 GB" ~14px/400 #5F6368 · "Windows (C:)" ~13px/600 #202124
- **Heights**: Primary Button (Reselect) 38px · Secondary Button (Continue) 38px · Search Input 36px · List Item Row 44px
- **Radii**: Dialog container r8 · Buttons r4 · Search/Input r4
- **Shadows**: blur24/y8/15% · blur8/y2/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke for outlines; solid fills for status indicators.; corners: Slightly rounded corners on rectangular icons.; notable: folder-blue, warning-triangle, checkmark-circle, search-magnifier
- **States visible**: selected, disabled
- **Empty/loading/error visuals**: Error visual present: 'Insufficient Disk Space' warning dialog with icon.
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling. | Some text in the background list is obscured by the modal overlay.

#### `07-recovery/recovery-retention-gate-recurrent.png`

- **Identity**: Data Recovery Results / Scan Complete / Modal Overlay (Risk Warning / Upsell) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#F5F7FA` background (Main window background behind modal) · `#FFFFFF` surface (Modal dialog background and list rows) · `#1F2329` text-primary (Modal title 'Wait! You are at risk...') · `#5E6269` text-secondary (Modal body text 'More than 50%...') · `#2B5CED` accent ('Buy Now' button background and 'permanent data loss' link) · `#FFA500` semantic ('Get Up to 40% Off' button background) · `#DCDFE6` border (Modal border and input fields) · `#EBEDF0` divider (Horizontal lines between list items)
- **Typography (samples)**: "Wait! You are at risk of losing data!" ~20px/600 #1F2329 · "Searched: 145044 files, 18.95 GB" ~14px/400 #5E6269 · "Scan Completed, Files Found: 1139234" ~15px/600 #1F2329 · "Get Up to 40% Off" ~15px/600 #FFFFFF
- **Heights**: Primary Button (Recover All) 44px · Secondary Button (Get Up to 40% Off) 38px · List Item Row 48px
- **Radii**: Modal container r8 · Buttons r6 · Input/Search field r4
- **Shadows**: blur30/y12/20% · blur12/y4/10%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke width for line icons; filled style for folder icons.; corners: Rounded corners on all icons.; notable: Hourglass (Warning), Folder, Checkmark circle, Close (X)
- **States visible**: selected, disabled
- **Ambiguities**: Exact pixel dimensions of the underlying window are obscured by the modal overlay. | Some text in the file list is truncated.

#### `07-recovery/recovery-retention-gate-v2.png`

- **Identity**: Data Recovery Results / Scan Complete / Modal Overlay (Upsell/Warning) — theme light, density comfortable, VLM confidence high
- **Palette (VLM sampled)**: `#ECEBF0` background (Main app background behind modal) · `#FFFFFF` surface (Modal dialog background) · `#000000` text-primary (Modal title 'Wait! You are...') · `#4A4A4A` text-secondary (Modal body text '1139246 files...') · `#2B7EF6` accent ('Open' button background, Links) · `#FFA500` semantic ('Get Up to 40% Off' button) · `#D1D1D6` border (Modal shadow edge / subtle borders)
- **Gradients**: vertical Hourglass icon in modal: #89CFF0 → #FFFFFF
- **Typography (samples)**: "Wait! You are at risk" ~18px/600 #000000 · "1139246 files (146.41 GB)" ~13px/400 #2B7EF6 · "Scan Completed, Files Found:" ~15px/600 #333333
- **Heights**: Primary Action Button (Recover All) 40px · Secondary Button (Get Up to 40% Off) 36px · List Item Row 44px
- **Radii**: Modal Dialog r8 · Buttons r6
- **Shadows**: blur24/y8/40%
- **Iconography**: dual-register; stroke: consistent 1.5-2px stroke for line icons; filled for folder icons.; corners: rounded; notable: hourglass, folder, checkmark-circle, filter, search
- **States visible**: selected
- **Ambiguities**: Exact pixel values are estimates based on standard Windows DPI scaling. | Some text is partially obscured by the modal overlay.

#### `07-recovery/recovery-retention-gate.png`

- **Identity**: Scan Results / Recovery Wizard / Modal Overlay (Risk Warning / Upsell) — theme light, density dialog, VLM confidence high
- **Palette (VLM sampled)**: `#E8EBF0` background (App background behind modal) · `#FFFFFF` surface (Modal dialog background) · `#1F1F1F` text-primary (Wait! You are at risk...) · `#5C5C5C` text-secondary (1139261 files found...) · `#3B82F6` accent (Online Service button text) · `#F59E0B` semantic (Get Up to 40% Off button) · `#D1D5DB` border (Modal outer edge/shadow boundary) · `#EFF6FF` card (Price too high? row background) · `#F3F4F6` divider (Subtle separators between action rows)
- **Gradients**: vertical Hourglass icon in modal header: #93C5FD → #3B82F6
- **Typography (samples)**: "Wait! You are at risk" ~18px/600 #111827 · "1139261 files (147.04 GB)" ~13px/400 #3B82F6 · "Price too high?" ~14px/500 #1E40AF · "Get Up to 40% Off" ~14px/600 #FFFFFF
- **Heights**: Primary Action Button (Orange) 38px · Secondary Action Button (Blue outline) 36px · Row Container (Price too high) 52px
- **Radii**: Modal container r8 · Buttons r6 · Action row backgrounds r6
- **Shadows**: blur25/y10/20%
- **Iconography**: filled/dual-register; stroke: Consistent 2px stroke for line icons (close, search), filled for folder/brand icons.; corners: Slight rounding on filled shapes.; notable: hourglass, close-x, folder, percentage-tag
- **States visible**: selected
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and screenshot compression. | Background application state is partially obscured by the modal overlay.

#### `07-recovery/retention-gate-landing-nag.png`

- **Identity**: Data Recovery - Select Location / Modal Overlay (Risk Warning / Upsell) — theme light, density dialog, VLM confidence high
- **Palette (VLM sampled)**: `#E8EBF0` background (Main app background) · `#FFFFFF` surface (Modal dialog background) · `#1F2329` text-primary (Modal title 'Wait!') · `#5E6673` text-secondary (Modal body text) · `#337EFF` accent ('Search for Lost Data' button bg) · `#FFA940` semantic ('Get Up to 40% Off' button bg) · `#DADDE3` border (Modal outer border) · `#E5E6EB` divider (Subtle lines in sidebar) · `#F7F8FA` card (Highlighted row 'Price too high?') · `#B4D8FF` gradient-stop (Hourglass icon bottom gradient)
- **Gradients**: vertical Hourglass icon in modal header: #B4D8FF → #FFFFFF
- **Typography (samples)**: "Wait! You are at risk" ~20px/600 #1F2329 · "More than 50% of permanent" ~14px/400 #5E6673 · "Select a location to search" ~24px/500 #1F2329 · "Hardware Disk" ~15px/500 #1F2329
- **Heights**: Primary Button (Search) 48px · Secondary Button (Get Up to...) 40px · Tertiary Button (Online Service) 36px
- **Radii**: Modal container r8 · Buttons r6
- **Shadows**: blur40/y12/15%
- **Iconography**: dual-register; stroke: Consistent 1.5-2px stroke weight for line icons; filled style for brand/logo icons.; corners: Slightly rounded corners on square icons.; notable: hourglass, hard-drive, sd-card, folder, cloud-download
- **States visible**: selected
- **Ambiguities**: Exact hex values may vary slightly due to anti-aliasing and screen capture compression. | Some sidebar text is truncated/obscured by the modal.

---

## 4. Cross-validation verdicts (VLM vs hard evidence)

| # | Parameter | VLM corpus says | Hard evidence says | Verdict / resolution |
|---|---|---|---|---|
| V-01 | Brand CTA body color | `#1677ff–#3d84ff` band | gradient `#0C99FF→#0560FD` (TDI n10) | RESOLVED→ gradient stops; VLM sampled mid-tones |
| V-02 | Secondary button height | 34–36px | 28px (QSS L604) | RESOLVED→ 28 (VLM over-read) |
| V-03 | Primary CTA height | 40–44px | 32 standard / 36 hero (QSS L1827/L3620) | RESOLVED→ 32/36 |
| V-04 | List row height | 44px ×13 votes | 44px (QSS L17419) | CONFIRMED (exact) |
| V-05 | Radius ladder | r4/r6/r8/r12/r20 | same (QSS census §21) | CONFIRMED 1:1 |
| V-06 | Shadow ladder | blur 8/12/24/30 | blur 12 hover / 20 window (QSS) | CONFIRMED; systematized 5-level |
| V-07 | Canvas/Surface/Text tokens | §2.1 table | themdark.ini slots | CONFIRMED (all 20+ tokens) |
| V-08 | Tree hover/selected | light-blue tint fills | `rgba(5,96,253,0.1)/(0.2)` (QSS L17405/9) | CONFIRMED (exact) |
| V-09 | Tooltip style | blue rounded rect | `#0560FD` bg, r8, 14px, opacity 0.9 (QSS L20–30) | CONFIRMED |
| V-10 | Progress gauge | circular blue ring w/ % center; linear bar bottom | `EuCircleProgress` + QProgressBar family (A19/A08) | CONFIRMED; spec in VISUAL-SPEC §Progress |
| V-11 | Context menu | dark translucent panel, ~43px items | `CShowContextMenu` QSS family; atlas measured 43.5px items | CONFIRMED (43px items — NOTE: our ContextMenu uses 36px [ADAPTATION] for grid discipline) |
| V-12 | Scrollbars | slim, 6–12px | home 12 / results 6 / tree 8 (QSS L1611/22/3418) | CONFIRMED |
| V-13 | Focus rings | none visible | `outline:none` QSS L18 | CONFIRMED absence; fixed by [ADAPTATION] |
| V-14 | Font stack | Segoe-like humanist sans, semibold-dominant | Segoe UI + Semibold 600 (A05 §3) | CONFIRMED |
| V-15 | Dark theme surfaces | `#2b3240→#1a1f29` strip (deep-scan) | `#1B1D1F/#202428/#32383E` (TDI dark 1/2/15) | CONFIRMED (VLM saw the one dark-ish element in light theme) |

---

## 5. Ambiguity register & corpus gaps

1. **Dark theme not captured** (atlas §13: theme switch blocked by retention gate). Dark tokens
   are sourced from `themdark.ini` [dark] + `ResourceDark/` RCC manifest (hard evidence, no VLM).
   Dark-parity verification deferred to the Windows-runner golden-image loop (VISUAL-SPEC
   §Acceptance).
2. **Icon-view grid state partial** (retention gate interrupted capture). Grid metrics from
   `preview=*160.png` IRX standards + QSS icon-view rules.
3. **VLM size estimates skew +4–8px** on small controls at 1024px capture scale — systematic,
   corrected against QSS in every case (see §4).
4. **Per-shot ambiguities** flagged inline in §3 entries (VLM's own honesty flags) — each is
   either resolved by a §4 verdict or carried as `ESTIMATED` in `VISUAL-SPEC.md`.
5. **Motion**: corpus is static PNGs (no GIF/frame sequences in `ui-screenshots/`); motion
   evidence comes from QSS frame-sequence assets (`ScanComplete_00024.png` 24-frame family, A01)
   and dynamic-run notes (gauge animation, "Searching" strip) — motion tokens are [ADAPTATION]
   with corpus anchors (DESIGN-SYSTEM §7).
