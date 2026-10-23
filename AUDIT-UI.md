# AUDIT-UI — Consolidated UI Ground-Truth Audit & Implementation Gate

> **Status: GATE PASSED. This document consolidates the four research reports
> (3,374 lines) produced from a line-by-line read of the easeus-drw-research
> corpus, core_recovery headers, and rda-driver transport sources. It records
> the binding architecture decisions, the truthful-degradation gap list, and
> the scope corrections that govern the wizard implementation. No code is
> written before this document is on `main`.**

Targets audited (Context Recovery Protocol step 3):

| Repo | Commit | Notes |
|------|--------|-------|
| `core_recovery` | `183dfcd` | 45 public headers read line-by-line; CLI `rrecov.cpp` (1,191 lines) read in full |
| `rda-driver` | `2b1db5f` | v0.2.0, wire 1.0; `rdacli.c` (443 lines) + `cr_transport.cpp` (807 lines) + shared headers read |
| `easeus-drw-research` | `18d6f14` | ~40 UIUX artifacts, 32 MOM module decodes, 5 SaveScan format docs, architecture docs |

Research deliverables (all under `docs/`):

| Document | Lines | Author | Content |
|----------|-------|--------|---------|
| `UI-MAP.md` | 992 | Agent A | 50 page-inventory rows, per-page anatomy, 30-edge state machine, ~30 error dialogs, 12 free/pro gates, omit/replace decisions |
| `DESIGN-SYSTEM.md` | 888 | Agent B | ~110 tokens (light+dark) anchored on shipped QSS ground truth, 18 component specs, motion, a11y, token dictionary |
| `CORE-CONTRACT.md` | 794 | Agent C | Complete core API surface (headers win over docs), CLI contract frozen, 23 gaps (G-01…G-23), 7 header/doc discrepancies |
| `TRANSPORT-CONTRACT.md` | 700 | Agent D | Full IOCTL table, Information÷512 rule, driver-presence state model, fallback verdict, install/elevation model, CI style |

---

## 1. Binding architecture decisions (ADR-style)

### ADR-1: UI integration mode — CLI-subprocess now, native shim later (G-01)

> **Update (core 2a93c45):** G-01 is CLOSED — the `cr_ui` C-ABI shim landed
> (`cr_ui.dll` / `libcr_ui.so`, `include/cr/cr_ui.h`). The "later" half of
> this ADR is now unblocked; the decision structure is unchanged (swap
> implementations behind the same Core interfaces when the v0.2.0-ui work
> starts). The ADR text below is preserved as the original ruling.

