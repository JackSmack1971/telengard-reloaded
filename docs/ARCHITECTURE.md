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

## Domain areas: implemented slices and reserved boundaries

The map below is a conceptual grouping of the current tree and the proposed
target architecture. It is not a claim that any area is complete. Status
labels distinguish implemented slices from presentation boundaries and areas
that remain reserved for future work:

```text
[implemented]         authoritative simulation code exists
[implemented slices]  part of the area is implemented; more scope remains
[boundary]             integration boundary exists; the client/system is incomplete
[reserved]             target area is identified but not implemented
```

```text
core/          [implemented]         simulation, rng, events, time
world/         [implemented slices]  dungeon, generation, floors, features, visibility
actors/        [reserved]            player, monsters, stats, effects
combat/        [implemented slices]  encounters, actions, damage, fleeing, threat
items/         [implemented slices]  inventory, equipment, treasure, affixes, identification
magic/         [implemented slices]  spells and spell effects; experimentation remains
knowledge/     [implemented slices]  journal, observations, confidence, cartography
progression/   [implemented slices]  experience, talents, legacy
economy/       [implemented]         carried wealth, secured wealth
meta/          [implemented slices]  suspension; save/profile scope remains partial
presentation/  [boundary]            modern, retro, terminal presentation boundaries
ui/            [reserved]             input, menus, HUD
```

Explicit save DTOs and migrations live in `Telengard.Save`; they are part of
the meta boundary but are intentionally separate from the Core domain folders.

Keep content definitions (monsters, items, spells, features, bands, loot tables, encounter tables, talents) separate from these simulation systems. The eventual file/resource format is intentionally undecided by the specification.

## Authoritative state

The simulation should expose one `GameState`, including version fields, world seed, simulation tick, player, expedition, dungeon, knowledge, legacy, secured progress, and settings. Runtime objects must not become the save format; use explicit save DTOs and versioned migrations.

## Determinism

Use named deterministic RNG streams derived from stable inputs such as world seed, generator version, floor, location, expedition, tick, entity, or feature activation count. Preserve generator, simulation, content, and save versions in the appropriate state/save boundaries.

## Presentation

Modern, Retro+, and Terminal modes are alternate presentations of the same simulation. Do not implement separate gameplay rules per renderer. Presentation may interpret state and events, but cannot become an authority for movement, combat, knowledge, wealth, progression, or save state.
