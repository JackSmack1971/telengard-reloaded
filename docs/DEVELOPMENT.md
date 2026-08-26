# Modern Telengard development

## Repository status

The repository contains the .NET 8 headless solution selected by
`docs/adr/ADR-001-technology-stack.md`, with the renderer-independent Core Alpha
composition, representative floors 1–5 production content, and substantial
Godot client foundations implemented.

The current convergence is the **Five-Floor MVP Demo**:

```text
TEL-129 multi-floor hosted session
  -> TEL-130 encounters/features/treasure in normal play
  -> TEL-131 demo setup + combat closure
  -> TEL-132 real fixed-seed floor-1-through-floor-5 acceptance
```

After TEL-132 passes, the broader client sequence resumes with TEL-126 Godot
save/suspend/resume, TEL-127 Playable Godot Vertical Slice acceptance, and
TEL-128 Art Production Ready.

Current TEL status is authoritative in [`docs/tasks/README.md`](tasks/README.md).
The current product checkpoint is [`docs/MVP_DEMO.md`](MVP_DEMO.md), its active
coordination plan is
[`docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md`](exec-plans/active/FIVE-FLOOR-MVP-DEMO.md),
and its acceptance gate is
[`docs/gates/FIVE-FLOOR-MVP-DEMO.md`](gates/FIVE-FLOOR-MVP-DEMO.md).
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
  Telengard.Godot/      hosted Godot application/input/session/graybox presentation

tests/
  Telengard.Architecture.Tests/
content/
  monsters/ items/ spells/ features/ bands/
  loot_tables/ encounter_tables/ talents/
tools/
  Telengard.GodotHost/  external authoritative Core/content composition for Godot
  Telengard.TestHarness/
docs/
  MVP_DEMO.md
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
remaining scope is controlled by the task ledger. `Telengard.Godot` remains a
separate presentation/application module so Core tests do not launch graphical
presentation. `Telengard.GodotHost` is an application/composition boundary, not
a second gameplay engine.

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

TEL-110–TEL-116 implemented the representative floors 1–5 authored content.
Their IDs are inputs to runtime ecology and presentation/resource mapping, but
Godot resource paths do not belong in authoritative runtime/save state. The MVP
should consume the existing production pack rather than create new first-slice
content merely to avoid integration work.

## Godot client development

### Current MVP work

For TEL-129–TEL-132 read:

- `MVP_DEMO.md`;
- `presentation/GODOT_CLIENT_BLUEPRINT.md`;
- the selected TEL ticket;
- `exec-plans/active/FIVE-FLOOR-MVP-DEMO.md`;
- `gates/FIVE-FLOOR-MVP-DEMO.md`; and
- any UX/architecture/invariant context declared by the selected ticket.

The current implementation priority is integration rather than presentation
breadth. A healthy next-slice selection should be TEL-129 until it is complete,
then TEL-130, TEL-131, and TEL-132.

Do not select TEL-126/TEL-127/TEL-128 or broaden TEL-125 merely because those
tickets are older. Their remaining requirements are post-MVP unless they are a
demonstrated hard prerequisite for the five-floor route.

### Post-MVP Godot work

After TEL-132 passes, resume:

- remaining TEL-121/TEL-122/TEL-125 manual/broad interaction acceptance as
  required by TEL-127;
- TEL-126 persistence/application lifecycle;
- TEL-127 full Playable Godot Vertical Slice gate; and
- TEL-128 Art Production Ready.

For that work read the broader
`exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md` and its relevant gates.

### Host and ownership

The Godot application host owns wiring, not gameplay truth. The expected flow is:

```text
Godot input
  -> host/application request boundary
  -> simulation command
  -> authoritative GameState + committed events
  -> PresentationStateAdapter / Modern projection
  -> Godot scenes, UI, animation, audio
```

Godot and its host may own transient UI/focus/transport/resource-cache state and
may select deterministic layouts/content configuration needed to invoke Core
commands. They must not own authoritative position, movement/stair legality,
encounter outcomes, combat, items, knowledge, wealth, feature outcomes, RNG,
death resolution, or save-domain state.

### Five-floor session composition

