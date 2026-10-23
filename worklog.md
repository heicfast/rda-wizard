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
