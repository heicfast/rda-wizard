# PARITY-DELTAS — rda-wizard (deliberate divergence registry)

> Seed by the UI build agent. The independent parity auditor verifies every
> row against RE evidence; rows may be promoted to fixes (MISMATCH) or
> accepted (DELTA) by the audit (`docs/PARITY-AUDIT.md` in core_recovery is
> authoritative). Every row cites its justification.

## Format

Each row: surface · og behavior (evidence) · our behavior · delta class ·
rationale. Classes: `SAFETY` · `SCOPE` · `LICENSING` · `MODERNIZATION` ·
`ENGINE-TRUTH` · `STRUCTURAL`.

## Registry

| # | Surface | og behavior (evidence) | Ours | Class | Rationale |
|---|---|---|---|---|---|
| 1 | Wizard stepper | None — QStackedWidget page flip (UI-MAP §4; ARCH05 §3) | 3-phase rail (Select→Scan→Recover) | MODERNIZATION | Non-technical users need position context in a panic moment; documented as improvement (AUDIT-UI ADR-12), never presented as parity |
| 2 | Focus outlines | Suppressed (`outline:none`, QSS L18; DESIGN-SYSTEM §2) | Visible 2px brand focus ring on all interactive controls | MODERNIZATION | Accessibility repair — WCAG 2.4.7; og's suppression was a defect, not a feature |
| 3 | Monetization/retention | 2 GB free quota, periodic retention nag, buy-guide window tracking the CTA (UI-MAP §12, §13) | Absent entirely | SCOPE | OSS product; every recovered byte is real. No upsell-dark-patterns by policy |
| 4 | Cloud/NAS/iCloud/RAID chains | ~15 windows for NAS alone (UI-MAP §2) | Absent | SCOPE | Standing user exclusion; capability copy never claims them |
| 5 | Telemetry / Aliyun / firebase | Present (og-codebase DLLs; UI-MAP §13) | Absent; logs are local-only | SCOPE+SAFETY | No telemetry standing rule |
| 6 | Session format | `.rsf` binary (SaveScan docs) | CRSF `.crsf` (our documented format) | ENGINE-TRUTH | The engine defines the session format; semantic parity (tree+counts+error map), not byte identity; no import until core ships one (G-11) |
| 7 | Pause during scan | Pause/Stop buttons (24px, QSS L4449) | No Pause button; Cancel with destructive confirmation | ENGINE-TRUTH | Engine has no resume API (G-07); a dead pause button violates the no-dead-buttons rule. Button appears when core lands resume (ROADMAP v0.3) |
| 8 | Live results during scan | Tree populates during scan (DYN-scan captures) | Results populate at completion | ENGINE-TRUTH | Tree is unstable mid-stage (G-02/03: data race, header-documented); UI defers with truthful copy |
| 9 | Progress model | Sector-read counters + PNG shimmer frames (DYN-scan §7) | Engine-weighted overall permille + elapsed + ETA-where-computable + live read-error ranges | ENGINE-TRUTH | Only what the CLI observably reports; no fabricated smoothness |
| 10 | Timestamps column | File dates shown | "—" with tooltip | ENGINE-TRUTH | Tree CSV projection carries no timestamps (G-24, tracked); never invented |
| 11 | Collision policy | Unevidenced in corpus (UI-MAP ambiguity #7) | Never silently overwrite; auto-rename `name (2).ext` default with policy picker | SAFETY | Data-destruction risk; og behavior unverifiable — our rule is the safe default, adjudicated in AUDIT-UI §5.7 |
| 12 | Destination overlap | Og warns (destination-picker error state) | Hard refusal pre-engine (WMI disk mapping + write probe + space floor) | SAFETY | Defense in depth over the engine's `[F12]` warning; prompt mandates a hard gate |
| 13 | Driver-absent behavior | Device.mo duality: driver path + direct fallback | Same duality, truthful label ("direct-access mode") + refuse only when both fail | ENGINE-TRUTH | Code-verified in core (`open_disk_view`/`open_disk`/`cr_enumerate`); TRANSPORT-CONTRACT §9 |
| 14 | Visual assets | Proprietary PNG/QSS corpus | Reconstructed token language + Segoe MDL2 glyphs (v1 interim per user grant) | LICENSING | Clean-room; DESIGN-SYSTEM documents the evidence-based reconstruction; homegrown art in v2 |
| 15 | Strings | Proprietary resource strings | Original en/zh-CN tables | LICENSING | Clean-room rule; structure mirrors og's shape (key-per-surface) |
| 16 | Layout-in-stylesheet | 36% of QSS declarations are geometry (A21) | WPF resource dictionaries (tokens) + view XAML geometry | STRUCTURAL | Idiomatic WPF; token system preserves the visual language without the QSS layout pattern |
| 17 | Icon-grid view mode | Present (partial RE evidence) | Detail view ships first; icon view is ROADMAP v0.4 | ENGINE-TRUTH+SCOPE | Corpus ambiguity #5; icon view without the og's icon resources would be a worse-than-og imitation — deferred to homegrown art |
| 18 | Engine linkage | In-process FS DLLs + MOM modules | rrecov CLI subprocess behind Core interfaces | STRUCTURAL | No C ABI exists (G-01); CLI is the sanctioned frozen surface with crash isolation; swap-ready seam for the shim (ADR-1) |
| 19 | Search bar styling | Dated yellow strip `#fff9e6` (QSS L4414) | `Bg.SearchStrip` token (light `#ECF4FF`) | MODERNIZATION | DESIGN-SYSTEM token table; dated pattern consciously not carried |
| 20 | WinPE/BUILDPE surfaces | Present in installed tree | Absent | SCOPE | WinPE media is a packaging-time concern, out of the desktop wizard scope |

## Interim assets (v1 policy, user-granted)

- Segoe MDL2 Assets glyph set (Microsoft, ships with Windows) — placeholder
  iconography pending homegrown set (DESIGN-SYSTEM §6).
- No og icons, strings, or QSS were copied; all colors derive from the
  documented QSS census *values* (hex numbers are facts, not copyrightable
  expression), reconstructed into our token system.
