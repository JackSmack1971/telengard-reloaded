# Phase 1 acceptance gate

Date verified: 2026-08-15

## Result

**PASS** — the headless simulation can enter a seeded floor, walk through
traversable geography, reject walls, find and use stairs, return to the
entrance, leave, and reproduce the same base geography and discovered map.

## Evidence

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Enter a seeded dungeon | `DungeonWalkingTests.Seeded_dungeon_can_be_entered_walked_changed_and_left` enters the floor-1 layout generated from seed `1234` and generator version `generator-1`. | PASS |
| Move through valid traversable space | The same test follows a computed path from the entrance to the down stairs; `Walls_reject_movement_while_corridors_and_doors_are_traversable` proves walkable neighbors move successfully. | PASS |
| Encounter walls/corridors correctly | A wall move is rejected without a state transition; a walkable corridor/room neighbor emits movement and changes position. | PASS |
| Find stairs and change floors | The walking test reaches both required stairs, applies down and up transitions, and verifies the transition event and destination positions. | PASS |
| Leave the dungeon | The returned floor-1 state is at the entrance-side stairs and `LeaveDungeonCommand` emits `DungeonLeftEvent`; leaving elsewhere is rejected by the resolver contract. | PASS |
| Revisit identical base geography | `Geography_and_discovered_map_revisit_identically` compares room definitions and every tile for equal seed, generator version, and floor, then restores the map against the regenerated layout. | PASS |
| Required anchors and stairs are reachable | `FloorLayoutGeneratorTests.Generated_layout_has_one_connected_walkable_region` checks 32 seeds, every room anchor, both stairs, and the complete walkable region. | PASS |
| Generated positions are valid | Layout and visibility tests reject cross-floor and out-of-bounds positions; generated room and stair positions are consumed only within layout bounds. | PASS |
| Unknown spaces remain unmapped | Visibility and walking tests verify positions outside discovery remain `Unknown`/absent from observed map state. | PASS |
| Discovered map state persists correctly | Fog-of-war state round-trips through `PersistentMapState`; save tests round-trip observed and visited positions, preserving the visited-implies-observed invariant. | PASS |

## Determinism and invariants

- Geography is derived from `world_seed`, `generator_version`, and floor scope
  through the named deterministic layout stream.
- The same inputs reproduce rooms, tiles, stairs, visibility, and restored map
  state.
- Required anchors are connected; walls are not traversable; visited positions
  are observed; hidden layout data is not added to map knowledge automatically.
- Authoritative state and domain events remain in Core; no renderer or Godot
  dependency is involved.

## Verification commands

```text
.\.dotnet\dotnet.exe test Telengard.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
.\.dotnet\dotnet.exe build Telengard.sln --configuration Release --no-restore
.\.dotnet\dotnet.exe format Telengard.sln --verify-no-changes --no-restore
```

Results: 43 tests passed, 0 failed; Release build succeeded with 0 warnings
and 0 errors; format verification passed.

## Scope confirmation

- No expedition economy, carried/secured wealth, monsters, combat, or dungeon
  feature system was implemented.
- TEL-020 was not started.
- No new save version or save schema field was required; discovered map state
  continues to use the existing version-2 explicit DTO boundary.
- No content, balance, or depth-ecology decisions were introduced.
