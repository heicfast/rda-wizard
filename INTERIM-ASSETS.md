# INTERIM-ASSETS.md — Interim Asset Register & v2 Replacement Plan

> Policy (user instruction): interim icons are permitted for v1 to unblock the wizard,
> but the design system ships its own icon set, and every interim asset is tracked here
> with a v2 replacement plan. **Nothing in this register is a copied proprietary asset** —
> the DRW/RCC binaries were never extracted for art; the corpus informed only the
> *inventory* (what icons exist) and the *visual language* (stroke discipline, registers).

## Current state (v0.1.0-design)

| Asset class | v1 status | Interim? | v2 plan |
|---|---|---|---|
| Chrome/action icons (nav, window, scan, action, health) | **Original bespoke paths** — `Icons.xaml` 24-grid, 1.5px stroke discipline | No — original v1 art | v2: per-size hinted redraws (16/20/24 tuned vertex sets) + 32px dense-detail variants |
| Device & status tiles (disk, USB, SD, camera, drone, RAID, cloud, locks; check/warn/error) | **Original bespoke paths** — 20-grid filled | No | v2: optical-balance pass against Fluent geometry conventions; state quadruples (nor/hov/pre/dis) as opacity+tint variants |
| Tree-branch markers (Deleted/Directory Intact/Lost/Tags/NameLost/PathLost/Reconstructed/Unimportant/Versions) | **Original bespoke paths** | No | v2: paired 16px row variants with tighter negative space for 28px tree rows |
| File-type tiles (Image/Video/Audio/Document/Archive/Email/Code/Font/Geo/Other) | **Original bespoke paths** + category glyphs | No | v2: per-extension family (IRX PriorityDisplayType ordering) with 2-letter ext labels as Path+TextBlock composites |
| Empty-state art (EmptySearch/NoResults/DriveLost/ScanComplete/Scanning/Error) | **Original geometric art** — deliberately abstract, no mascot | No (design choice: restraint, P1) | v2: evaluate adding one product illustration style if user research supports it |
| Display font | Segoe UI Variable Display 700 fallback (DIN-Black NOT shipped — license + clean-room) | Yes — [ADV] documented in DESIGN-SYSTEM §3.1 | v2: commission/choose an open display face (e.g. Inter Display, Saira Condensed) if hero surfaces need more character |
| Icon glyph states (nor/hov/pre/dis quadruples, QSS `qproperty-*Icon` parity) | **Single geometry + template state re-coloring** (opacity/tint) | Yes — functional but not distinct artwork per state | v2: distinct pressed/hover artwork where optical weight shifts matter (24px chrome set) |
| Dark-theme golden images | Generated (TDI hard evidence) but unverified against rendered output | Yes — verification pending | v0.2.0: Windows-runner gallery captures ×3 themes → VLM diff loop (VISUAL-SPEC §5) |
| Motion demos | Gallery "Motion" tab (token-driven, manual trigger) | Yes — demos, not auto-verified | v0.2.0: FlaUI-driven gallery capture series per choreography |
| DPI evidence | Manifest + layout rules (per-monitor v2, 0.5px snapping documented) | Yes — evidence not yet captured | v0.2.0: gallery screenshots at 100/125/150/175/200% on Windows runners |

## Explicitly NOT carried (standing exclusions — DESIGN-SYSTEM §12)

- Promo/gold gradients (`#FFCD83→#FF9826` family), gift frames, win-back surfaces.
- Fear-marketing retention gates as periodic nags (redesigned as one-shot acknowledge gates).
- Apower vestiges, OEM branding slots, DIN/DINCond faces, `shadow.png` bitmap.
- DRW's raster frame-sequence animations (replaced by shimmer/arc animations).

## Replacement workflow

1. v2 icon work lands in a branch off `main`, extending `Icons.xaml` (keys are additive;
   the Icon control resolves by key so existing consumers are unaffected).
2. Each v2 art change updates this register (row moves out of "interim").
3. Golden-image verification (Windows runner + VLM diff) gates the v2 merge.
