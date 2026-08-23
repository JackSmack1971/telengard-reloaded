# Modern Telengard architecture

## Current repository state

The repository contains a C#/.NET 8 renderer-independent solution, engineering-contract documents, data-driven content loading, Terminal/Modern presentation projections, and a separate Godot presentation boundary.

Core Alpha system composition is proven headlessly. TEL-090–TEL-093 prove presentation separation and renderer-independent save compatibility. `src/Telengard.Godot/ModernRenderer.tscn` remains a visual renderer prototype rather than a complete playable application host.

The current convergence milestone is a **Playable Godot Vertical Slice**. TEL-110–TEL-116 author the representative floors 1-5 content while TEL-120–TEL-127 build the production-shaped Godot client using placeholder/graybox presentation. See `docs/presentation/GODOT_CLIENT_BLUEPRINT.md` and the active Godot umbrella ExecPlan.

The structure below separates implemented boundaries from incomplete client/presentation work. Future agents must still verify the actual tree and current task ledger before extending any path.

## Architectural boundary

```text
keyboard / controller / UI intent
              |
              v
      application/input adapter
              |
              v
        simulation command
              |
              v
simulation ---> authoritative GameState
        |
        +---- committed domain events
        |
        v
PresentationStateAdapter
        |
        +---- Modern projection ---> Godot client
        +---- Terminal projection -> Terminal client
```

Commands are requests. The simulation validates commands and is the only owner of authoritative gameplay state. Domain events describe committed domain facts for presentation, knowledge, audio, telemetry, debugging, and legacy mechanics.

## Domain areas: implemented slices and planned boundaries

Status labels:

```text
[implemented]         authoritative production boundary exists
[implemented slices]  substantial behavior exists; additional scope remains
[prototype]           proof/boundary exists; not a complete user-facing client
[planned]             explicit TEL/blueprint work exists but is not implemented
```

```text
core/          [implemented]         simulation, rng, events, time
world/         [implemented slices]  dungeon, generation, floors, features, visibility
actors/        [implemented slices]  player/monster runtime state and creation boundaries
combat/        [implemented slices]  encounters, actions, damage, fleeing, threat
items/         [implemented slices]  inventory, equipment, treasure, affixes, identification
magic/         [implemented slices]  spells and spell effects; later experimentation may remain
knowledge/     [implemented slices]  journal, observations, confidence, cartography
progression/   [implemented slices]  experience, talents, legacy
economy/       [implemented]         carried wealth, secured wealth
meta/          [implemented slices]  setup/suspension/save/profile-related boundaries
content/       [implemented boundary] external versioned pack; TEL-110–116 author slice data
presentation/  [implemented prototype] safe projections + Modern/Terminal renderer proofs
Godot host/UI  [planned]             TEL-120–127 playable application/client work
```

Explicit save DTOs and migrations live in `Telengard.Save`; they are part of the meta boundary but are intentionally separate from Core domain folders.

Keep content definitions (monsters, items, spells, features, bands, loot tables, encounter tables, talents) separate from simulation systems and Godot resource mappings.

## Authoritative state

The simulation exposes one authoritative `GameState`, including version fields, world seed, simulation tick, player, expedition, dungeon, knowledge, legacy, secured progress, and applicable settings. Runtime objects must not become the save format; use explicit save DTOs and versioned migrations.

Godot scene trees, nodes, animation state, camera state, focus state, texture/audio resources, and resource paths are presentation/application state and do not become authoritative gameplay state merely because the Godot client uses them.

## Determinism

Use named deterministic RNG streams derived from stable inputs such as world seed, generator version, floor, location, expedition, tick, entity, or feature activation count. Preserve generator, simulation, content, and save versions in the appropriate state/save boundaries.

Simulation speed and outcomes must not depend on rendering FPS. Godot frame callbacks may drive an application/time adapter, but authoritative time progression follows the renderer-independent time contract.

## Presentation

Modern, Retro+, and Terminal modes are alternate presentations of the same simulation. Do not implement separate gameplay rules per renderer.

TEL-090/TEL-091 establish a renderer-safe Modern projection, but that prototype contract may be expanded when production scenes need additional **observable** geometry/theme/identity. The correct dependency direction is:

```text
hidden/runtime state
      X  (no direct renderer access)

GameState
  -> safe presentation adapter/projection
  -> Godot scene/resource lookup
```

When Godot needs more drawing information, extend the smallest renderer-safe projection and add redaction/determinism tests. Do not reach around the boundary into hidden content/runtime internals.

## Godot application host

TEL-120–TEL-127 own the missing application/client layer described by `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`.

The host is responsible for:

- content bootstrap;
- creation/load of authoritative session state;
- command dispatch wiring;
- committed event collection;
- presentation projection refresh;
- simulation-clock integration;
- scene/session navigation derived from authoritative/application state;
- explicit persistence flow;
- presentation resource registry and placeholder mapping.

Godot visual nodes may own transient display state but never authoritative movement, combat, item, feature, knowledge, wealth, death, RNG, or save semantics.

## Presentation resources

Stable content/presentation IDs map to Godot assets through a presentation-side registry. Godot resource paths/UIDs do not belong in `GameState` or save DTOs. See `docs/presentation/ASSET_PIPELINE_BLUEPRINT.md`.

This allows final art to be replaced or reorganized without changing simulation identity or save meaning.

## Production-art transition

The architecture intentionally uses placeholders/graybox assets while client and projection contracts are being proven.

Systematic final asset batches are not architecture prerequisites. They become eligible only after:

1. the real first-slice content exists;
2. the full Godot loop works through the authoritative boundaries;
3. the presentation/resource mapping is stable;
4. `docs/gates/GODOT-PLAYABLE-SLICE.md` passes;
5. `docs/gates/ART-PRODUCTION-READY.md` passes.

Concept/style exploration before those gates is presentation design work, not authorization to hard-code production asset contracts.
