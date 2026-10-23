# TESTING — RdaWizard

Test strategy per the mandate: ViewModel unit tests (no WPF dependency),
navigation/state-machine tests, contract tests against the core CLI grammar
with seeded fixtures, FlaUI UI smoke on Windows runners. UI tests run in CI.

## 1. Test projects

| Project | Target | Runs on | What it proves |
|---|---|---|---|
| `RdaWizard.Core.Tests` | net10.0 | Linux + Windows | State machine guards (incl. hard overlap gate, cancel confirmations, shared-name edges), navigation state gating/back-stack, filter engine semantics (engine-exact deleted states, raw/lost inclusion, unicode paths, G-24 date truth), settings persistence/corruption fallback, scan-mode flag mapping |
| `RdaWizard.Integration.Tests` | net10.0 | Linux + Windows | Golden-fixture parser contracts for the frozen `rrecov` grammar (list/scan progress/summary JSON/session-saved/`[volume]` extents/stage-log read-errors/tree CSV with exact numeric enums/health SMART+NVMe+TRIM/fix plan+REFUSED+execute-OK+undo), CLI argument-builder parity (every wizard scan == one exact rrecov command line), 1M-row tree parse performance bound |
| `RdaWizard.Ui.Tests` | net10.0-windows + FlaUI | Windows runners only | Shell smoke: launch → title/home headline/about navigation assertions; engine absent → truthful error/empty states, never a crash |

## 2. Local fast loop (any OS)

```
dotnet test tests/RdaWizard.Core.Tests/RdaWizard.Core.Tests.csproj
dotnet test tests/RdaWizard.Integration.Tests/RdaWizard.Integration.Tests.csproj
```

99 tests green as of `a9902de` (52 + 47). WPF projects cannot compile on
non-Windows — the Linux CI job builds/tests only the managed projects.

## 3. Windows CI (authoritative)

`.github/workflows/ci.yml`:
- **managed (ubuntu-latest)**: fast feedback on Core + Integration.
- **build (windows-latest + windows-2022 matrix)**: full solution build
  (`TreatWarningsAsErrors` on product code with a documented NoWarn set),
  both test suites, `dotnet format --verify-no-changes`, portable publish
  artifact on windows-latest.

`ui-smoke.yml`: FlaUI shell smoke on windows-latest.
`release.yml`: tag-driven portable + self-contained zips + SHA256SUMS.

## 4. Contract freeze discipline

- Parsers are tested against golden fixtures captured from the frozen
  grammar in `docs/CORE-CONTRACT.md` §13 (verified line-by-line against
  `rrecov.cpp`). When core changes its output grammar, the fixtures AND the
  contract doc must move in the same PR — drift is a parity finding.
- The CLI argument builder tests assert the EXACT flag sequence per
  wizard configuration (parity by construction — ADR-1).

## 5. Bugs the tests caught (kept as regression evidence)

1. `RegexOptions.ExplicitCapture` made unnamed groups non-capturing — every
   progress/read-error parse would have thrown `FormatException` at runtime.
2. `TryTransition` early-returned on the first same-name edge with wrong
   `From` — `BackToSelect` from Results was unreachable.
3. Navigation re-activation bypassed the wizard state gate.
4. `--deep` rode Quick scans (contradictory `--quick-only --deep` pair).

## 6. Planned growth (tracked)

- FlaUI full wizard flow on a seeded VHDX (launch → select → scan → results
  → recover → hash verify) once the engine ships in CI artifacts.
- Dispatcher-watchdog budget assertions in the perf job (startup < 2 s,
  filter < 200 ms @ 1M nodes, preview < 500 ms).
- Fuzz smoke over the tree-CSV parser (structure-aware corpus).
