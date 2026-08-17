# Modern Telengard development

## Repository status

The repository now contains the Phase 0 scaffold selected by
`docs/adr/ADR-001-technology-stack.md`. The headless .NET solution contains
the simulation, content, save, Terminal, and architecture-test projects.
The Godot presentation is represented separately under `src/Telengard.Godot`
and is not required to build or test the simulation.

## Commands

The following commands are the configured headless verification commands for the selected stack:

| Purpose | Command |
|---|---|
| Build | `dotnet build Telengard.sln --configuration Release` |
| Tests | `dotnet test Telengard.sln --configuration Release --no-restore` |
| Formatter/linter | `dotnet format Telengard.sln --verify-no-changes` |
| Deterministic test mode | Unknown — no executable exists yet; the specification requires an equivalent of `game --seed <seed> --deterministic` |

The commands are configured by the scaffold but were not executable in the
current environment because `dotnet` is unavailable. Re-run them after
installing the .NET 8 SDK. The Godot editor/CLI is also not installed here.

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
they contain no gameplay implementation. `Telengard.Godot` remains a separate
presentation module so core tests do not launch graphical presentation.

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
