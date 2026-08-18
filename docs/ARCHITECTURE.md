# Modern Telengard architecture

## Current repository state

The repository contains a C#/.NET 8 headless solution, engineering-contract
documents, and a separate Godot presentation boundary. The renderer-independent
simulation has implemented and verified dungeon walking, expedition/safety
transitions, encounter/combat slices, and dungeon-feature slices; see
[`docs/BUILD_STATUS.md`](BUILD_STATUS.md) for the current phase and remaining
scope. Terminal and Godot clients remain presentation boundaries rather than
complete playable clients.

The structure below separates implemented boundaries from the specification's
proposed target architecture. Future agents must still verify the actual tree
and current status before using or extending any path.

## Architectural boundary

```text
commands / input
        |
        v
simulation ---> authoritative GameState
        |
        +---- domain events ---> modern renderer
        +---- domain events ---> retro renderer
        +---- domain events ---> terminal renderer
```

Commands are requests. The simulation validates commands and is the only owner of authoritative state. Domain events describe committed domain facts for presentation, knowledge, audio, telemetry, debugging, and legacy mechanics.

## Proposed domain areas

These areas are represented as empty/reserved boundaries; they are not
implemented gameplay systems yet:

```text
core/          simulation, rng, events, time
world/         dungeon, generation, floors, regions, tiles, features, hazards
actors/        player, monsters, stats, effects
combat/        encounters, actions, damage, fleeing, threat
items/         inventory, equipment, treasure, affixes, artifacts, identification
magic/         spells, spell effects, experimentation
knowledge/     journal, observations, confidence, cartography
progression/   experience, talents, legacy
economy/       carried wealth, secured wealth
meta/          save, game modes, profile
presentation/  modern, retro, terminal
ui/            input, menus, HUD
```

Keep content definitions (monsters, items, spells, features, bands, loot tables, encounter tables, talents) separate from these simulation systems. The eventual file/resource format is intentionally undecided by the specification.

## Authoritative state

The simulation should expose one `GameState`, including version fields, world seed, simulation tick, player, expedition, dungeon, knowledge, legacy, secured progress, and settings. Runtime objects must not become the save format; use explicit save DTOs and versioned migrations.

## Determinism

Use named deterministic RNG streams derived from stable inputs such as world seed, generator version, floor, location, expedition, tick, entity, or feature activation count. Preserve generator, simulation, content, and save versions in the appropriate state/save boundaries.

## Presentation

Modern, Retro+, and Terminal modes are alternate presentations of the same simulation. Do not implement separate gameplay rules per renderer. Presentation may interpret state and events, but cannot become an authority for movement, combat, knowledge, wealth, progression, or save state.
