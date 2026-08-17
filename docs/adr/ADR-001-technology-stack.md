# ADR-001: Technology Stack

- Status: Accepted
- Date: 2026-08-15
- Decision scope: initial implementation stack for Modern Telengard

## Context

The repository is specification-only. It contains no language, engine, project
manifest, renderer, or test harness to preserve. The authoritative
requirements are the renderer-independent simulation boundary, deterministic
generation and replay, explicit save migrations, data-driven content, a
modern 2D presentation, and a minimal Terminal presentation using the same
simulation.

## Decision

Use **C# on .NET 8** for the game and simulation code, with **Godot 4.x's .NET
edition** as the modern 2D engine and presentation framework. Pin the exact
Godot minor version in the first project setup rather than depending on an
unbounded editor version.

Use:

- **Language:** C# with nullable reference types enabled and ordinary .NET
  value types/records for domain data.
- **Engine/framework:** Godot 4 .NET for the graphical client, scene tree,
  rendering, audio, windowing, and keyboard/controller input.
- **Tests:** `dotnet test` with xUnit for unit and integration tests, plus
  FsCheck/FsCheck.Xunit for property-based tests.
- **Serialization:** explicit versioned save DTOs serialized with
  `System.Text.Json`. Runtime domain objects are never the save schema.
  Migrations are explicit functions selected by `save_version` and preserve
  `simulation_version`, `generator_version`, and `content_version`.
- **Data definitions:** human-readable JSON files for monsters, items,
  spells, features, bands, loot tables, encounter tables, and talents. Load
  and validate them into simulation-facing definitions; keep renderer keys
  and presentation metadata out of authoritative rules where practical.
- **Project organization:** one .NET solution with independent projects:

  ```text
  src/
    Telengard.Core/       authoritative simulation, commands, events, RNG
    Telengard.Content/    JSON loading, validation, content definitions
    Telengard.Save/       save DTOs and migrations
    Telengard.Terminal/   console presentation and input adapter
    Telengard.Godot/      Godot scenes, rendering, audio, and input adapter
  tests/
    Telengard.Core.Tests/
    Telengard.Content.Tests/
    Telengard.Save.Tests/
  content/
    monsters/ items/ spells/ features/ bands/ loot/ encounters/ talents/
  ```

`Telengard.Core` must have no reference to Godot, terminal APIs, sprites,
scenes, UI, fonts, animation, or rendering modes. The Godot project references
the core assemblies and adapts Godot input actions to commands. The Terminal
project references the same core assemblies and maps keyboard input to those
commands. Both clients render state and consume domain events; neither owns or
directly mutates authoritative state.

## Reasons for the decision

### Fit to the requirements

- **Renderer-independent simulation:** a normal .NET class library gives the
  simulation a build and test boundary that does not require an engine
  process, scene tree, or frame callback.
- **Determinism:** C# provides explicit integer/value types and straightforward
  implementation of named RNG streams derived from stable seeds, versions,
  coordinates, entities, and ticks. Determinism remains a core API contract,
  not an engine feature.
- **Testing:** `dotnet test` can execute fast simulation tests headlessly.
  xUnit covers examples and state transitions; FsCheck covers generation,
  coordinate, reachability, RNG, and save/load properties.
- **Saves and migrations:** `System.Text.Json` is in the .NET platform and is
  suitable for explicit DTOs. Version fields and migration functions remain
  visible, reviewable code instead of accidental runtime serialization.
- **Modern 2D:** Godot supplies tilemaps, 2D drawing, lighting, animation,
  audio, scenes, and desktop packaging without requiring simulation code to
  live in scenes.
- **Terminal renderer:** a console application can execute the simulation
  directly, which also makes deterministic smoke tests and debugging tools
  inexpensive.
- **Input:** Godot's InputMap supports named actions bound to keyboard and
  gamepad controls. The mapping ends at command creation; command validation
  remains in the simulation.
- **Data-driven content:** JSON is diffable, hand-editable, easy for agents to
  inspect, and independent of Godot resource ownership. Content loading and
  schema checks can run in ordinary .NET tests.
