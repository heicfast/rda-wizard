# ROADMAP — RdaWizard

## v0.1.0-ui (this session) — the first production shell

Done:
- Research gate: UI-MAP (992 lines), DESIGN-SYSTEM (888), CORE-CONTRACT
  (794), TRANSPORT-CONTRACT (700), AUDIT-UI (ADRs binding) — `d79d88b`
- Core + Integration layers, 99 tests green locally — `c2eaad6`
- Full WPF layer: design system, controls, shell, all 10 views, CI — `a9902de`
- Docs: ARCHITECTURE, TESTING, this roadmap, PARITY-DELTAS seed

Closing (tracked, in order):
- CI baseline green on windows-latest + windows-2022 (first full WPF compile)
- T4: static-shell screenshots from the CI VM (artifact)
- T11: preview worker implementation (bounded decode over materialized
  content — the `IPreviewService` seam is wired; workers land next)
- T9/T19: 1M-node virtualization + filter latency perf tests in CI
- T18: FlaUI full wizard flow against a seeded engine build
- Tag `v0.1.0-ui` + release artifacts (portable + self-contained zips)

## v0.2.0-ui — native engine binding

Core prerequisites LANDED: the C ABI shim (issue #1 — `cr_ui.dll` /
`libcr_ui.so`, G-01 closed at core 2a93c45) and the snapshot APIs
(issues #2/#3 — closed with it). Unblocked:
- Swap `IScanService`/`ISessionService`/`IRecoveryService` implementations
  to in-process bindings behind the same interfaces (ADR-1 hedge pays off).
- Live results pane at stage boundaries; select-and-recover during scan
  where the snapshot contract allows.
- Progress: per-stage fractions + live error map (core issues #3).
- Preview: native bounded reader (core issue #7) replacing
  temp-materialization; thumbnails for carved images from core extractors.

## v0.3.0-ui — resilience + scale

- Scan resume from checkpointed sessions (core issues #4/#9) → the Pause
  button appears with truthful semantics.
- Recovery report JSON from core (issue #6) → richer completion page.
- Core-side query/filter push-down (issue #8) → filters leave the client.
- Auto-checkpointing (core issue #9) → crash-safe long scans.
- Destination mkdir + empty-folder preservation in the writer (issue #5) →
  the truthful notes in the report upgrade to capabilities.

## v0.4.0-ui — fit and finish

- Homegrown icon set (replaces v1 interim glyphs) — DESIGN-SYSTEM §6 mapping.
- Large-icon view mode parity surface polish.
- High-contrast theme mapping audit + full keyboard-nav pass with
  screen-reader review.
- NSIS installer (signed MSI later); engine bundled via CI artifact.
- zh-CN copy review pass by a native speaker (structure ships now).

## Standing truths (do not regress)

- No capability claim without engine backing (truth table: AUDIT-UI §3).
- No dead buttons: unavailable surfaces explain themselves.
- No telemetry. Logs only, local, reconstructable.
- Win7→Win11 posture: WPF on .NET LTS, per-monitor v2 with v1 fallback,
  elevation only at the moment needed (asInvoker manifest).
