# rda-wizard — RDA Data Recovery Wizard

Production-grade Windows desktop data recovery wizard (WPF, .NET 9) for the
`core_recovery` engine stack and the `rda-driver` kernel transport.

> **Status:** v0.1.0-ui under active development. First production UI of the
> RDA stack. See [worklog.md](worklog.md) for the live session log and
> [AUDIT-UI.md](AUDIT-UI.md) for the UI ground-truth audit against the
> easeus-drw-research corpus.

## What this is

A clean-room, user-first recovery wizard covering the full flow:

**Home / Device Selection → Scan Configuration → Scan Progress → Results
Explorer → Preview → Recovery Queue → Completion**, plus disk health (SMART),
transactional repair (RawFixer), session save/load (`.rrc` / CRSF), and
settings/about with driver + engine status.

- **All recovery intelligence comes from `core_recovery`** — the UI never
  parses filesystems or disk structures itself.
- **All raw disk I/O flows through `rda-driver`** (`OPEN_TARGET`, `GET_INFO`,
  session IOCTLs, `Information÷512` unscale semantics) — the UI never
  reimplements transport and never bypasses the driver's access control.
- **Truthful degradation over impressive dead buttons** — if the core does not
  provide a capability, the UI says so instead of faking it.

## Repository layout

```
rda-wizard/
├── src/
│   ├── RdaWizard.App/              # WPF entry, Generic Host, Views, Resources, Themes
│   ├── RdaWizard.Core/             # Pure .NET: ViewModels, navigation, services, DTOs — no WPF ref
│   ├── RdaWizard.Integration/      # Core engine bindings + rda-driver transport client
│   ├── RdaWizard.Controls/         # Custom controls, behaviors, virtualization
│   └── RdaWizard.Design/           # Design tokens, theme dictionaries, icon assets
├── tests/
│   ├── RdaWizard.Core.Tests/       # ViewModel/service unit tests (no UI thread)
│   ├── RdaWizard.Integration.Tests/# Contract tests against core CLI / driver stub
│   └── RdaWizard.Ui.Tests/         # FlaUI UI automation (Windows runners)
├── docs/                           # UI-MAP, DESIGN-SYSTEM, CORE-CONTRACT, TRANSPORT-CONTRACT, ...
├── .github/workflows/              # ci.yml, ui-smoke.yml, release.yml
├── worklog.md
└── AUDIT-UI.md
```

## Building

WPF builds on Windows only. Use any Windows 10/11 box with the .NET 9 SDK:

```
dotnet build RdaWizard.sln -c Release
dotnet test  tests/RdaWizard.Core.Tests
```

CI runs the full matrix (build, unit, contract, FlaUI smoke, artifact
publish) on `windows-latest` and `windows-2022`. This repository is authored
cross-platform; the Linux side validates the pure-.NET
`RdaWizard.Core`/`Integration` projects where possible.

## Documentation

| Document | Purpose |
|----------|---------|
| [AUDIT-UI.md](AUDIT-UI.md) | Consolidated UI ground-truth audit + gap list |
| [docs/UI-MAP.md](docs/UI-MAP.md) | DRW wizard page/state/flow inventory (from RE) |
| [docs/DESIGN-SYSTEM.md](docs/DESIGN-SYSTEM.md) | Visual language: tokens, components, motion |
| [docs/CORE-CONTRACT.md](docs/CORE-CONTRACT.md) | core_recovery public API surface for the UI |
| [docs/TRANSPORT-CONTRACT.md](docs/TRANSPORT-CONTRACT.md) | rda-driver transport contract for the UI |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Wizard architecture and wiring |
| [docs/TESTING.md](docs/TESTING.md) | Test strategy: unit / contract / UI smoke |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Roadmap beyond v0.1.0-ui |

## License

MIT — see [LICENSE](LICENSE).

## Related

- [`core_recovery`](https://github.com/heicfast/core_recovery) — engine layer
- [`rda-driver`](https://github.com/heicfast/rda-driver) — kernel transport
- [`easeus-drw-research`](https://github.com/heicfast/easeus-drw-research) —
  RE research corpus used as UI ground truth (clean-room reference only; no
  proprietary strings, icons, or assets are copied)