For the MVP, do not model the dungeon as one retained floor-1 layout. Movement
must use the authoritative player's current floor layout, and adjacent stair
transitions must use the existing Core floor-transition boundary with the
current and destination layouts. Deterministic layouts are derived from stable
world seed, generator version, and floor inputs.

The MVP client stops at the designated floor-5 demo endpoint even though Core
supports deeper floors.

### Runtime ecology composition

Use the production `ContentPack` to supply existing Core systems with authored
first-slice configuration:

- floor-appropriate encounter tables/configuration;
- deterministic feature runtime state and existing feature resolvers; and
- loot tables feeding the existing treasure acquisition boundary.

Do not implement encounter rolls, feature outcomes, loot rolls, or carried
wealth mutation in Godot to make the demo easier.

### Missing presentation data

Do not solve a visual requirement by reading hidden `GameState` or content internals directly from a scene. Expand the smallest renderer-safe observable projection and preserve redaction tests.

### Input and UI

Keyboard/controller/UI actions submit commands. Presentation-only navigation/focus must not mutate authoritative state. Required MVP actions may not rely on mouse or developer/debug commands.

Keyboard-first normal-input acceptance is sufficient for TEL-132. Complete
controller parity remains a TEL-127 requirement; do not expand the MVP solely to
finish that later gate.

### Simulation time

Simulation speed/outcomes must remain independent of Godot rendering FPS. Normal/slowed/paused behavior uses the renderer-independent time/application boundary rather than frame callbacks as gameplay authority.

### Presentation resources

Stable content/presentation IDs resolve through the presentation-side asset registry described in `presentation/ASSET_PIPELINE_BLUEPRINT.md`. Do not scatter direct ID-to-resource-path conditionals across scenes and do not persist Godot resource paths/UIDs in saves.

### Placeholder first and gate split

Use conspicuous placeholders/graybox visuals through TEL-132 and TEL-127. TEL-132 owns the narrow Five-Floor MVP Demo gate. TEL-127 later owns the broader Playable Godot Vertical Slice gate. Final production assets are systematically ticketed only after TEL-128 separately passes `gates/ART-PRODUCTION-READY.md`.

TEL-128 must not invent unresolved visual-direction, binary/LFS, or other product/repository policy merely to pass readiness. If such a decision is missing, report the explicit blocker.

### Godot observation

A Godot-visible ticket must perform the manual/interactive observation required by its acceptance criteria. Record the Godot/runtime version and fixed seed when useful. The canonical Full verification command is defined in [`AGENTS.md`](../AGENTS.md); it remains mandatory for code changes but does not replace required presentation acceptance.

TEL-132 specifically requires a real Godot fixed-seed route through floor 5; a
headless-only proof cannot pass the MVP gate.

Before declaring Godot unavailable, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/godot-doctor.ps1`. The doctor checks PATH, common installation roots, and WinGet package locations, then reports the executable path and runtime version. If it finds an installed runtime, use that executable for manual acceptance. If no runtime is found, do not weaken the ticket; report the concrete environment blocker.

## Save-schema changes

Before changing persisted state, decide whether the change affects profile saves, expedition suspend saves, or both. Update explicit DTOs, increment the appropriate schema/version marker, add a forward migration, preserve generator/simulation/content version fields, and test old-save loading plus save-load replay. Never rely on runtime object serialization as an accidental compatibility contract.

Godot scene/resource state is not authoritative save data. If client work reveals genuinely missing domain persistence, assign and implement it through the existing save boundary rather than serializing scene objects.

Normal TEL-126 persistence breadth is intentionally post-MVP. Do not introduce
save-schema work into TEL-129–TEL-132 unless the current integration exposes a
genuine authoritative persistence defect.

## Scoped TEL work

Use the task template in the specification: design intent, current architecture, requirements, non-goals, invariants, data model, public API, events, determinism, save impact, tests, observation when relevant, and acceptance criteria. Avoid unrelated refactors and never silently redesign a public interface while implementing another TEL ticket.

For the current convergence, explicit MVP dependencies and product priority
override numeric TEL order. One autonomous implementation run still owns exactly
one coherent TEL slice.
