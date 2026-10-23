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
