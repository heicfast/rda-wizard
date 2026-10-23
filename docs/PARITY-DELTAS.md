# PARITY-DELTAS — deliberate-deviations registry (UI-scoped mirror)

> **Authoritative registry:** `heicfast/core_recovery` → `docs/PARITY-DELTAS.md`
> (35 evidence-cited entries in 8 groups; verdict roll-up 27 ACCEPTED / 9 NEEDS-WORK).
> This mirror lists the deltas that live in THIS repo plus the acceptance posture the
> UI must carry. Full evidence for every entry: core's registry + `docs/parity/`.

## Binding UI deltas (ratified ADRs — AUDIT-UI.md)

| ID | Delta | Og behavior (evidence) | Our behavior | Verdict |
|---|---|---|---|---|
| ADR-1 | CLI-subprocess integration now, native shim later | og UI talks to engines in-process via MOM `.mo` modules (DRWUI main-architecture artifact) | `rrecov.exe` subprocess + `.crsf` session transport behind `IEngineHost`; no engine logic above Integration | ACCEPTED (crash isolation; every flow CLI-reproducible; swap-in shim designed) |
| ADR-3 | No Pause in v1; Cancel-with-warning | og offers Pause during scan (Parity-B F-B-7) | Cancel discards unsaved results with explicit warning; resume only via session load | ACCEPTED **until core pause/resume lands** — then the control appears (registered against core issue #4) |
| ADR-4 | Truthful-coarse progress | og shows live file count, sector ticker, remaining time (runtime capture) | Weighted stage permilles + elapsed; ETA only where computable | ACCEPTED; upgrade path = core issue #3 telemetry |
| ADR-7 | Hard destination-overlap gate (defense in depth) | og blocks at UI level ("Source Drive!" + disabled button — ExportThread also has an undecoded engine-side branch, D-17) | UI refuses up-front via disk-identity resolution, on top of the engine's `ERR_DEST_OVERLAP` | ACCEPTED (wording of F12 in core's error-fix-registry corrected: og is not "warning-only") |
| ADR-8 | Preview via bounded temp-materialization | og has a native preview subsystem (assistant-grading-preview artifact); the 15 s / 100 MB caps are og **FREE-TIER gates**, not og engine limits (D-28) | Managed previewers over recovery-path content with hard caps | ACCEPTED — **framing corrected**: caps are OUR safety policy, not "matching the og"; DESIGN-SYSTEM §12.7 wording must not claim og parity for them |
| ADR-9 | Client-side filters over session snapshot | og: engine-side filtering + external FileFilter.xml (8 categories / 1489 extensions) | C# filtering over the loaded tree (ADR-9) | ACCEPTED interim; core issue #8 carries the externalized-data recommendation |
| ADR-12 | WizardStepper 3-step rail | og: QStackedWidget + sidebar, no stepper | 3-step rail (Select → Scan → Recover) | ACCEPTED (user-first adaptation; never presented as parity) |

## Omitted / replaced og surfaces (scope policy — AUDIT-UI.md §4)

Cloud/NAS/iCloud/RAID chains, retention nag + buy-guide tracking, telemetry/Aliyun
surfaces, in-app IE webviews, license/activation UI, WinPE-only flows — **omitted by
policy**; monetization gates replaced with none ("every recovered byte is real");
suppressed focus outlines replaced with visible focus rings (a11y). All ACCEPTED.

## UI-specific open items (NEEDS-WORK, from the parity audit)

- **F-C-9:** localization infra exists with zero string tables — must not ship v1
  without keys (structure parity with og language packs: key conventions, fallback).
- **F-C-10:** UI-MAP REPLACE-10 promises the quick→advanced two-phase scan
  continuation; absent and not registered as a delta — either implement with the core
  pause/resume work (issue #4) or amend UI-MAP.
- **D-30:** FileCategory comment claims "og parity" at 7 categories/~90 extensions
  (og: 8/1489 via FileFilter.xml) — fix the claim or externalize the data.
- **F-D-2/3:** CI landed with the WPF layer but must stay green; `RdaWizard.sln`
  must track the App/Tests projects as they land.

## Asset policy (v1 interim)

Icons: Fluent System Icons (MIT) mapped to the reconstructed taxonomy — interim per
the user's instruction, homegrown art in v2. All strings original. No og assets,
QSS, icons, or strings copied. The audit notes which assets are interim
(DESIGN-SYSTEM §6 mapping).