`core_recovery` exposes a **C++20 class surface with no C ABI and no exported
DLL**. The prompt names two sanctioned surfaces: "public APIs **or its CLI
contract**." The CLI (`rrecov`) is complete, CI-tested, and the parity surface
the prompt itself mandates ("every UI action must be reproducible by the
existing `rrecov` CLI").

**Decision:** `RdaWizard.Integration` drives `rrecov.exe` as a monitored
subprocess with structured output parsing (`--json`, `--csv`, stage log),
behind `IEngineHost` interfaces designed so an MSVC-compiled native shim DLL
can replace the process boundary later without touching ViewModels. The UI
**never** links C++ classes directly, never reimplements engine logic, and
parses the exact output formats frozen in `CORE-CONTRACT.md` §13 (including
G-20: `list` exit code is never trusted; stdout is parsed).

**Consequences:** process isolation for free (engine crash ≠ UI crash —
aligned with the crash-isolation mandate); `--out file.crsf` is the session
transport between scan and recover commands; every wizard flow maps 1:1 to a
CLI line (parity is testable by construction).

### ADR-2: Recovery during scan is deferred — core-verified (G-02, G-03)

`cr_node.h` documents the tree as "stable only between stages"; engines emit
from worker threads with no snapshot/read-lock API. Mid-scan recovery is a
data race.

**Decision:** the Results surface populates at scan completion (or
stage-boundary refresh), and Recover is enabled only then. During scan, the
progress page shows live counts from the CLI progress stream only. The UI
copy states this truthfully ("recovery opens when the scan completes —
results are still being discovered"), mirroring the prompt's explicit
fallback requirement. Tracked as core issues (umbrella + G-02/G-03).

### ADR-3: Pause does not exist; Cancel is destructive-with-warning (G-07)

The core stores `stage_done_mask` in sessions but has no resume API; the CLI
saves a session only on scan success.

**Decision:** no Pause button in v1 (a dead button is forbidden). Cancel
shows a confirmation stating results found so far are discarded unless a
session has been saved. Post-scan, "resume" surfaces only as
**load-session** (results browsing + recovery of a completed scan). When the
core lands resume, the button appears — not before.

### ADR-4: Progress is truthful-coarse (G-04, G-05, G-06)

Fine-grained progress exists only for the NTFS walk; other stages tick at
boundaries. `bytes_scanned` is carve-only; the ReadErrorMap merges after the
run.

**Decision:** weighted stage-level progress (quick → deep → carve) with
per-stage sub-states: percentage where the core provides it, "running —
elapsed mm:ss" where it does not. ETA is shown only where computable (carve,
from `bytes_scanned` + throughput). Bad-sector map surfaces post-scan from
the session ERROR_MAP, and read-error ranges stream live from the `--log`
stage log (`read-error range: @… +… (attempts …)` lines are parsed
real-time). No fabricated smoothness, no fake ETA.

### ADR-5: Driver-absent = truthful "direct path", not refusal (code-verified)

The core **has and uses** a direct-access fallback today:
`open_disk_view()` probes `rda_available(L"RDA")` then falls back to
`\\.\PhysicalDriveN`; `open_disk()`/`open_disk_rw()` are RDA-first with
unconditional direct fallback; enumeration is always direct
(`cr_enumerate.cpp:98-112`). This mirrors the og's Device.mo duality.

**Decision:** the UI follows the core exactly. Startup + scan preflight
render the five driver states from `TRANSPORT-CONTRACT.md` §9 (not
installed / installed-not-loaded / blocked / wrong version / loaded-ok)
with truthful transport labeling ("RDA driver path" vs "direct-access
mode") and the honest caveat: direct path is weaker for locked volumes and
damaged media; both paths need elevation for raw access. A scan is refused
only when **both** paths fail. The UI never bypasses SDDL and never issues
IOCTLs itself — transport comes from the core via the CLI/transport layer.

### ADR-6: Destination handling — UI pre-creates folders (G-08)

`DestIo` has no mkdir; nested destinations fail `ERR_DEST_UNWRITABLE`.

**Decision:** the Recovery pipeline pre-creates the destination directory
tree (including reconstructed subfolders) before invoking `recover`.
Empty-folder preservation is **not offered** in v1 (core cannot reconstruct
empty dirs) — the completion report says so truthfully. Tracked as core
issue G-08.

### ADR-7: Destination-overlap refusal is a hard UI gate (defense in depth)

The CLI prints `[F12] WARNING: destination is on the SOURCE disk %u` and
refuses overlapping items, but exits with its own semantics.

**Decision:** the UI computes disk identity via `rrecov list` + volume→disk
resolution (`volume_extent` behavior, G-21) and **refuses up-front** with a
hard gate before queuing: destination on the source disk or source volume
is blocked with an explanation, regardless of CLI behavior. This implements
the prompt's hard-gate requirement on top of the core's warning.

### ADR-8: Preview is a first-class UI subsystem over core extents (G-10)

The core has no preview API (no bounded reader/thumbnails/EXIF/Office text).

**Decision:** previews run in the UI's bounded worker pool over **file
content obtained through the sanctioned recovery path**: selected items are
materialized to a UI-managed temp directory via `recover --filter`
(invocation is logged; content is hashed on write by the safe writer), then
decoded by managed previewers with hard size/time caps
(15 s / 100 MB — matching the og's observed caps). This never touches source
media, never invents core capabilities, and gives every format the core can
recover a preview path. Carved-image thumbnails use the same pipeline. Every
failure surfaces a diagnostic reason. Tracked as core issue G-10 (native
bounded reader is the better long-term path).

### ADR-9: Filters are client-side over the loaded tree (G-12)

No core-side query/filter/paging APIs exist; the CLI offers `--filter`
substring + `--deleted-only`.

**Decision:** the UI loads the CRSF session tree via `rrecov tree --csv`
(byte-grammar frozen in CORE-CONTRACT §13), materializes an in-memory
snapshot (bounded: node counts, not content), and filters/paginates in C#
with full virtualization. The prompt's "push filters into the core" applies
"where supported" — it is not, so this is the truthful implementation;
tracked as core issue G-12. The 1M-node budget is met with UI+data
virtualization (async paged list, recycling VirtualizingStackPanel).

### ADR-10: Session format is CRSF (`.crsf`) — no `.rsf` import (G-11)

**Decision:** save/load via `--out` / `--in` exactly as the CLI defines;
corrupt-session failures surface the CLI's load error verbatim-classified
(never a partial tree). Auto-save mirrors the og behavior (session saved at
scan completion) with our own naming scheme; a human-readable report export
is UI-generated (G-09 tracked).

### ADR-11: Tech stack (per mandate, latest stable pins verified at scaffold)

.NET 9 + WPF; CommunityToolkit.Mvvm 8.4.x; Microsoft.Extensions.Hosting;
hand-rolled typed navigation + wizard state machine (no Prism — justified:
fixed page graph, no module hot-swap); Serilog file logging per session;
FlaUI for UI automation on Windows runners; `windows-latest` +
`windows-2022` CI matrix. Icons v1: Fluent System Icons (MIT) mapped to the
reconstructed taxonomy (DESIGN-SYSTEM §6); homegrown art in v2.

### ADR-12: WizardStepper is a documented improvement

The og has no stepper (QStackedWidget + sidebar); DESIGN-SYSTEM speccs a
3-step rail (Select → Scan → Recover). Confirmed as a deliberate,
user-first adaptation for non-technical users — not presented as parity.

---

## 2. Scope corrections verified against headers (standing rule honored)

- **ReFS is first-class:** `scan_refs` full MSB-chain engine,
  `REFS_FS_KIND=10`, dispatched via `FsFamily::Refs`. The UI lists ReFS among
  supported filesystems with no "report-only" caveat anywhere.
- **LDM/Dynamic Disk is in scope:** `parse_ldm_database`, `LdmVolumeDevice`,
  ~15 structs; simple/spanned/striped/mirrored/RAID-5 (XOR reconstruction of
  one missing column) — surfaced with the honest G-19 scoping (present disks
  only).
- ZFS scoping is stated honestly (G-18: single-vdev pools; BE pools
  refused).

## 3. Gap register → tracked core_recovery issues

All 23 core gaps (G-01…G-23) and 9 transport NEEDS-ISSUE items are filed
against `core_recovery` (umbrella issue + focused issues for the ones the UI
must degrade on). UI truth table:

| Wizard surface | v0.1.0-ui behavior when gap hits |
|---|---|
| Scan during driver absence | Direct-access mode label + caveat; refuse only when direct open fails too |
| Live result tree during scan | Populates at completion/stage boundaries (G-02) |
| Recover button during scan | Disabled with truthful tooltip (G-03) |
| Pause | Not offered; Cancel-with-warning (G-07) |
| Deep-scan progress | Stage bar + elapsed; ETA only in carve (G-04/G-05) |
| Bad-sector map | Post-scan from session; live read-error ranges from stage log (G-06) |
| Nested destination folders | UI pre-creates tree (G-08) |
| Empty folders | Not reconstructed — stated in report (G-08) |
| Recovery report | UI-generated from queue + WriterStats + CLI summary (G-09) |
| Previews | Bounded workers over temp-materialized content (G-10) |
| `.rsf` import | Absent — "our session format" copy (G-11) |
| Core-side filters | Client-side filtering over session snapshot (G-12) |
| Auto-checkpoint mid-scan | Not offered (cancel loses results — warned) (G-13) |
| Verified badge | Carved items only (hash-less for structured files) (G-22) |
| Session reload | Tree restored; source disk re-selected manually (G-14/G-21) |
| SMART over USB bridges | Degrades to NVMe/geometry/TRIM facts with reason (G-16) |

## 4. Omitted / replaced vs. the og (from UI-MAP §13, ratified)

**Omitted (policy/scope):** cloud/NAS/iCloud/RAID chains, retention nag +
buy-guide tracking windows, telemetry/Aliyun surfaces, in-app IE webviews,
license/activation UI, WinPE-only flows, EU repair executables beyond
core-provided repair.

**Replaced:** monetization gates → none (every recovered byte is real);
Apower vestiges → clean shell; QSS-in-stylesheet layout → WPF resource
dictionaries (tokens from DESIGN-SYSTEM); suppressed focus outlines →
visible focus rings (a11y); no-stepper → WizardStepper (ADR-12).

## 5. Corpus ambiguities — adjudicated

1. **Settings contents** (corpus silent): our Settings page covers theme,
   language, log location, default destination, default scan mode (mandate
   list). 2. **Landing ground truth** = sidebar + device cards (Agent A
   recommendation adopted). 3. **Recover dialog geometry**: fresh design per
   DESIGN-SYSTEM (corpus has strings only). 4-6. Preview chrome, icon-grid,
   dark-theme live states: designed fresh within the token system.
   7. **Collision policy** (unevidenced in og): our rule — never silently
   overwrite; auto-rename `name (2).ext` with an explicit policy picker
   (rename / skip / ask), stated in the recovery confirmation.
   8-10. About/updates, repair wizards, NAS/BitLocker flows: designed fresh
   or out of scope as documented.

## 6. Green-light checklist

- [x] Page graph reconstructed from corpus (50 surfaces, 30-edge machine) —
      no guessing.
- [x] Design tokens extracted from shipped QSS ground truth (light+dark).
- [x] Core contract frozen (45 headers; CLI command/flag/exit/output
      surface frozen; 23 gaps classified).
- [x] Transport contract frozen (IOCTLs, ÷512 rule, five driver states,
      fallback verdict code-verified).
- [x] Every gap has a truthful UI degradation + tracked issue.
- [x] ReFS/LDM scope correction verified in headers and carried into
      capability claims.
- [x] Ambiguities adjudicated and recorded.

**Proceed to T3 (solution scaffold) with ADR-1…ADR-12 as binding.**
