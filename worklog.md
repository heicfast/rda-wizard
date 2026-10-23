# rda-wizard worklog

Live session log. Append-only, newest at top of each session block.

---

## Session 1 — 2026-10-23 (UI session: wizard from scratch)

### Environment reality check (T1)

- Authoring environment is **Linux** (no .NET SDK preinstalled, no Windows).
  WPF cannot build here. Division of labor per user instruction:
  - **Linux box:** authors all source/docs/CI, validates pure-.NET projects
    (`RdaWizard.Core`, `RdaWizard.Integration`) with a locally installed
    .NET SDK where possible.
  - **Windows runners (GitHub Actions):** build the WPF App, run unit,
    contract, and FlaUI smoke tests, publish artifacts. Workflows pinned to
    `windows-latest` + `windows-2022` per the Win7-inclusive posture.
- Network: up. GitHub token verified (login `heicfast`). Token stored at
  `/home/z/my-project/.git-token` (gitignored, never committed; user rotates
  post-session).

### Target commits (recorded at session start, protocol step 3)

| Repo | Commit | Date (UTC) | Notes |
|------|--------|------------|-------|
| `core_recovery` | `183dfcd` | 2026-10-23 | Win-layer fixes: alignment-descriptor offsets were CACHE-LINE fields — slice creation never worked on direct-path volumes |
| `rda-driver` | `2b1db5f` | 2026-10-22 | bench parity recorded; `Information<<9` legacy contract honored in rdabench (c692ca6) |
| `easeus-drw-research` | `18d6f14` | 2026-10-17 | RE corpus with og-codebase (1.7 GB installed tree) + re-workspace + architecture docs |

`core_recovery` is under active development (same-day pushes) — `git pull`
re-check before each integration-facing milestone.

### Key ground-truth pointers discovered during T1 survey

- `easeus-drw-research/architecture/05-uiux-architecture.md` and
  `07-user-flow-uiux.md` — documented UI ground truth.
- `easeus-drw-research/re-workspace/reversed/` — decompiled tree inventory.
- `easeus-drw-research/og-codebase/` — full installed og: `DRWUI.exe`
  (Qt5-based wizard), FS search DLLs (NTFS/FAT/exFAT/EXT4/HFS+/RAW/ReFS/
  Btrfs/Zfs SearchDLL), `SaveScan/` (session artifacts), `diskhealth/`,
  `Resource/` + `ResourceDark/`, `language/`, `FileFilter.xml`,
  `PartitionRecovery/`, viewers (PDFViewer, EUOfficeViewer, VideoViewer).
- `core_recovery/include/cr/` — public header modules: btrfs, carve,
  compress, crypto, exfat, ext4, fat, hfsplus, lost, ntfs, orchestrator,
  recovery, refs, repair, session, storage, table, tree, win, zfs.
- `core_recovery/docs/crsf-format.md` — our session format (`.rrc`).
- `rda-driver/src/client/` — existing transport client implementation.
- `rda-driver/.github/workflows/{ci,benchmark,release}.yml` — CI style to
  follow.

### Actions

- [x] Repos cloned, exact commits recorded (above).
- [x] `rda-wizard` repo created via API (public, MIT, same org).
- [x] Skeleton pushed: README, LICENSE (MIT), .gitignore (dotnet),
      `worklog/session-ui-prompt.md` (verbatim, token redacted), this worklog.
- [ ] T2: research agents A–D → UI-MAP / DESIGN-SYSTEM / CORE-CONTRACT /
      TRANSPORT-CONTRACT → AUDIT-UI.md → push → only then scaffold.

### Decisions

1. **Repo name `rda-wizard`** (as recommended) — separate repo, same org.
   Rationale: separate CI (Windows-heavy), separate release cadence from the
   C engine core; `rda-driver` precedent (also separate).
2. **MIT license** — consistent with `core_recovery` and `rda-driver`.
3. **Windows-Actions-first build strategy** — mandated by environment; also
   the only way to run FlaUI/UI-automation truthfully.

---

### Session 1 — restart #1 (unexpected stop recovery)

Workspace SURVIVED the stop. Immediate action per user directive (push ASAP):

- [x] Verified all 4 repos intact (core 183dfcd, driver 2b1db5f, research 18d6f14, wizard d79d88b)
- [x] T3 work-in-progress (40 source files: full Core + Integration layers) committed
      as 0050f46 IMMEDIATELY — this is why it was still local-only when the stop hit.
      Lesson applied: never let a coherent unit of work sit uncommitted again.
- [ ] Finish Core build (NavigationService param fix was mid-flight), build
      Integration, then App/Controls/Design + tests + CI.
