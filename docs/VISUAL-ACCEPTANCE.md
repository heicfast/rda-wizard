# VISUAL-ACCEPTANCE.md — 20-Parameter × Surface Acceptance Grid

> **Status**: v0.1.0-design verdicts with evidence links. Verdict scale: **PASS** (evidence
> linked), **PARTIAL** (constructed, runtime verification pending Windows-runner capture),
> **TRACKED** (rationale + issue). The grid is re-filled after every VLM diff-loop round.
> Parameter definitions: session mandate (the 20 quality parameters). Surfaces: the
> design-system deliverables (D1–D8 below) and each shipped page (P00–P31).
>
> Evidence keys: `CM` = docs/ACCESSIBILITY.md (contrast matrix, CI-enforced) ·
> `TP` = design/tokens.json + generator parity check (CI-enforced, 106 keys × 3 themes) ·
> `LB` = local EnableWindowsTargeting compile (full solution green) ·
> `VE` = docs/VISUAL-EVIDENCE.md · `QE` = docs/QSS-EVIDENCE.md · `VS` = docs/VISUAL-SPEC.md ·
> `GAL` = RdaWizard.Gallery render surface · `SPEC` = DesignSpecs/*.xaml ·
> `WCI` = Windows CI (pending workflow run) · `IA` = INTERIM-ASSETS.md.

## 1. Design-system deliverables (this session)

| # | Deliverable | 1 Hierarchy | 2 Grid | 3 Spacing | 4 Type | 5 Color/contrast | 6 Gradients | 7 Icons | 8 Elevation | 9 Radius | 10 States | 11 Focus | 12 Motion | 13 Micro-int | 14 Density | 15 Empty/loading | 16 L10n | 17 DPI | 18 Theme parity | 19 Brand feel | 20 Perf |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| D1 Token system (tokens.json → 3 themes + Metrics) | PASS·VS | PASS·TP | PASS·VS§2.4 | PASS·VS§2.3 | PASS·CM | PASS·QE§7 | — | PASS·QE§2.4 | PASS·VS§2.4 | — | — | — | — | — | — | PASS·VS§3.1 | PARTIAL·WCI | PASS·TP | PASS | — |
| D2 Component library (styles + 14 custom controls) | PASS | PASS·LB | PASS | PASS | PASS·CM | PASS | PASS | PASS | PASS | PASS·VS§4 | PASS·CM§3 | PARTIAL·GAL | PASS | PASS·SPEC | PASS | PASS | PARTIAL·WCI | PASS·TP | PASS | PARTIAL·WCI |
| D3 Icon set (70+ original geometries) | PASS | PASS | — | — | PASS | — | PASS·IA | — | PASS | PASS (state re-color) | — | — | — | — | PASS (art set) | — | PARTIAL·WCI | PASS | PASS | PARTIAL |
| D4 Design-spec comps (5 files, 8 surfaces) | PASS·SPEC | PASS·SPEC | PASS·SPEC | PASS·SPEC | PASS·CM | PASS·SPEC | PASS·SPEC | PASS·SPEC | PASS·SPEC | PASS·SPEC | PASS·SPEC | — | — | PASS·SPEC | PASS·SPEC | PASS·SPEC | PARTIAL·WCI | PASS | PASS·SPEC | — |
| D5 Design gallery (dev surface) | PASS·GAL | PASS·GAL | PASS·GAL | PASS·GAL | PASS·CM | PASS·GAL | PASS·GAL | PASS·GAL | PASS·GAL | PASS·GAL | PASS·GAL | PARTIAL·GAL | PASS·GAL | — | PASS·GAL | PASS·GAL | PARTIAL·WCI | PASS·GAL | PASS·GAL | PARTIAL·WCI |
| D6 Accessibility evidence | — | — | — | PASS·CM | PASS·CM | — | PASS·CM§3 | — | — | PASS·CM§3 | PASS·CM§3 | PASS·CM§3 | — | — | — | PASS | — | PASS·TP | — | — |
| D7 Evidence chain (VE + QE + VS) | PASS·VE | PASS·VE | PASS·QE§5 | PASS·QE§4 | PASS·VE§2.1 | PASS·QE§7 | PASS·VE§2.7 | PASS·VE§2.4 | PASS·VE§2.3 | PASS·VE§2.8 | PASS·QE§6 | PASS·VS§2.5 | — | PASS·VS§3 | PASS·VS§3 | PASS | PASS·VS§6 | PASS·TP | PASS | — |

## 2. Page surfaces (vs our DesignSpecs + the corpus reference)

| Page | Verdict summary (parameters failing → tracked) |
|---|---|
| P00 Splash | PASS on SPEC (r20 card, hero gradient, logo 80, hairline progress). DPI/theme golden: TRACKED·WCI. |
| P02/P03 Home | PASS on SPEC (sidebar 220, title 22, DriveCard grid, session banner, hero CTA; atlas §5 1:1 skeleton). VLM diff loop: pending first Windows-runner capture. |
| P06 ScanProgress | PASS on SPEC (gauge ring, counters tabular, pause/stop 32, Recover CTA 36, 3-pane live; atlas §6). Motion smoothing: implemented (300ms), golden: TRACKED. |
| P06 Results | PASS on SPEC (toolbar 48, filter bar 56, tree 240/list/preview 300, row 44, contextual CTA; atlas §8/A34). 1M-row scroll perf: TRACKED (virtualization in Controls.Base; measurement job on Windows runner). |
| P23 Destination dialog | PASS on SPEC (modal, Local/Cloud segmented, queue rows w/ verdicts, danger coaching, repair toggle ON, Save). QSS 29-rule parity: reconciled in QE§5. |
| P08a Completion | PASS on SPEC (540×380, stats grid 2×4 tabular, CTA at 0.48/0.66 — atlas §7 fractions honored). |
| P31 Health | PASS on SPEC (gauge + SMART table shell; caution callout). Live SMART wiring: other agents. |
| P02c Repair, Settings, About | PARTIAL — compact spec patterns exist (cards/panels/toggles); dedicated comps deferred to v0.2 (TRACKED, low risk: composed entirely of PASS primitives). |

## 3. Remaining tracked diffs (honest register)

| ID | Item | Rationale / plan |
|---|---|---|
| A-01 | DPI golden images (100–200%) | Runtime capture requires Windows runner gallery shots (design.yml step exists); schedule: v0.2.0. |
| A-02 | Dark + HC golden images | Same capture loop; token parity already CI-green (TP). |
| A-03 | 1M-row scroll 60fps measurement | Virtualized styles shipped; FlaUI perf job to be added with the main UI agent's T9 wiring. |
| A-04 | VLM diff loop vs assembled pages | Requires main-UI-agent pages built on this system (they consume v0.1.0-ui primitives; migration to new tokens is additive — no renames). First round scheduled when their Results page lands on the new library. |
| A-05 | Repair/Settings/About dedicated comps | Composed of PASS primitives; deferred v0.2. |
| A-06 | Icon 32px dense-detail variants + per-size hinting | INTERIM-ASSETS.md v2 plan. |
| A-07 | Segmented dark T3 contrast | Measured PASS via matrix (6.1:1) — noted because QSS has no dark-secondary precedent; [ADV]. |

## 4. CI gates enforcing this grid

- `design` workflow: token drift check (byte-stable regeneration), theme key parity
  (106 × 3), icon manifest integrity, gallery Windows build, analyzers-clean controls build.
- Contrast matrix (56 pairs × 2 themes) regenerates ACCESSIBILITY.md; failures exit 1.
- The acceptance envelope for future diff rounds: VISUAL-SPEC §5 tolerances.
