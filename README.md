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
> **Telengard Reloaded is in pre-alpha development. The immediate product milestone is the Five-Floor MVP Demo.**
>
> The simulation, first-slice content, Godot host/session shell, graybox renderer, and basic HUD/input bridges exist. The remaining MVP work is primarily integration: compose those systems into one legitimate floor-1-through-floor-5 Godot playthrough.

A significant portion of the underlying game already exists, including:

* deterministic dungeon generation, visibility, traversal, and floor-transition rules;
* expedition state and carried vs. secured progress;
* encounters, combat actions, threat assessment, and death policies;
* dungeon features such as fountains, altars, pits, and teleporters;
* items, equipment, affixes, curses, identification, spells, and progression primitives;
* player knowledge and persistent discoveries;
* deterministic random-number streams;
* explicit versioned save data and migrations;
* headless simulation and acceptance testing;
* a production content-pack boundary with representative floors 1–5 content; and
* a hosted Godot graybox client path with renderer-safe projections.

What does **not** exist yet is the complete MVP composition: the current hosted client still needs floor-aware multi-floor session wiring, production runtime encounter/feature/treasure composition, a demo-ready combat/setup path, and a real fixed-seed five-floor acceptance run.

The canonical MVP definition is [`docs/MVP_DEMO.md`](docs/MVP_DEMO.md). Current TEL status is maintained in [`docs/tasks/README.md`](docs/tasks/README.md); [`docs/BUILD_STATUS.md`](docs/BUILD_STATUS.md) is append-only verification history rather than the current scheduling ledger.

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

## Current Milestone: Five-Floor MVP Demo

The immediate goal is deliberately narrower than the full Playable Godot Vertical Slice gate.

A successful MVP lets a player launch the Godot client, start a deterministic demo session, enter the real first-slice dungeon, legitimately traverse floors 1–5, encounter representative authored gameplay, and reach a clear floor-5 end-of-demo state without developer/debug commands.

Fixed seed/demo character setup and graybox presentation are acceptable. Save/load UX, full controller parity, complete death/Legacy flow, production art, and the broader TEL-127 acceptance checklist are **post-MVP** work.

The remaining implementation sequence is:

1. **TEL-129** — compose deterministic floors 1–5 into the hosted Godot session;
2. **TEL-130** — compose authored encounters, features, and treasure into normal hosted play;
3. **TEL-131** — close the explicit demo-ready setup and combat playthrough path;
4. **TEL-132** — perform and record the real fixed-seed five-floor Godot acceptance run.

After TEL-132 passes, development returns to TEL-126 save/suspend/resume, TEL-127 full Playable Godot Vertical Slice acceptance, and TEL-128 Art Production Ready.

See [`docs/gates/FIVE-FLOOR-MVP-DEMO.md`](docs/gates/FIVE-FLOOR-MVP-DEMO.md) and [`docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md`](docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md).

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

This keeps movement, combat, dungeon generation, knowledge, progression, saves, and other game systems independent of any particular user interface. The same simulation can power the Godot client, a retro-inspired presentation, the Terminal interface, and deterministic tests/debugging tools.

---

## Project Structure

```text
telengard-reloaded/
├── src/
│   ├── Telengard.Core/       # Authoritative simulation
│   ├── Telengard.Content/    # Content definitions and rules
│   ├── Telengard.Save/       # Save DTOs and migrations
│   ├── Telengard.Terminal/   # Terminal presentation boundary
│   └── Telengard.Godot/      # Godot presentation/application shell
│
├── tests/
│   └── Telengard.Architecture.Tests/
│
├── tools/
│   ├── Telengard.GodotHost/  # External authoritative host for Godot
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
├── docs/                     # Design, architecture, plans, status, gates
├── eng/                      # Build and verification tooling
└── Telengard.sln
```

### Main Projects

| Project | Responsibility |
| --- | --- |
| `Telengard.Core` | Renderer-independent game simulation, commands, events, deterministic RNG, world state, combat, exploration, and progression |
| `Telengard.Content` | Content definitions and content-driven behavior |
| `Telengard.Save` | Explicit save DTOs, serialization, versioning, and migrations |
| `Telengard.Terminal` | Lightweight console presentation |
| `Telengard.Godot` | Godot scenes, graybox rendering, client input, and presentation-only session state |
| `Telengard.GodotHost` | External .NET composition/transport boundary between Godot intent and Core |
| `Telengard.TestHarness` | Deterministic simulation and developer testing tools |

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

The authoritative simulation does **not** depend on Godot. Most game logic therefore builds and runs headlessly without launching the engine.

---

## Getting Started

### Requirements

For the headless solution:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

