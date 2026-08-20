# Modern Telengard development

## Repository status

The repository contains the .NET 8 headless solution selected by
`docs/adr/ADR-001-technology-stack.md`, with implemented simulation slices and
ongoing Core Alpha work. The current phase and verified scope are recorded in
[`docs/BUILD_STATUS.md`](BUILD_STATUS.md). The Godot presentation is separate
under `src/Telengard.Godot` and is not required to build or test the simulation.

## Commands

The following commands are the configured headless verification commands for the selected stack:

| Purpose | Command |
|---|---|
| Restore | `./eng/dotnet.ps1 restore Telengard.sln` |
| Build | `./eng/dotnet.ps1 build Telengard.sln --configuration Release` |
| Tests | `./eng/dotnet.ps1 test Telengard.sln --configuration Release --no-restore` |
| Formatter/linter | `./eng/dotnet.ps1 format Telengard.sln --verify-no-changes` |
| Deterministic test mode | No user-facing game executable exists yet. The repository's deterministic test harness accepts `--seed <seed> --deterministic`; the specification requires an equivalent user-facing mode. |

### Provisioning the repository-local SDK

A fresh clone does not include the ignored `.dotnet/` directory. From the
repository root in PowerShell, install the pinned SDK before running
`eng/doctor.ps1`:

```powershell
$installer = Join-Path $env:TEMP 'dotnet-install.ps1'
Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer `
    -Version 8.0.100 `
    -InstallDir (Join-Path (Get-Location) '.dotnet') `
    -NoPath
```

The installer writes only to the repository-local `.dotnet/` directory. The
wrapper then selects that SDK for repository commands; it does not require a
global SDK or a machine `PATH` change.

The repository pins SDK `8.0.100` in `global.json` and provides the supported
PowerShell wrapper at `eng/dotnet.ps1`, which selects `.dotnet/dotnet.exe`.
Run `./eng/doctor.ps1` when SDK or environment behavior is uncertain. Godot is
not required for headless verification.

GitHub Actions uses the Windows `Repository verification` workflow to read the
same `global.json` through `actions/setup-dotnet` and then invokes
`./eng/verify.ps1 -Mode Full`. CI does not need to populate the ignored
`.dotnet/` directory: the wrapper's supported fallback to the SDK provisioned
on `PATH` keeps the server check aligned with the repository's pinned SDK
policy. If `global.json` changes, the workflow follows that file rather than
duplicating the SDK version in YAML.

## Project structure

The current structure is:

```text
Telengard.sln
Directory.Build.props
global.json
src/
  Telengard.Core/       renderer-independent simulation boundaries
  Telengard.Content/    content-definition boundary
  Telengard.Save/       DTO and migration boundary
  Telengard.Terminal/   console presentation boundary
  Telengard.Godot/      Godot presentation placeholder
tests/
  Telengard.Architecture.Tests/
content/
  monsters/ items/ spells/ features/ bands/
  loot_tables/ encounter_tables/ talents/
tools/                  debug/test tooling boundary
docs/
  modern-telengard-spec.md
  ARCHITECTURE.md
  INVARIANTS.md
  DEVELOPMENT.md
  BUILD_STATUS.md
  tasks/
```

The target domain areas are represented as boundaries under `Telengard.Core`;
several are implemented incrementally and the remaining areas are still
scoped by the task ledger. `Telengard.Godot` remains a separate presentation
module so core tests do not launch graphical presentation.

## Domain events

1. Define an event for a committed domain fact, not a UI action.
2. Put the event at the simulation boundary with the smallest stable payload needed by consumers.
3. Emit it only after the authoritative state transition succeeds.
4. Add tests for emission, payload, ordering where relevant, determinism, and save/replay impact.
5. Keep renderers and other consumers subscribed to events rather than calling simulation internals.

## Commands

1. Define a command for an intent such as `MoveNorth`, `Flee`, or `ActivateFeature`.
2. Translate keyboard/controller/UI input into that command.
3. Validate and resolve it inside the simulation.
4. Mutate authoritative state only there, then emit domain events.
5. Add tests for valid, invalid, boundary, deterministic, and relevant knowledge/wealth outcomes.

## Data-defined content

Add definitions for monsters, items, spells, features, bands, loot tables, encounter tables, or talents in the chosen content resource format. Keep identifiers and schema validation explicit. Load definitions into simulation-facing data structures; do not put renderer behavior or duplicated game rules in content files. Add a deterministic fixture test for each new content behavior.

## Save-schema changes

Before changing persisted state, decide whether the change affects profile saves, expedition suspend saves, or both. Update explicit DTOs, increment the appropriate schema/version marker, add a forward migration, preserve generator/simulation/content version fields, and test old-save loading plus save-load replay. Never rely on runtime object serialization as an accidental compatibility contract.

## Scoped TEL work

Use the task template in the specification: design intent, current architecture, requirements, non-goals, invariants, data model, public API, events, determinism, save impact, tests, and acceptance criteria. Avoid unrelated refactors and never silently redesign a public interface while implementing another TEL ticket.