- [ ] User is pushing raw session-log files to the wizard repo in ~10 min
      (commit on main) — analyze when they appear, reconcile with local state.
- [ ] Context recovery with parallel agents on next stop (mandatory each time).

Build state at commit: Core has remaining CS0103/CS8625/CA1822-style errors
mid-fix; Integration not yet compiled. All remaining fixes are small
(parameter renames, using directives, nullable annotations).

---

## Session 1 — parity-audit pass (independent auditor, 2026-10-23)

### PARITY-T1 — audit bootstrap (no UI code touched)

- Role: independent parity auditor (third agent). Mandate saved verbatim
  (token redacted) at `worklog/session-parity-prompt.md`.
- Pins at audit start: rda-wizard `c2eaad6`, core_recovery `2473c03e`,
  rda-driver `7e88e38` (v0.2.1), easeus-drw-research `18d6f14`.
- Calibration reads: `AUDIT-UI.md` (gate doc, ADR-1..12), `docs/CORE-CONTRACT.md`
  in full (§13 CLI freeze, G-01..G-23, D-1..D-7), worklog.
- Audit axis registered: CORE-CONTRACT.md pins core @ `183dfcd`; core has
  advanced to `2473c03e` (e.g. `PhysicalDiskList` API change) — every §13/§2-12
  claim will be re-verified against current HEAD, drift = finding.
- Next: UI parity sub-agent (page graph/state/wiring vs decompiled DRWUI.exe +
  MOM modules vs UI-MAP.md), then consolidation into core's
  `docs/PARITY-AUDIT.md` + mirrored summary here. Audit before fix.

---

### Session 1 — resume #2: WPF layer landed (a9902de via merge e787bcd)

Coordination note (three-agent mode now active):
- The independent PARITY AUDITOR has bootstrapped (worklog/session-parity-prompt.md,
  pins rda-wizard @ c2eaad6, core @ 2473c03e, driver @ 7e88e38 v0.2.1).
- My contract docs pin core @ 183dfcd and driver @ 2b1db5f — I verified the
  rrecov.cpp CLI grammar is UNCHANGED between 183dfcd..2473c03e (LDM
  cross-disk registry is internal; one new stage-log line only), so the
  Integration parsers remain contract-valid. rda-driver 7e88e38 (v0.2.1)
  needs a TRANSPORT-CONTRACT delta check on next pull.
- A core UI-readiness sub-agent is closing core issues #1-#10 (C ABI shim,
  snapshot, pause/resume, report JSON, preview reader, query API,
  checkpointing). My ADR-1 (CLI-subprocess now, shim later) was designed
  exactly for this: when the shim lands, IResultNodeSource/engine services
  swap implementations behind the SAME Core interfaces.

Landed in a9902de (see commit message for the full inventory):
- Design: Tokens + Theme.Light/Dark (~110 tokens from QSS census) + Controls.Base
- Controls: WizardStepper, ConfidenceBadge, HealthGauge, StateSurface + Generic.xaml
- App: Generic Host + Serilog, MainWindow shell (stepper + region + status
  bar + keyboard nav), ShellOrchestrator (flow glue: VM events -> guarded
  transitions -> navigation; hard preflight gate; session path allocation),
  all 10 page Views with loading/empty/error states, converters,
  en+zh-CN string tables, app.manifest (asInvoker, PerMonitorV2, Win7-10 OSD)
- CI: ci.yml (Linux fast job + windows-latest/2022 matrix + format verify +
  portable publish), ui-smoke.yml (FlaUI), release.yml (tags, zips, SHA256SUMS)
- Local validation: Core 52/52 + Integration 47/47 green (net10.0.402)
- WPF projects compile on Windows CI only (Linux cannot build WPF) — CI
  baseline is the next checkpoint to watch.

Remaining for v0.1.0-ui (tracked): docs/ARCHITECTURE.md + TESTING.md +
ROADMAP.md, T4 static-shell screenshots (Windows runner capture), T9 1M-node
virtualization perf test wiring, preview worker implementation (bounded),
T18 FlaUI full flow on seeded VM, tag v0.1.0-ui.

---

## Session 1 — FINAL HANDOFF (2026-10-23)

### Milestone: v0.1.0-ui tagged, all CI green, release published

- `ci` ✅ windows-latest + windows-2022 (full WPF build, 99 tests, dotnet
  format clean, portable artifact) + ubuntu fast-feedback job
- `ui-smoke` ✅ FlaUI launches the real app on CI: shell title, home
  headline, About navigation all asserted; shell-home.png + shell-about.png
  captured as artifacts (T4); Serilog dir collected for diagnosability
