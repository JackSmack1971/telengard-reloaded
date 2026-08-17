# Telengard Reloaded

**A modern, deterministic reimagining of the classic dungeon-crawling experience.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Godot 4](https://img.shields.io/badge/Godot-4.x-478CBF)

Telengard Reloaded is a modern take on the dungeon RPG that first captured my imagination years ago.

The goal is not simply to recreate an old game with new graphics. It is to preserve the things that made Telengard compelling — **danger, mystery, speed, procedural exploration, strange discoveries, and the constant temptation to go just one floor deeper** — while modernizing the controls, readability, content depth, and long-term progression.

> **Does this make the decision to go one floor deeper more interesting?**

That question guides the design of the project.

---

## Project Status

> [!IMPORTANT]
> **Telengard Reloaded is currently in pre-alpha development.**
>
> The renderer-independent game simulation is actively being built and tested, but there is not yet a complete playable Terminal or Godot client.

A significant portion of the underlying simulation already exists, including:

* deterministic dungeon generation and traversal;
* expedition state and carried vs. secured progress;
* encounters and combat;
* threat assessment;
* dungeon features such as fountains, altars, pits, and teleporters;
* items, equipment, affixes, curses, and identification;
* spells and progression primitives;
* player knowledge and persistent discoveries;
* deterministic random-number streams;
* explicit versioned save data and migrations;
* headless simulation and acceptance testing.

The current development goal is to turn these systems into a complete **Core Alpha vertical slice** that can be played from character creation through a full dungeon expedition and return to safety.

Detailed implementation status is maintained in [`docs/BUILD_STATUS.md`](docs/BUILD_STATUS.md).

---

## The Vision

Telengard Reloaded is built around a simple expedition loop:

```text
INN
 ↓
Prepare
 ↓
Enter the dungeon
 ↓
Explore
 ↓
Fight • Flee • Discover • Interact
 ↓
Find treasure
 ↓
Choose:
    DESCEND → greater danger, greater reward
    RETREAT → try to make it home alive
 ↓
Escape
 ↓
Secure progress
 ↓
Prepare for the next descent
```

The intended player experience is a cycle of:

```text
curiosity
→ uncertainty
→ discovery
→ confidence
→ greed
→ danger
→ retreat or deeper descent
→ relief or loss
```

### Design Pillars

The project is being built around five core ideas:

1. **The dungeon should feel vast.**
2. **Going deeper should always be tempting.**
3. **Returning alive should matter.**
4. **Strange dungeon features should be central to exploration.**
5. **Player knowledge should itself be a form of progression.**

The project intentionally avoids turning into a generic fantasy roguelite filled with crafting economies, inventory management minigames, enormous skill trees, MMO-style loot clutter, or linear story progression.

---

## Architecture

Telengard Reloaded separates the authoritative game simulation from its presentation.

```text
                   ┌───────────────────────┐
                   │   Telengard.Core      │
                   │                       │
                   │  Authoritative        │
                   │  Game Simulation      │
                   └───────────┬───────────┘
                               │
                       State + Domain Events
                               │
              ┌────────────────┼────────────────┐
              │                │                │
              ▼                ▼                ▼
         Godot Client     Terminal Client    Test Harness
```

Input becomes a **domain command**.

The simulation validates and resolves that command, updates the authoritative `GameState`, and emits **domain events** describing what happened.

Presentation code consumes those results but does not own gameplay rules.

### Why?

This keeps movement, combat, dungeon generation, knowledge, progression, saves, and other game systems independent of any particular user interface.

The same simulation can eventually power:

* a modern Godot presentation;
* a retro-inspired presentation;
* a Terminal interface;
* deterministic tests and debugging tools.

---

## Project Structure

```text
telengard-reloaded/
├── src/
│   ├── Telengard.Core/       # Authoritative simulation
│   ├── Telengard.Content/    # Content definitions and rules
│   ├── Telengard.Save/       # Save DTOs and migrations
│   ├── Telengard.Terminal/   # Terminal presentation boundary
│   └── Telengard.Godot/      # Godot 4 .NET presentation
│
├── tests/
│   └── Telengard.Architecture.Tests/
│
├── tools/
│   └── Telengard.TestHarness/
│
├── content/
│   ├── bands/
│   ├── encounter_tables/
│   ├── features/
│   ├── items/
│   ├── loot_tables/
│   ├── monsters/
│   ├── spells/
│   └── talents/
│
├── docs/                     # Design, architecture, plans, and status
├── eng/                      # Build and verification tooling
└── Telengard.sln
```

### Main Projects

| Project                 | Responsibility                                                                                                               |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `Telengard.Core`        | Renderer-independent game simulation, commands, events, deterministic RNG, world state, combat, exploration, and progression |
| `Telengard.Content`     | Content definitions and content-driven behavior                                                                              |
| `Telengard.Save`        | Explicit save DTOs, serialization, versioning, and migrations                                                                |
| `Telengard.Terminal`    | Lightweight console presentation and eventual playable Terminal client                                                       |
| `Telengard.Godot`       | Godot 4 .NET scenes, rendering, audio, and input                                                                             |
| `Telengard.TestHarness` | Deterministic simulation and developer testing tools                                                                         |

---

## Technology

Telengard Reloaded uses:

* **C# 12**
* **.NET 8**
* **Godot 4 .NET**
* **xUnit**
* **System.Text.Json**
* deterministic, named RNG streams
* explicit save-schema migrations
* data-oriented content boundaries

The authoritative simulation does **not** depend on Godot.

This allows most game logic to build and run headlessly without launching a graphics engine.

---

## Getting Started

### Requirements

For the headless solution:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

The repository pins .NET SDK `8.0.100` and permits compatible feature-band roll-forward.

For graphical development:

* [Godot 4 .NET](https://godotengine.org/download/)

Godot is not required to build or test the headless simulation.

### Clone the Repository

```bash
git clone https://github.com/JackSmack1971/telengard-reloaded.git
cd telengard-reloaded
```

### Restore

```bash
dotnet restore Telengard.sln
```

### Build

```bash
dotnet build Telengard.sln --configuration Release
```

### Run the Tests

```bash
dotnet test Telengard.sln --configuration Release --no-restore
```

### Verify Formatting

```bash
dotnet format Telengard.sln --verify-no-changes
```

### Full Repository Verification

From PowerShell:

```powershell
./eng/verify.ps1 -Mode Full
```

The full verification path checks the repository's configured build, formatting, and test gates.

---

## Deterministic by Design

Procedural generation is a core part of Telengard, but reproducibility matters just as much.

Telengard Reloaded does not rely on one global random-number generator.

Instead, systems derive independent deterministic streams from stable information such as:

```text
world seed
generator version
system / stream name
floor
position
expedition
entity
activation count
```

For example:

```text
layout       → world seed + generator version + floor
encounter    → world seed + expedition + tick
feature      → world seed + feature + activation count
loot         → world seed + location + context
```

This means unrelated changes in one system should not unexpectedly regenerate another part of the world simply because an extra random number was consumed.

Determinism is treated as part of the game's architecture rather than as a testing convenience.

---

## Saves and Compatibility

Save files are explicit data contracts.

Runtime objects are **not** serialized directly as an accidental save format.

The save system maintains dedicated DTOs and migrations so that changes to the game's runtime model can be handled deliberately.

Saved state preserves version information for areas such as:

* save schema;
* simulation;
* dungeon generation;
* content.

The intent is for old saves to have a defined migration path as development progresses.

---

## Development Roadmap

The immediate goal is **Core Alpha**: one complete, deterministic, end-to-end expedition.

Near-term work includes:

* character creation;
* deterministic world-seed selection;
* initial player setup;
* treasure acquisition and unsecured loot;
* completion of the expedition gameplay loop;
* legacy knowledge handoff;
* deterministic vertical-slice integration;
* developer/debug command tooling;
* a usable Terminal presentation;
* the first playable Godot presentation.

After the core loop is proven, development can increasingly shift toward:

* data-driven monsters, items, spells, and dungeon features;
* dungeon ecology and depth bands;
* balance and progression;
* additional feature interactions;
* presentation, sound, and atmosphere;
* expanding the reasons to risk one more floor.

See [`docs/BUILD_STATUS.md`](docs/BUILD_STATUS.md) for the current implementation state and [`docs/tasks/`](docs/tasks/) for scoped work items.

---

## Documentation

The repository contains detailed design and engineering documentation.

| Document                                                    | Purpose                                                   |
| ----------------------------------------------------------- | --------------------------------------------------------- |
| [`modern-telengard-spec.md`](docs/modern-telengard-spec.md) | Product and implementation specification                  |
| [`ARCHITECTURE.md`](docs/ARCHITECTURE.md)                   | Architectural boundaries and system organization          |
| [`INVARIANTS.md`](docs/INVARIANTS.md)                       | Rules that must remain true across implementation changes |
| [`DEVELOPMENT.md`](docs/DEVELOPMENT.md)                     | Development workflow and implementation conventions       |
| [`BUILD_STATUS.md`](docs/BUILD_STATUS.md)                   | Current implementation and verification status            |
| [`PLANS.md`](docs/PLANS.md)                                 | ExecPlan process for significant work                     |
| [`docs/adr/`](docs/adr/)                                    | Architecture Decision Records                             |
| [`docs/tasks/`](docs/tasks/)                                | Scoped TEL implementation tasks                           |
| [`docs/gates/`](docs/gates/)                                | Phase acceptance criteria and results                     |

The design specification is intentionally more detailed than this README. The README is the project's front door; the `docs/` directory is the source for deeper technical and design information.

---

## Engineering Principles

A few rules are intentionally treated as architectural contracts:

* authoritative gameplay state belongs to the simulation;
* renderers do not implement separate gameplay rules;
* state changes are validated before they are committed;
* committed changes produce domain events;
* randomness must be deterministic and scoped;
* hidden information remains hidden until legitimately observed;
* carried wealth and secured wealth are distinct concepts;
* content definitions stay separate from runtime state;
* saves use explicit DTOs and migrations;
* simulation behavior must remain testable without launching Godot.

These constraints exist to protect the design as the project becomes larger.

---

## Contributing

Contributions, suggestions, and bug reports are welcome.

Before making a significant gameplay or architecture change, please review:

* [`docs/modern-telengard-spec.md`](docs/modern-telengard-spec.md)
* [`docs/INVARIANTS.md`](docs/INVARIANTS.md)
* [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)

Large features, broad refactors, save migrations, and architectural changes should follow the ExecPlan process described in [`docs/PLANS.md`](docs/PLANS.md).

Before submitting code, run the full verification gate:

```powershell
./eng/verify.ps1 -Mode Full
```

Keeping the simulation deterministic and renderer-independent is more important than preserving any particular implementation.

---

## Inspiration

Telengard Reloaded began as an attempt to revisit a game I loved as a kid.

The aim is to preserve the feeling that made that experience memorable rather than simply reproducing its technology:

**mystery, danger, discovery, greed, loss — and the irresistible question of whether to descend one more floor.**

---

## License

The source code in this repository is licensed under the [MIT License](LICENSE).