- **Windows and agent maintainability:** .NET SDK, C#, Visual Studio/VS Code,
  Godot's .NET editor, and command-line test/build workflows are well suited
  to Windows development and text-based code review.

Godot's current documentation confirms that C# projects use the .NET editor,
support desktop platforms including Windows, and require the .NET SDK for
building. It also documents named InputMap actions for keyboard and gamepad
bindings:

- [Godot C#/.NET documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
- [Godot C# prerequisites and project setup](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [Godot InputMap actions](https://docs.godotengine.org/en/stable/getting_started/first_3d_game/02.player_input.html)
- [.NET `System.Text.Json` overview](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)

## Simulation boundary rules

The authoritative flow is:

```text
keyboard/controller/terminal input
        ↓
client input adapter
        ↓
domain command
        ↓
Telengard.Core validates and resolves the command
        ↓
GameState mutation + domain events
        ↓
Godot, Terminal, knowledge, audio, and debug consumers
```

Godot callbacks may poll input and request a command, but they may not resolve
movement, combat, feature outcomes, wealth, knowledge, progression, or saves.
Scenes and renderer objects are presentation state only. Simulation time uses
discrete ticks and never depends on rendered frames.

## Alternatives considered

### Godot with GDScript

Godot/GDScript is a viable rapid-prototyping stack and has the tightest engine
integration. It was rejected as the primary language because the required
headless simulation library, Terminal client, explicit DTO tooling, and
agent-friendly static boundaries are clearer in a standard .NET solution.
GDScript remains unnecessary in this decision; mixing it into authoritative
logic would weaken the boundary.

### Unity with C#

Unity has strong 2D, input, and tooling support. It was rejected because the
project does not need a large proprietary editor/runtime dependency, and a
separate console client plus a pure simulation assembly is less natural in a
Unity-first project. Godot meets the 2D requirement with a smaller, open stack.

### MonoGame or a custom SDL framework

These would preserve a clean C# simulation boundary and provide direct control,
but they require more application infrastructure for scenes, editor workflow,
2D authoring, input configuration, lighting, and packaging. That is avoidable
work for this game.

### Unreal Engine with C++

Unreal is not proportionate to a 2D, grid-based dungeon RPG and would add
engine complexity and a heavier C++ testing/build workflow without improving
the stated requirements.

### Rust with Bevy or a custom renderer

Rust offers strong correctness properties, but the ecosystem and engine
integration would add more moving parts for Windows tooling, 2D authoring,
controller input, and agent maintainability than this repository needs at
Phase 0.

## Disadvantages and tradeoffs

- Godot C# projects require the .NET-enabled editor and a separately installed
  .NET SDK; the standard Godot editor is insufficient.
- Godot C# currently does not target the web platform. Web deployment is not a
  requirement for this project; adding it later would be a deliberate stack
  review.
- Two presentation clients and a shared core add project boundaries and
  adapter code. This is intentional duplication at the edge to protect the
  simulation boundary.
- JSON is less compact and less editor-integrated than binary or engine-native
  assets. It is preferred for content reviewability and agent maintenance;
  binary packaging can be added at build time without changing the content
  contract.
- Explicit DTOs and migrations require ongoing maintenance. That cost is
  accepted because save compatibility and deterministic replay are core
  requirements.
- Godot's scene/editor workflow can tempt gameplay logic into nodes. Code
  review and project references must enforce that `Telengard.Core` remains
  engine-free.

## Consequences

The first implementation phase must establish the solution, a minimal
`Telengard.Core`, deterministic RNG, command/event boundaries, save DTOs, and
headless tests before building substantial Godot scenes. A minimal Terminal
client should be built early enough to prove that the same commands and saves
work without graphical presentation.

This ADR does not select gameplay formulas, a database, a networking model, an
anti-save-scumming policy, or a final content schema beyond the JSON direction.
Those decisions belong to later, scoped ADRs or implementation tasks.
