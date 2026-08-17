# Modern Telengard invariants

These are testable contracts derived from `docs/modern-telengard-spec.md`. New domain behavior must add or update the smallest relevant tests.

## Simulation and presentation

- Rendering cannot directly mutate authoritative `GameState`.
- The same command sequence is resolved by the simulation regardless of renderer.
- Every state-changing command is validated by the simulation before mutation.
- Domain events describe simulation results; presentation code does not invent authoritative results.

## Determinism and generation

- Same `world_seed` + `generator_version` + location produces the same base dungeon geography.
- The same seed, version set, initial state, and command sequence produce the same authoritative result.
- RNG streams are derived and scoped; unrelated code must not change another system's random sequence by consuming a shared global stream.
- Simulation speed and outcomes do not depend on rendering FPS.

## Visibility and knowledge

- Unknown spaces cannot become fully mapped without player discovery.
- A journal fact cannot appear without a qualifying player observation.
- Internal definitions, hidden outcomes, raw danger values, and unobserved resistances are not automatically player knowledge.
- Persistent knowledge may improve future knowledge or threat descriptions, but does not expose facts never observed.

## Expedition and economy

- Gold and loot acquired in the dungeon remain carried/unsecured until the player reaches an inn or other defined safety boundary.
- Only the safety-boundary transition converts carried wealth to secured wealth.
- Death resolution applies the selected Classic, Legacy, or Adventure rules without silently changing the wealth model.
- Legacy-mode character death preserves persistent knowledge as specified.

## World and depth

- Dungeon positions use stable floor/x/y coordinates within the valid floor range.
- Required generated anchors and stairs are reachable when the generator contract requires them.
- Features cannot be placed in invalid geometry.
- Deeper bands change ecology, features, hazards, generation, or strategy—not only numeric enemy scaling.

## Combat and content

- The player has a path to attempt disengagement unless an explicitly defined effect prevents it.
- Content definitions are data/configuration; simulation logic does not duplicate content tables as hidden hard-coded alternatives.
- Undefined formulas remain configurable and versioned where their output affects replay, saves, or balance.

## Saves and versions

- Saves contain `save_version`, `simulation_version`, `generator_version`, and `content_version`.
- Save loading uses explicit DTOs and migrations rather than assuming serialized runtime objects remain compatible.
- Loading and then continuing a valid save preserves authoritative simulation state and deterministic behavior.
