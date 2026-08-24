# Modern Telengard development

## Repository status

The repository contains the .NET 8 headless solution selected by
`docs/adr/ADR-001-technology-stack.md`, with the renderer-independent Core Alpha
composition and presentation-separation proof implemented. The current
convergence is a **Playable Godot Vertical Slice** followed by the separate
**Art Production Ready** handoff: TEL-110–TEL-116 author the representative
floors 1-5 content, TEL-120–TEL-127 build/prove the production-shaped Godot
client using placeholder/graybox presentation, and TEL-128 separately decides
when final production asset batches may begin.

Current TEL status is authoritative in [`docs/tasks/README.md`](tasks/README.md).
Verification history is recorded in [`docs/BUILD_STATUS.md`](BUILD_STATUS.md).
The durable presentation methodology is
[`docs/presentation/GODOT_CLIENT_BLUEPRINT.md`](presentation/GODOT_CLIENT_BLUEPRINT.md).

The Godot presentation remains separate under `src/Telengard.Godot` and is not
required to build or test renderer-independent simulation code. Godot-visible
tickets may additionally require manual Godot acceptance beyond the headless
repository gate.

## Commands

The following commands are the configured headless verification commands for the selected stack:

| Purpose | Command |
|---|---|
| Restore | `./eng/dotnet.ps1 restore Telengard.sln` |
| Build | `./eng/dotnet.ps1 build Telengard.sln --configuration Release` |
| Tests | `./eng/dotnet.ps1 test Telengard.sln --configuration Release --no-restore` |
| Formatter/linter | `./eng/dotnet.ps1 format Telengard.sln --verify-no-changes` |
| Deterministic test mode | The headless test harness accepts `--seed <seed> --deterministic --script <path>` and emits stable JSON Lines while comparing replayed final saves/events. |

### Coverage and mutation scope

`./eng/coverage.ps1` reports role-tagged rows for the four production projects
(`Telengard.Core`, `Telengard.Content`, `Telengard.Save`, and
`Telengard.Terminal`) and for `Telengard.TestHarness` as test support. Only
the production aggregate is gated; test-support totals remain visible in the
generated reports.

`./eng/mutation.ps1` preserves the default all-production-project baseline
under `TestResults/mutation-baseline`. Pass scoped Stryker options through
`-AdditionalStrykerArgs` with a distinct `-ResultsDirectoryName`; `--since`
and `--with-baseline` are rejected when the default baseline directory is
selected. Use an explicit branch, tag, or commit as the Stryker diff target.

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

GitHub Actions uses the Windows `Repository verification` workflow to provision
SDK `8.0.100` through `actions/setup-dotnet` and then invokes
`./eng/verify.ps1 -Mode Full`. The workflow uses the explicit version because
`actions/setup-dotnet`'s `global-json-file` mode can leave `dotnet format`
unable to locate its SDK build host on hosted Windows runners. Keep this
workflow value synchronized with `global.json` when the repository SDK pin
changes. CI does not need to populate the ignored `.dotnet/` directory: the
wrapper's supported fallback to the SDK provisioned on `PATH` keeps the server
check aligned with the repository's pinned SDK policy.

## Project structure

The current structure is:

```text
Telengard.sln
Directory.Build.props
global.json
src/
  Telengard.Core/       renderer-independent simulation + presentation projections
  Telengard.Content/    content-definition/loading boundary
  Telengard.Save/       DTO and migration boundary
  Telengard.Terminal/   console presentation boundary
  Telengard.Godot/      Godot presentation boundary/prototype; TEL-120+ builds client

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
  presentation/
  gates/
  tasks/
  exec-plans/
```

The target domain areas are represented as boundaries under `Telengard.Core`;
several are implemented incrementally and remaining scope is controlled by the
task ledger. `Telengard.Godot` remains a separate presentation/application
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