- `release` ✅ v0.1.0-ui published: rda-wizard-portable-win-x64.zip,
  rda-wizard-selfcontained-win-x64.zip, SHA256SUMS

### Commits this session (chronological)

71c7512 skeleton → d79d88b T2 research gate → 0050f46 T3 WIP (stop-recovery
push) → 015392b build clean → c2eaad6 99 tests + 4 real bug fixes →
e787bcd merge w/ parity-auditor bootstrap → a9902de full WPF layer →
86994a8 docs suite → [CI fix series: 0da41c4 MC4005, 217089e MC4111,
c89a41f Automation using, 5f59de6 MC3074+MC3042, 9b0ac74 C# fixes,
2efe1ef bool compare, d93ae99 xmlns refs, e1ac722 format pass, ci GREEN] →
39d38a9 shutdown race → 4b156fb Thickness tokens [ui-smoke GREEN] →
23b5630 screenshots → v0.1.0-ui tag → 8625903 release perms → RELEASE.

### Where the next agent picks up

Pins: rda-wizard @ 8625903 (tag v0.1.0-ui) — core @ 2473c03+ (pull! the
UI-readiness sub-agent is landing the C ABI shim/snapshot/pause-resume/
report/preview/query APIs on issues #1-#10) — driver @ 7e88e38 (v0.2.1;
TRANSPORT-CONTRACT.md still pins v0.2.0 semantics — delta-check on pull) —
parity auditor active in a third session.

Priority queue (docs/ROADMAP.md is authoritative):
1. Preview workers (T11): IPreviewService seam is wired in Core + App;
   implement bounded decode over temp-materialized content (Docnet.Core is
   already referenced for PDF).
2. 1M-node virtualization + filter-latency CI perf job (T9/T19): parser
   already handles 1M rows <10s locally (test exists); add the ListView
   scroll-perf measurement on Windows.
3. FlaUI full wizard flow against a real seeded rrecov build (T18) —
   blocked on engine artifacts being consumed in CI (coordinate with core
   CI to publish rrecov.exe as an artifact the UI workflow downloads).
4. Swap Integration implementations to the core C ABI shim when it lands
   (behind the same Core interfaces — no ViewModel/view changes).
5. TRANSPORT-CONTRACT.md v0.2.1 delta check.

### Standing cautions for the next agent

- WPF XAML cannot compile on Linux: the 10-round CI fix series in this
  worklog shows the failure classes (attached-property validity, Trigger
  target registration, namespace federation, Thickness-vs-Double tokens,
  async-startup shutdown race). Windows CI is the ONLY compile truth.
- Every UI capability claim must trace to CORE-CONTRACT.md §13 + the gap
  register — the parity auditor verifies exactly this.
- Push after EVERY coherent unit — the mid-session stop proved the rule.

### PARITY-T2/T3 — audit complete, gate doc mirrored (independent auditor)

NOTE re the coordination note above: "Integration parsers remain contract-
valid" holds for grammar/flags, but NOT for the list LINE FORMAT — §13
mis-transcribed the table kind as bracketed at BOTH 183dfcd and 2473c03e;
the engine has always printed it unbracketed. See F-C-1 below.

- 8 sub-agent parity reports produced (UI appendix: docs/parity/PARITY-UI.md);
  auditor re-verified all P0/P1 personally against cited code lines.
- **F-C-1 P0 VERIFIED**: `ListOutputParser` disk regexes
  (src/RdaWizard.Integration/Parsers/OutputParsers.cs:14,18-19) require
  bracketed `[GPT|MBR|no-table]`; engine prints table kind unbracketed as the
  final token (rrecov.cpp:237-242, verified at both commits) → zero disks
  parsed, Home broken. Root cause: CORE-CONTRACT §13 transcription error
  inherited by the golden fixture (99 green tests validate the bug).
- **F-C-2/3/4 P1**: ADR-4 live read-error tailer absent; recovery ignores
  user selection; fixed 64 MiB space gate vs og P25. Filed for sequencing.
- State machine 13/30 edges: all implemented edges corpus-backed; F-C-10
  (two-phase quick→advanced continuation promised by UI-MAP REPLACE-10)
  absent + undocumented.
- docs/PARITY-AUDIT.md (mirror) + docs/parity/ appendices committed; core
  holds the authoritative copy. Gate pushed BEFORE fixes, per mandate.
- Next: F-C-1 surgical fix in THIS repo (parser + fixture + tests) paired
  with the CORE-CONTRACT §13 correction in core; issues filed for the rest.

### PARITY-W1 — F-C-1 P0 FIXED: ListOutputParser matches real engine output

