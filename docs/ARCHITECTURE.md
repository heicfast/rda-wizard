# ARCHITECTURE — RdaWizard

> The wizard's architecture, layering, and wiring. Grounded in
> `AUDIT-UI.md` ADR-1..ADR-12 and the frozen contracts in
> `docs/CORE-CONTRACT.md` + `docs/TRANSPORT-CONTRACT.md`.

## 1. Solution shape

```
RdaWizard.sln
├── src/RdaWizard.Core          net10.0          ViewModels, navigation, wizard state machine, services (interfaces), DTOs, filters — ZERO WPF
├── src/RdaWizard.Integration   net10.0          Engine bindings: rrecov subprocess host, output parsers (frozen grammar), driver probe, destination gate
├── src/RdaWizard.Design        net10.0-windows  Design tokens + theme dictionaries (light/dark) + base control styles
├── src/RdaWizard.Controls      net10.0-windows  Recovery-specific controls (WizardStepper, ConfidenceBadge, HealthGauge, StateSurface)
└── src/RdaWizard.App           net10.0-windows  WPF entry: Generic Host, shell, all views, converters, string tables
```

Dependency direction (strict, one-way):

```
App ──▶ Controls ──▶ Design
 │
 └──▶ Integration ──▶ Core
        (Core has no WPF refs; ViewModels are unit-testable headless)
```

## 2. Layer responsibilities

| Layer | Owns | Never does |
|---|---|---|
| **Core** | Wizard state machine (guarded transitions with reasons), typed navigation (state-gated), all page ViewModels, filter engine, settings, localization, service INTERFACES | Touch WPF types, talk to processes, parse disks |
| **Integration** | `EngineProcessHost` (monitored rrecov subprocess: `\r`/`\n` live line streaming, bounded head/tail/kept retention, process-tree kill), all output parsers (list/scan/tree-CSV/health/fix), `DriverStatusDetector` (`\\.\RDA` CreateFile probe + GLE→state + SCM), `RecoveryService` pre-flight (WMI overlap gate + write-probe + free-space floor) | Parse filesystems, issue IOCTLs, own UI state |
| **Design** | ~110 color tokens ×2 themes (QSS census ground truth), type ramp, spacing, radii, motion durations, base styles for standard controls | Business logic of any kind |
| **Controls** | Recovery-specific controls with `Generic.xaml` default templates | Data access |
| **App** | Generic Host composition, `ShellOrchestrator` (VM events → guarded transitions → navigation), MainWindow shell, views, WPF dialogs, theme service, dispatcher watchdog | Engine logic, state-machine decisions |

## 3. The flow glue — ShellOrchestrator

Page ViewModels raise semantic events (`ScanStarted`, `ScanCompleted`,
`SessionLoaded`, `RecoveryCompleted`, …). The orchestrator translates each
into:

1. A **guarded state-machine transition** (`WizardStateMachine.TryTransition`)
   — every refusal returns a human reason surfaced through a dialog. No
   unguarded navigation exists anywhere in the app.
2. **Navigation** for the new state (`MainViewModel.NavigateForState`).
3. Session bookkeeping: scan runs allocate `scan-diskN-<desc>-<timestamp>.crsf`
   paths and register the source disk (`SessionSourceRegistry`) so the
   recovery overlap gate can resolve "destination on source disk" later.

## 4. Threading model

- All engine work is subprocess I/O inside `EngineProcessHost.RunAsync` —
  never the UI thread; stdout/stderr stream on thread-pool readers.
- `ViewModelBase.InstallUiMarshaler` (installed by App at startup) lets Core
  ViewModels raise `PropertyChanged` from engine threads via
  `Dispatcher.BeginInvoke(DataBind)` — Core never references Dispatcher types.
- `Progress<T>` callbacks marshal through the sync context captured at scan
  start (UI thread in the app, immediate in tests).
- `IAsyncRelayCommand`/explicit commands with `CancellationToken` on every
  long path; cancellation kills the engine process tree (engine cleanup is
  crash-safe by design; sessions are only written on success — gap G-07).
- Debug builds run a **dispatcher watchdog**: any UI-thread stall > 50 ms is
  logged (performance budget, enforced continuously).

## 5. Engine integration (ADR-1)

`core_recovery` exposes a C++20 class surface with no C ABI (gap G-01), so
the wizard drives the **`rrecov` CLI** as a monitored subprocess — the same
surface the parity mandate freezes ("every UI action reproducible by the
CLI"). Benefits: crash isolation (engine fault ≠ UI fault), enforced CLI
parity by construction, `--out file.crsf` as the session transport between
scan → tree → recover invocations.

When the core's C-ABI shim lands (tracked issue #1), the
`IScanService`/`ISessionService`/`IRecoveryService` implementations swap to
in-process bindings **behind the same Core interfaces** — ViewModels and
views do not change. This seam is the deliberate architecture hedge.

## 6. Truthful degradation map

Every gap (G-01..G-23) maps to a visible, honest UI behavior — the table
lives in `AUDIT-UI.md` §3. Highlights:

- **No pause button** (no resume API — G-07); cancel is
  destructive-with-confirmation, stated in copy.
- **Progress is engine-weighted only** (the CLI's overall permille line);
  ETA shows only where the fraction exists; read-error ranges stream live
  from the stage log.
- **Driver-absent = "direct-access mode"** label (core has the fallback,
  code-verified) — scans refuse only when both paths fail.
- **Destination overlap = hard refusal** computed UI-side before the engine
  runs (defense in depth over the engine's `[F12]` warning).
- **Timestamps column shows "—"** (tree CSV carries none — G-24 tracked).

## 7. Session lifecycle

```
scan --out sessions/scan-diskN-….crsf --log …​.stage.log --json
   └─▶ (progress lines + summary JSON parsed live)
tree --in <session> --csv           → LoadedSession snapshot (flat nodes)
   └─▶ client-side filters/tree building (G-12) with virtualization
recover --in <session> --disk N --to DIR
   └─▶ pre-flight gate first (overlap + writability + space), then engine
```

Recent sessions list from the managed `%LOCALAPPDATA%\RdaWizard\sessions`
directory; import jumps straight to Results (ImportSession edge).

## 8. Localization + theming

- JSON string tables (`strings/en.json`, `strings/zh-CN.json`), original
  clean-room copy; `LocalizationService` overlays culture tables with
  English fallback; missing keys degrade to key text with a logged warning.
- Themes swap the LAST merged dictionary in `Application.Resources` — every
  consumer references tokens via `DynamicResource`, so light/dark/system
  switching is live without visual tree rebuilds.

## 9. What is deliberately NOT here

Cloud/NAS/RAID chains, monetization gates, telemetry, retention nags — see
`UI-MAP.md` §13 and `AUDIT-UI.md` §4 (omitted/replaced registry).