Add definitions for monsters, items, spells, features, bands, loot tables, encounter tables, or talents through the external content-pack boundary. Keep identifiers and schema validation explicit. Load definitions into simulation-facing data structures; do not put renderer behavior or duplicated game rules in content files. Add deterministic fixture/loader tests proportional to each new content behavior.

TEL-110–TEL-116 own the representative floors 1-5 authored content. Their IDs become inputs to presentation/resource mapping but Godot resource paths do not belong in authoritative runtime/save state.

## Godot client development

For TEL-120–TEL-128 read:

- `presentation/GODOT_CLIENT_BLUEPRINT.md`;
- the selected TEL ticket;
- any UX/art/asset blueprint referenced by that ticket;
- `exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md`;
- the relevant presentation gate when acceptance claims gate progress.

### Host and ownership

The Godot application host owns wiring, not gameplay truth. The expected flow is:

```text
Godot input
  -> simulation command/application boundary
  -> authoritative GameState + committed events
  -> PresentationStateAdapter / Modern projection
  -> Godot scenes, UI, animation, audio
```

Godot may own transient camera, focus, animation, audio, and resource-cache state. It must not own authoritative position, combat, items, knowledge, wealth, feature outcomes, RNG, death resolution, or save-domain state.

### Missing presentation data

Do not solve a visual requirement by reading hidden `GameState` or content internals directly from a scene. Expand the smallest renderer-safe observable projection and preserve redaction tests.

### Input and UI

Keyboard/controller/UI actions submit commands. Presentation-only navigation/focus must not mutate authoritative state. Required first-slice actions may not rely solely on mouse or developer/debug commands.

### Simulation time

Simulation speed/outcomes must remain independent of Godot rendering FPS. Normal/slowed/paused behavior uses the renderer-independent time/application boundary rather than frame callbacks as gameplay authority.

### Presentation resources

Stable content/presentation IDs resolve through the presentation-side asset registry described in `presentation/ASSET_PIPELINE_BLUEPRINT.md`. Do not scatter direct ID-to-resource-path conditionals across scenes and do not persist Godot resource paths/UIDs in saves.

### Placeholder first and gate split

Use conspicuous placeholders/graybox visuals through TEL-127 until the full client path is proven. TEL-127 owns the Playable Godot Vertical Slice gate. Final production assets are systematically ticketed only after TEL-128 separately passes `gates/ART-PRODUCTION-READY.md`.

TEL-128 must not invent unresolved visual-direction, binary/LFS, or other product/repository policy merely to pass readiness. If such a decision is missing, report the explicit blocker.

### Godot observation

A Godot-visible ticket must perform the manual/interactive observation required by its acceptance criteria. Record the Godot/runtime version and fixed seed when useful. `./eng/verify.ps1 -Mode Full` remains mandatory for code changes but does not replace required presentation acceptance.

Before declaring Godot unavailable, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/godot-doctor.ps1`. The doctor checks PATH, common installation roots, and WinGet package locations, then reports the executable path and runtime version. If it finds an installed runtime, use that executable for manual acceptance. If no runtime is found, do not weaken the ticket; choose another eligible slice when possible or report the concrete environment blocker.

## Save-schema changes

Before changing persisted state, decide whether the change affects profile saves, expedition suspend saves, or both. Update explicit DTOs, increment the appropriate schema/version marker, add a forward migration, preserve generator/simulation/content version fields, and test old-save loading plus save-load replay. Never rely on runtime object serialization as an accidental compatibility contract.

Godot scene/resource state is not authoritative save data. If client work reveals genuinely missing domain persistence, assign and implement it through the existing save boundary rather than serializing scene objects.

## Scoped TEL work

Use the task template in the specification: design intent, current architecture, requirements, non-goals, invariants, data model, public API, events, determinism, save impact, tests, observation when relevant, and acceptance criteria. Avoid unrelated refactors and never silently redesign a public interface while implementing another TEL ticket.

For the current convergence, numerical TEL order does not override explicit dependencies across the TEL-110–TEL-116 content track and TEL-120–TEL-128 Godot/readiness track.