The repository pins .NET SDK `8.0.100` and permits compatible feature-band roll-forward. A fresh clone must provision the ignored repository-local SDK before running the checks; follow the [SDK provisioning instructions](docs/DEVELOPMENT.md#provisioning-the-repository-local-sdk).

For graphical development:

* [Godot 4 .NET](https://godotengine.org/download/)

Godot is not required to build or test the headless simulation.

### Clone the Repository

```bash
git clone https://github.com/JackSmack1971/telengard-reloaded.git
cd telengard-reloaded
```

### Restore

```powershell
./eng/dotnet.ps1 restore Telengard.sln
```

### Build

```powershell
./eng/dotnet.ps1 build Telengard.sln --configuration Release
```

### Run the Tests

```powershell
./eng/dotnet.ps1 test Telengard.sln --configuration Release --no-restore
```

### Verify Formatting

```powershell
./eng/dotnet.ps1 format Telengard.sln --verify-no-changes
```

### Full Repository Verification

```powershell
./eng/verify.ps1 -Mode Full
```

---

## Deterministic by Design

Procedural generation is a core part of Telengard, but reproducibility matters just as much. Telengard Reloaded does not rely on one global random-number generator.

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

This means unrelated changes in one system should not unexpectedly regenerate another part of the world simply because an extra random number was consumed. Determinism is treated as part of the game's architecture rather than as a testing convenience.

---

## Saves and Compatibility

Save files are explicit data contracts. Runtime objects are **not** serialized directly as an accidental save format.

The save system maintains dedicated DTOs and migrations so that changes to the runtime model can be handled deliberately. Saved state preserves version information for the save schema, simulation, dungeon generation, and content.

Godot save/suspend/resume integration remains valid work, but it is intentionally scheduled **after** the five-floor MVP is proven.

---

## Development Roadmap

The immediate roadmap is product-first rather than subsystem-first:

**Five-Floor MVP Demo**

```text
TEL-129 multi-floor hosted session
        ↓
TEL-130 encounters/features/treasure in normal play
        ↓
TEL-131 demo setup + combat closure
        ↓
TEL-132 real fixed-seed floor-1 → floor-5 acceptance
```

Then:

```text
TEL-126 Godot persistence lifecycle
        ↓
TEL-127 full Playable Godot Vertical Slice
        ↓
TEL-128 Art Production Ready
        ↓
production art/audio batches
```

The full design still includes deeper ecology, balance/progression, additional feature interactions, presentation atmosphere, and many more reasons to risk one more floor. Those are not allowed to displace the current MVP integration milestone unless the product priority is explicitly changed.

---

## Documentation

| Document | Purpose |
| --- | --- |
| [`MVP_DEMO.md`](docs/MVP_DEMO.md) | Current product milestone and scope |
| [`tasks/README.md`](docs/tasks/README.md) | Current human-readable TEL status ledger |
| [`tasks/index.json`](docs/tasks/index.json) | Generated compact next-slice scheduling view |
| [`gates/FIVE-FLOOR-MVP-DEMO.md`](docs/gates/FIVE-FLOOR-MVP-DEMO.md) | MVP acceptance checklist |
| [`exec-plans/active/FIVE-FLOOR-MVP-DEMO.md`](docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md) | Living MVP coordination plan |
| [`modern-telengard-spec.md`](docs/modern-telengard-spec.md) | Product and implementation specification |
| [`ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Architectural boundaries and system organization |
| [`INVARIANTS.md`](docs/INVARIANTS.md) | Cross-cutting rules that must remain true |
| [`DEVELOPMENT.md`](docs/DEVELOPMENT.md) | Development workflow and implementation conventions |
| [`BUILD_STATUS.md`](docs/BUILD_STATUS.md) | Append-only implementation/verification history |
| [`PLANS.md`](docs/PLANS.md) | ExecPlan process for significant work |

The design specification remains the source for long-term product intent. The MVP documents are a project-local sequencing decision about **what to prove next**, not a reduction of the long-term design.

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
* saves use explicit DTOs and migrations; and
* simulation behavior must remain testable without launching Godot.

The MVP is an integration milestone, not permission to weaken these contracts.

---

## Contributing

Contributions, suggestions, and bug reports are welcome.

Before selecting new implementation work, read [`docs/MVP_DEMO.md`](docs/MVP_DEMO.md) and the current task ledger. Work outside the MVP sequence should not pre-empt TEL-129 through TEL-132 unless it is a demonstrated prerequisite, blocker, or explicit product-priority change.

For significant gameplay or architecture changes, also review:

* [`docs/modern-telengard-spec.md`](docs/modern-telengard-spec.md)
* [`docs/INVARIANTS.md`](docs/INVARIANTS.md)
* [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)

Large features, broad refactors, save migrations, and architectural changes should follow the ExecPlan process described in [`docs/PLANS.md`](docs/PLANS.md).

Before submitting code, run the full verification gate:

```powershell
./eng/verify.ps1 -Mode Full
```

---

## Inspiration

Telengard Reloaded began as an attempt to revisit a game I loved as a kid.

The aim is to preserve the feeling that made that experience memorable rather than simply reproducing its technology:

**mystery, danger, discovery, greed, loss — and the irresistible question of whether to descend one more floor.**

---

## License

The source code in this repository is licensed under the [MIT License](LICENSE).
