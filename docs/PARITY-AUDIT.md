# PARITY-AUDIT.md — UI-side mirror of the independent parity audit

> **Authoritative document:** `heicfast/core_recovery` → `docs/PARITY-AUDIT.md`
> (same commit pair). This mirror covers the findings that land in THIS repo plus the
> contract corrections the UI consumes. Full evidence: `docs/parity/PARITY-UI.md`,
> `docs/parity/PARITY-ARCH.md`, `docs/parity/PARITY-DELTAS-draft.md` (UI-relevant
> appendices; engine appendices live in core).

Pins: rda-wizard `c2eaad6`+`c083bdd` · core_recovery `2473c03e`+`d42bb00` · corpus
`18d6f14`. Auditor: independent third-party parity agent (see core worklog
`PARITY-T1`/`PARITY-T2`). Method: read-only sub-agent audits + auditor re-verification
of every P0/P1 against cited code lines.

## UI verdict summary

| Surface | Verdict | Notes |
|---|---|---|
| Wizard state machine | **MATCH** | All 13 implemented edges trace to corpus-backed og edges; every divergence ADR-documented except F-C-10 (two-phase quick→advanced continuation promised by UI-MAP REPLACE-10, absent) |
| Scan flag grammar (ScanArgumentBuilder) | **MATCH** | Verified against actual `rrecov.cpp` at both the contract pin and HEAD |
| Health/fix output parsers | **MATCH** | Exit codes, G-20 list-never-trust, text formats |
| TreeCsvParser | **MATCH** (caveats) | Escaped-path edge cases noted (F-C-16) |
| `list` output parser | **MISMATCH — P0** | F-C-1 below |
| Live progress truthfulness (ADR-4) | **MISMATCH — P1** | F-C-2 below |
| Recovery funnel semantics | **MISMATCH — P1** | F-C-3/F-C-4 below |
| Layering (VM/Integration/App) | **MATCH** | No CLI strings above Integration; no engine logic in VMs |
| Threading/cancellation | **MATCH** | ADR-3 cancel semantics verified |
| Localization | **MISSING (adoption)** | Infra exists, zero string tables/keys (F-C-9) |
| CI posture | **MISSING** | No CI in repo; README claims a matrix (F-D-2); sln references 4 nonexistent projects (F-D-3) |

## Auditor-verified findings affecting this repo

- **F-C-1 (P0)** — `ListOutputParser` disk-line regexes
  (`src/RdaWizard.Integration/Parsers/OutputParsers.cs:14,18-19`) require the table kind
  bracketed (`[GPT|MBR|no-table]`). The engine prints it as the final **unbracketed**
  token; `[removable|fixed]` is the bracketed one (`core_recovery/src/app/rrecov.cpp:237-242`,
  verified at both commits). Real `rrecov list` parses **zero disks** → Home permanently
  empty. Root cause: `docs/CORE-CONTRACT.md` §13 transcription error, inherited by the
  golden fixture — the 99 green tests validate the bug. **Fix wave 1** (parser + fixture +
  tests + §13 correction, coordinated with core).
- **F-C-2 (P1)** — ADR-4's "live read-error ranges from the stage log" is unimplemented:
  the engine writes them to the `--log` **file**; `ScanService` parses **stdout** only;
  no log tailer exists anywhere. Fix: tail `--log` (issue filed; design in PARITY-UI.md).
- **F-C-3 (P1)** — Recovery ignores the user's selection: it recovers the full session;
  the request model cannot express subsets (og P23/P24 parity).
- **F-C-4 (P1)** — Space gate is a fixed 64 MiB hard refusal vs og P25's needed-space
  computation + continue-anyway.
- **F-C-5..8 (P2)** — overlap preflight silently passes when WMI fails; recovery progress
  frozen (engine percent lines unparsed); collision/folder-structure settings are dead
  controls; imported sessions recover with `--disk -1`.
- **F-C-9 (P2)** — localization adoption gap (must not ship v1 without string tables).
- **F-C-10 (P2)** — UI-MAP REPLACE-10 promises quick→advanced two-phase continuation
  ("keep exactly"); absent and not registered as a delta.
- **F-D-2/3 (P2)** — no CI despite README claims; `RdaWizard.sln` references 4
  nonexistent projects (App/Tests projects in flight — sln must be completed with them).

## Contract corrections consumed by this repo (coordinated pair with core)

`docs/CORE-CONTRACT.md` §13 (list line), §5.1/§5.2 (ScanConfig.ldm_resolver, LDM hook),
§12/G-19 (cross-disk LDM registry), §5.3 (0..10000 claim), progress-line trailing
spaces. The wizard's parsers must be re-verified against the corrected §13 after the
fix wave (fixture + tests updated together).

## Fix plan for this repo (auditor wave 1 = F-C-1 only; rest filed as issues for the
active UI agent to sequence)

1. F-C-1 surgical fix (parser regexes, golden fixture, regression tests shaped like real
   engine output) + CORE-CONTRACT §13 correction — coordinated push-pair with core.
2. Issues filed: F-C-2, F-C-3, F-C-4, F-C-5..8, F-C-9, F-C-10, F-D-2, F-D-3.
3. After the UI agent lands the App layer: re-run the UI parity appendix against it
   (page inventory, error dialogs, feature-gate inventory are currently IN-FLIGHT).
