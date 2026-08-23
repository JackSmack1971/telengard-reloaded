# Modern Telengard invariants

These are testable contracts derived from `docs/modern-telengard-spec.md` and the repository's renderer-independent architecture. New domain or client behavior must add or update the smallest relevant tests/acceptance evidence.

## Simulation and presentation

- Rendering cannot directly mutate authoritative `GameState`.
- The same command sequence is resolved by the simulation regardless of renderer.
- Every state-changing command is validated by the simulation before mutation.
- Domain events describe simulation results; presentation code does not invent authoritative results.
- UI, scene, input, animation, and audio callbacks may request or visualize simulation outcomes; they do not resolve authoritative movement, combat, feature, item, knowledge, wealth, death, or progression outcomes.
- When presentation needs additional drawing information, expose the smallest renderer-safe observable projection rather than allowing direct access to hidden runtime/content internals.

## Determinism and generation

- Same `world_seed` + `generator_version` + location produces the same base dungeon geography.
- The same seed, version set, initial state, and command sequence produce the same authoritative result.
- RNG streams are derived and scoped; unrelated code must not change another system's random sequence by consuming a shared global stream.
- Simulation speed and outcomes do not depend on rendering FPS.
- Visual-only interpolation/effects do not consume or alter authoritative RNG state.

## Visibility and knowledge

- Unknown spaces cannot become fully mapped without player discovery.
- A journal fact cannot appear without a qualifying player observation.
- Internal definitions, hidden outcomes, raw danger values, and unobserved resistances are not automatically player knowledge.
- Persistent knowledge may improve future knowledge or threat descriptions, but does not expose facts never observed.
- Presentation projections and resource selection must not leak hidden geography, raw danger, unobserved monster internals, or hidden feature outcomes.

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
- Presentation resource mappings do not duplicate gameplay/content rules; stable content/presentation IDs map to visual/audio resources outside authoritative simulation state.

## Godot/client resources

- Godot resource paths, resource UIDs, scene/node identity, texture/audio paths, camera state, focus state, and transient animation state are not authoritative gameplay state.
- Engine-specific resource identifiers are not persisted in `GameState` save DTOs merely to reconstruct presentation.
- Missing final assets use the documented presentation registry/placeholder path during graybox development rather than bypassing the production-shaped mapping boundary.
- Systematic final production-art/audio batches are not treated as implementation prerequisites before `docs/gates/ART-PRODUCTION-READY.md` has passing evidence.

## Saves and versions

- Saves contain `save_version`, `simulation_version`, `generator_version`, and `content_version`.
- Save loading uses explicit DTOs and migrations rather than assuming serialized runtime objects remain compatible.
- Loading and then continuing a valid save preserves authoritative simulation state and deterministic behavior.
- Reconstructing a Godot scene after load must derive from authoritative state/projections; serialized scene objects are not required for gameplay correctness.