- Regex corrected to the actual rrecov grammar (table kind = final
  UNBRACKETED token: GPT|MBR|-|no-table; only [removable|fixed] is
  bracketed). Dead GeneratedRegex DiskLineRegex (declared, never used —
  a second drifting copy of the grammar) removed.
- Golden fixtures reshaped to real engine lines; new guard tests:
  UnknownLayoutDash_Parsed ('-' layout token), BracketedTableKind_
  RejectedAsEngineDrift (the old wrong grammar must never parse again).
- docs/CORE-CONTRACT.md §13 corrected (root cause: transcription error
  that shipped a parser matching zero real disks — Home was permanently
  empty against a real engine).
- Verified locally with .NET 10.0.402: Integration 49/49 (47 + 2 new),
  Core 52/52. WPF App still Windows-CI-only.

### PARITY-W1/W2 — fix waves + registry promotion (independent auditor)

- F-C-1 P0 fixed (bd83be3): parser regex + golden fixtures + guard tests
  + CORE-CONTRACT §13; verified Integration 49/49 + Core 52/52 on
  net10.0.402; Windows CI green (ci + ui-smoke).
- CORE-CONTRACT drift corrections (042e081): §5.1/§5.2 LDM trio
  (unconditional hook + ldm_resolver + cross-disk registry), §5.3
  0..10000 removal, §13 trailing spaces, G-18 gang clause, G-19
  resolver. PARITY-DELTAS.md UI mirror landed (ADR framing corrected:
  preview caps are og FREE-TIER gates, D-28).
- Issues filed: rda-wizard#1 (parser P0 notice + verify-Home-against-
  real-engine action).
- Auditor engine-side fixes in core (details in core worklog
  PARITY-W1/W2): FAT12 sentinels, NTFS viability probe, fixup
  first-2-tails, HFS+ layout, NVMe offsets, carve double-count,
  undo-apply guard, CI unblock. Suite 159/159 green.
---

## Session 2 — Visual Design & Component System agent (2026-10-23)

### VD-T1 — bootstrap + environment unlock

- Role: visual design & component system agent. Mandate saved verbatim (token redacted) at
  `worklog/session-visual-design-prompt.md`.
- Pins at session start: rda-wizard `042e081`, core_recovery `5eaddab`, rda-driver `7e88e38`
  (v0.2.1), easeus-drw-research `18d6f14`.
- **Environment unlock**: .NET SDK 10.0.402 installed at /home/z/.dotnet; **WPF projects now
  COMPILE on Linux via `-p:EnableWindowsTargeting=true`** (full solution builds clean, 18.8s).
  Session-1's "Windows-CI-only compile truth" no longer holds for compile validation — runtime
  /FlaUI/screenshots remain Windows-only. This session compiles every XAML locally before push.
- Read line-by-line: UI-MAP.md (992L), DESIGN-SYSTEM.md (889L), worklog.md, atlas A35, A05
  styling/theming, A21 QSS census, themdark.ini 118 slots (both themes), style.css button
  system (L602-940) + DiskScanButton + tooltip/scrollbar/menu/tree specs + splash/title specs.

### VD-T2..T6 — evidence extraction + gate docs (committed as this unit)

- **VD-A complete**: all 63 corpus screenshots VLM-extracted (fixed template, 0 parse errors,
  resumable NDJSON). Aggregate: VLM re-derives 20+ core tokens within antialiasing noise;
  radius histogram r4/r6/r8/r12/r20 matches QSS 1:1; shadow ladder 8/12/24/30 confirmed;
  systematic VLM drift explained by CTA gradient mid-tones (honor stops, not samples).
- **VD-B complete**: QSS-EVIDENCE.md — full 118-slot dual palette mapped to tokens, verbatim
  button system (7 variants w/ exact gradients + states), typography table with QSS lines,
  geometry register, chrome components, complete gradient inventory.
- **New gate docs**: docs/VISUAL-EVIDENCE.md (1,117L: manifest, aggregate, 63 per-shot
  extractions, 15 cross-validation verdicts V-01..V-15, ambiguity register),
  docs/QSS-EVIDENCE.md, docs/VISUAL-SPEC.md (authoritative parameter spec: token register,
  per-screen specs for our 10 pages + dialogs, 60-component inventory + state matrix,
  acceptance envelope with numeric tolerances, ESTIMATED register).
- Design decisions (recorded in VISUAL-SPEC): existing token key-names preserved (no renames,
  main agent's consumers unaffected); only additive changes; HC theme as third full parity
  dictionary; tokens.json becomes machine SoT with byte-stable CI generation check.

Next (T7+): design/tokens.json + generator → XAML (light/dark/HC) + Metrics.cs; then the
component waves, icons, motion, comps, gallery, a11y/DPI/l10n evidence, acceptance grid,
tag v0.1.0-design.


### PARITY-T20 — handoff (independent auditor)

- PIN: rda-wizard db5a9da (CI green: ci + release + ui-smoke on the
  parity series bd83be3..db5a9da).
- Auditor deliverables in this repo: F-C-1 P0 fix (bd83be3) + guard
  tests; CORE-CONTRACT drift corrections (042e081); PARITY-DELTAS.md UI
  mirror; PARITY-AUDIT.md mirror; issue #1 (P0 notice).
- NEXT UI-SIDE PARITY WORK (evidence in docs/parity/PARITY-UI.md):
  F-C-2 (log tailer for live read-error ranges), F-C-3 (recovery honors
  user selection), F-C-4 (P25 needed-space gate), F-C-5..8 P2 batch,
  F-C-9 (string tables before v1 ships), F-C-10 (UI-MAP REPLACE-10
  amendment or implementation with core pause), D-30 (FileCategory
  claim), F-D-7 (externalize filter data per og FileFilter.xml shape).
- Re-run the UI parity appendix against the App layer once it stabilizes
  (page inventory, error dialogs, feature gates were IN-FLIGHT at audit
  time).
---

## Session 2 (cont.) — token SoT + component library + icon system

### VD-T7/T8 — tokens as code

- `design/tokens.json` — machine source of truth (103 theme keys × light/dark/HC +
  gradients + effects + geometry/typography/motion). HC values: @-prefixed Windows
  SystemColors dynamic keys (OS remaps in real HC themes) + high-visibility literals
  for semantics; gradients fall back to solid in HC; elevation OFF in HC.
- `design/generate-tokens.mjs` — byte-stable generator → Tokens.xaml, Theme.Light/
  Dark/HighContrast.xaml, Metrics.cs. `--check` mode for CI drift detection.
  Parity enforced: all 3 themes have identical key sets (99→103 keys).
- ThemeService: high-contrast support (follows SystemParameters.HighContrast + registry
  probe; HC wins over requested palette unless pinned). App.xaml: 3 new style
  dictionaries + Icons.xaml merged.
- GAP FIX: Text.CaptionStrong was referenced (ScanProgress/Results views) but never
  defined — silent unstyled fallback at runtime since v0.1.0-ui. Fixed + full Text.*
  family completed (BodyStrong/Header/Micro/Link/Numeral tabular).

### VD-T10..T12 — components + icons (compile-verified locally)

- New style dictionaries: Controls.Buttons.xaml (Secondary/Outline/Ghost/Link/Icon/
  SplitAffix — verbatim QSS L602–940 metrics), Controls.Navigation.xaml (ToolTip per
  QSS L20–30, TabControl+TabItem underline strip, ContextMenu+MenuItem 36px, Slider),
  Controls.Surfaces.xaml (Card ×3 variants, Panel, Dialog.Surface, Overlay.Scrim,
  ResizeGrip, Divider, Section.Header).
- New custom controls (RdaWizard.Controls): ToggleSwitch, Banner (4 severities,
  dismissible, action), Toast (auto-dismiss, hover-pause, reduced-motion aware),
  Segmented (+item style), ProgressSurface (determinate-smoothed/indeterminate-shimmer/
  staged), CircularGauge + Arc shape (scan ring, paused state), FileTypeFilterChip
  (icon+count, checked states), DriveCard (capacity bar, OS/USB/SSD badges, TRIM chip,
  BitLocker overlay, selection check), SearchField (placeholder+clear), Breadcrumb,
  RecoveryQueueRow (fit-verdict chips), SessionBanner, Icon (Chrome/Solid/Art kinds,
  optical stroke compensation).
- Icon system: 70+ original geometries in Icons.xaml (nav/window/scan/action chrome on
  24-grid stroke discipline; devices/status/tree/file tiles 20-grid filled; 6 empty-
  state art pieces). All original — no proprietary assets copied.

Local build: full solution green via EnableWindowsTargeting (Linux). Windows CI remains
the runtime/FlaUI truth. Next: gallery app, comps, motion demo, a11y/DPI/l10n evidence,
CI wiring, acceptance grid, tag.

---

## Session 2 (cont.) — gallery, comps, a11y evidence, acceptance grid, design CI

### VD-T13..T17 + T19 highlights

- **Motion (VD-E)**: tokens in tokens.json (durations ×11 + easings), gallery Motion tab
  demos page-transition/panel/press choreography; reduced-motion honored via
  SystemParameters.ClientAreaAnimation in Toast/ProgressSurface/gallery. Spec:
  VISUAL-SPEC §2.5, DESIGN-SYSTEM §7.
- **Gallery (T15)**: src/RdaWizard.Gallery (dev-only, in solution) — overview stats,
  live-enumerated color swatches + icon grids (reads dictionaries themselves — zero
  hand-maintained lists), typography ramp w/ CJK+German stress, geometry (spacing/
  radii/elevation/tints/charts), buttons ×6 variants ×states, inputs, feedback
  (banners/toasts/badges/progress/gauge), data (DriveCard/chips/queue rows/health),
  states (empty/error/skeleton), motion, dialog shells. Live light/dark/HC switcher.
- **Comps (VD-C, T14)**: App/DesignSpecs/ — HomeSpec (sidebar+canvas+session banner+
  tiles+hero CTA per atlas §5), ScanProgressSpec (3-pane live + gauge/counters/pause/
  stop/Recover per atlas §6), ResultsSpec (toolbar/filter-bar/3-pane/contextual CTA per
  atlas §8/A34/A35§9), RecoveryDialogSpec (P23 modal + gates per atlas §10),
  CompactSpecs (P00 splash, scan modes, P08a celebration 540×380, P31 health).
  All static-data, all design-system primitives only, compile-verified.
- **A11y (T16)**: design/contrast-matrix.mjs → docs/ACCESSIBILITY.md (56 pairs ×
  light/dark, WCAG 2.2 relative luminance, alpha composited; CI-fails on regression).
  11 genuine DRW-palette AA failures found and remediated via [ADV] tokens: Text.Link
  light #256CE7, SuccessText #1D8352, DangerText #C01F1F, SecondaryFg #2264D5,
  DangerFill/DangerOn pair, Brand.On dark → inverse. Beyond-color register + focus +
  target-size + reduce-motion + SR labels documented. MATRIX GREEN.
- **CI (T18)**: .github/workflows/design.yml — ubuntu token-drift + theme-key parity +
  icon-manifest jobs; windows-latest/2022 gallery+controls builds w/ analyzers-as-errors.
- **Acceptance (T19)**: docs/VISUAL-ACCEPTANCE.md — 20-param × deliverable grid with
  verdicts + evidence keys; 7 tracked items (A-01..A-07) with rationale (DPI/dark
  golden images + 1M-row perf + first VLM diff round need Windows-runner captures —
  the design.yml steps exist, scheduled v0.2.0).
- Local gates at commit: full solution builds (EnableWindowsTargeting), token check
  byte-stable (106 keys × 3 themes), contrast matrix 0 failures, icons ≥60 integrity.

### Handoff pins (next agents)

- Design system consumers: reference ONLY DynamicResource keys (106 Brush/Gradient/
  Effect + geometry/type/motion in Tokens.xaml) + Metrics.cs constants. Existing keys
  unchanged since v0.1.0-ui — migration is additive. Text.CaptionStrong now EXISTS
  (was silently missing).
- Windows CI must run the new `design` workflow; gallery artifacts land as
  gallery-artifacts-* for the VLM diff loop.
- v0.2.0 queue: A-01..A-07 (golden images, perf job, diff loop round 1, Repair/
  Settings/About comps, 32px icon variants).

---

## Session 3 — OG-exact interface rebuild (page-by-page, crop-VLM methodology)

Methodology pivot per user directive: no more batch-VLM over all 63 shots; build page by page
against the architectural UIUX docs first (R-A digest = 36 artifacts read line-by-line), core
architecture second (R-B digest), then per-page crop-VLM refinement on the specific screenshot.
Anti-slop directive: only what architecture/userflow/UIUX docs + 63 shots prove exists.

### S3-T1..T2 — recovery + Phase-0 cleanup (commits 9a3ac94)

- 4 parallel reading agents (R-A UIUX truth / R-B core wiring / R-C codebase audit /
  R-D research corpus) → digests under /home/z/my-project/reading/ (agent-local, not committed).
- Crop-VLM toolchain built (scripts-local crop_vlm.py: semantic region crops → VLM extract).
- P0 fixed: CompletionView crash (missing BoolToVisibility resource) — view later deleted by
  the shell rebuild anyway.
- S-3 removed: About "RECOVERY ENGINE" version row + EngineInfoService (invented --version
  equivalent; no such flag in the frozen CLI). About now OG P33: product/version/license only.
- S-8 noted: DialogService.ConfirmAsync(confirmLabel) ignored — to be fixed by the OG dialog
  family rebuild (T8).

### S3-T3 — Shell rebuild to OG structure (commit a22c558, pushed after outage recovery)

Structural restructure per R-A §1/§3 (03-window-class-map, 34-navigatebar, atlas 02-landing):
- MainWindow = custom chrome: ~35px title bar (brand lockup, hamburger, min/max/close) +
  6-entry sidebar in 3 sections + stacked page host. Removed: WizardStepper rail (no stepper
  in og), persistent status bar + "Ready" light (S-1/S-6), driver transport caption (S-5),
  HealthGauge Band="3" (S-2), SELECTED recap (S-9), transport truth strip (S-4) — all views
  carrying them deleted with the old page graph.
- Page graph → og QStackedWidget model: Landing (PeripheralHomeView, default), Detail
  (CDetailPage: scan+results unified), SdCard/MoreTools/FileRepair/LostPartition/ChronoSnap
  (sidebar destinations), Health/Repair/Settings/About. Deleted: ScanConfig (og scans directly
  from CTA), ScanProgress+Results (merged into Detail), Recovery+Completion (→ modal dialogs).
- WizardState: Select/Scanning/Results/Recovering; navigation legal to sidebar pages during
  Scanning (og runs scan app-level); frozen while Recovering (modal).
- Tests updated to new semantics: 52 Core + 49 Integration green.

### S3-T4 — landing page precise rebuild (IN PROGRESS at outage; work lost, restarted)

- Tile-structure crop-VLM extraction completed pre-outage (digested into R-A PG-02 table).
- Model expansion for capacity bar (used/total) — MultiEdit lost to the outage; restarted now.
- OUTAGE LESSON (user-directed): push ASAP after every coherent unit; worklog entry BEFORE
  moving to the next unit. Both commits above were stranded unpushed for the whole outage.

Next: T4 landing page exact rebuild (capacity model → tiles → location trio → CTA → states),
then T5/T6 Detail page scan/results states, T7 remaining slop + core wiring cross-check,
T8 OG dialog family, T9 sidebar pages, T10 zero-warning cleanup + final parity loop.

### S3-T4 — landing page og-exact rebuild (commit ff9f1a2)

- VolumeInfo.UsedBytes + TileName; ScanTarget Folder kind (og PushFolderFilter
  semantics: volume scan + client-side path scoping); EnrichWithCapacity pass
  (DriveInfo = og WMI equivalent) in DeviceEnumerationService.
- LandingViewModel: Internal/External tile groups, og single-selection, trio
  (Recycle Bin/Desktop/Select Folder) as folder targets, CTA 'Search for Lost
  Data' / 'Return to scan' while scanning (ReturnToScanRequested wired).
- LandingView: og layout per crop-VLM re-extraction (compact vs selected-
  expanded tiles w/ blue border + check badge + capacity bar; bare trio items;
  divider bottom bar + link + 200x36 CTA); loading/failure/no-devices states;
  P41 banner. 12 new tests; 64+49 green.
- OUTAGE RECOVERY NOTE: the focused crop-VLM extraction was lost with the
  outage (analysis.md had empty headers); re-ran per-crop targeted prompts +
  band verification crops (drives-band/trio-band/content-lower) to resolve the
  tile asymmetry (selected = expanded state) and the trio arrangement (bare
  icon+label, wrapped, no card chrome).

### S3-T5 — Detail page scan-state og rebuild (commit 4fabead)

- Crop-VLM scan-phase extraction (scan-21pct toolbar/tree/bottom-bar/searching-strip +
  scan-paused bottom-bar): og bottom bar = 56-64px ring gauge w/ % inside, Pause+Stop
  (Pause unavailable in our CLI wiring — sanctioned delta), 2-line status text, Recover
  All ... enabled during scan in og (ours gated on session truth per G-02/G-03).
- DetailViewModel: StatusPrimary/Secondary og formats; WatchSessionBoundariesAsync
  (session-file size growth = stage-boundary checkpoint → partial tree install,
  ADR-2 browse-while-scanning); FileRowViewModel rows; FoundBytes session truth.
- DetailView: full og chrome (Searching: header, CNavigateBar toolbar, Path/Type tabs,
  column list, CBottomArea). AppModalDialog (DrwMessageBox 540-family) — S-8 fixed
  (custom labels now render). 70 Core + 49 Integration green.
- Next: T6 results-state refinement (filter bar, preview pane, tree counts + icons,
  context menu, PathPrefix scoping for folder targets), then T7 slop remainder.

### S3-T6 — Detail results-state og rebuild (commit b2cf114)

- 05-results crop-VLM extraction (tree counts, column headers, preview pane,
  bottom-complete, filter bar, context menu, row-selected) — 7 regions.
- Filter bar presets on FilterEvaluator truth; folder-target PathPrefix in the
  ENGINE CSV grammar (volume-relative backslash) with segment-boundary matching
  (fixed a real grammar bug: forward-slash prefix never matched engine paths).
- Tree rollups + og CountText; origin icons; contextual Recover CTA; preview
  pane (folder/file states); og 6-item context menu; tree→list scoping.
- 82 Core + 49 Integration green (12 new DetailResultsTests).
- Known follow-ups: T8 recovery-flow dialogs (destination/space/progress/
  result), T9 sidebar pages detail pass, T10 zero-warning sweep.

### S3-T7/T8 — wiring truth fixes + og recovery funnel (commits 06797c9, 3920901)

- T7 core cross-check found the cancel wiring in the pre-ADR-3 era: exit-3/
  kill semantics now truthful (stopped scans keep found files browsable via
  the G-13 checkpoint session — 'Scan Stopped, Files Found: N (size)';
  before-first-stage stops route home honestly). ScanService keeps the
  cooperative-cancel lines; parser handles 'session saved (cancelled):'.
- S-7 verified already removed (shell rebuild). S-8 fixed in T5 (AppModalDialog
  custom labels).
- T8: ChooseDestinationDialog (og most-styled surface; volume rows + source
  warning + Choose folder + gated Save; Cloud tab omitted — standing
  exclusion) + F-C-4 space gate (needed-bytes soft gate w/ Continue-anyway;
  64MiB hard floor removed from preflight).
- 87 Core + 49 Integration green. Remaining: T9 sidebar pages detail pass,
  T10 zero-warning sweep + final parity loop.

### S3-T9/T10 — sidebar og copy + zero-warning gate (commits ae0b40b, 763da80)

- T9: all five sidebar pages re-extracted via content-area crop-VLM and
  reconciled to og verbatim copy (SD-card subtitle + SDR/SSR/DVR tiles +
  links lead-in; More-Tools descriptions; File Repair label-only cards —
  invented sub-descriptions REMOVED; Lost-Partition subtitle + scenario
  subtexts; ChronoSnap feature descriptions).
- T10: zero-warning build achieved (CA1707 test-naming exemption documented
  per csproj; CA1816 fixed). All gates green at commit 763da80: 0 errors /
  0 warnings; 87 Core + 49 Integration tests; tokens byte-stable (106 keys
  × 3 themes); WCAG contrast matrix 0 failures.

## Session 3 completion summary (og-exact page-by-page rebuild)

Recovered from the outage (2 stranded commits pushed first lesson: push
early, worklog with every unit). Rebuilt per the crop-VLM + architecture
methodology: landing (tiles/trio/capacity), Detail scan state (og bottom
bar + ADR-2 stage-boundary browse-while-scanning), Detail results state
(filter bar/preview/context menu/tree rollups), truthful stop semantics
(exit-3 + checkpoint), og recovery funnel (destination dialog + F-C-4 space
gate), sidebar og copy, zero-warning. Slop register: S-1..S-9 all resolved
(S-8 via AppModalDialog custom labels; driver-fallback About copy remains
the ADR-5-sanctioned delta — policy override only if the owner asks).

Remaining v2 queue (tracked, not slop): Windows-runner golden images +
FlaUI smoke; Up button (needs folder-breadcrumb history); Type tab tree
(classified view); AI Recommend / retention / Cloud tab / free-limit gates
deliberately absent (no AI/licensing/cloud capability — honest scoping).

---

## Coordinated sync — G-01 landed (core 2a93c45)

The core's cr_ui C-ABI shim landed (parallel session-6; independently
verified against the RE corpus: the og binds DRWUI to the *SearchDLL
engines in-process via EUCreate*Searcher factory exports — no subprocess,
no IPC on that leg). Wizard-side truth docs updated in this push:

- CORE-CONTRACT §2.1 rewritten (the "no C ABI today" claims are now
  history): 57-export C11 surface, cr::Err space, zero-init-safe configs,
  two-call pattern, ABI-version policy; §14 G-01 row flipped to CLOSED with
  the v0.2.0-ui swap as the follow-up.
- ARCHITECTURE §5 (ADR-1): the hedge's "when the shim lands" is now; the
  swap remains the v0.2.0-ui work item, CLI-subprocess stays the
  parity-tested default until then.
- ROADMAP v0.2.0-ui: prerequisites marked LANDED, work unblocked.
- AUDIT-UI ADR-1: update note added, original ruling preserved.

No Integration code changed in this push — the seam contract is the point;
the P/Invoke swap is its own tracked work item.
