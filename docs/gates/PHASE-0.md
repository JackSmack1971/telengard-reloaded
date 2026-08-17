# Phase 0 acceptance gate

Date verified: 2026-08-15

## Result

**PASS** — TEL-001 through TEL-006 provide a headless deterministic simulation
skeleton that starts, accepts commands, changes authoritative state, emits
domain events, saves, loads, and reproduces the same result.

## Evidence

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Same initial state, commands, and deterministic inputs reproduce authoritative state | `SimulationTestHarnessTests.Run_reproduces_rng_driven_state_events_and_save_without_presentation` runs the same RNG-driven command script twice with the same seed, generator version, and save/reload checkpoint; final saves and states match. | PASS |
| Save/load preserves authoritative state | `SaveGameSerializerTests.Serialize_and_deserialize_preserves_authoritative_state` compares canonical serialized state before and after load. DTO loading restores collection fields to canonical arrays. | PASS |
| Events are reproducible | The RNG-driven integration test compares both event sequences; dispatcher tests also prove events publish only after state commit and preserve order. | PASS |
| Renderer/presentation modules are unnecessary for simulation tests | The architecture test project references Core, Save, and TestHarness only; it has no Terminal or Godot project reference. The full simulation integration test is headless. | PASS |
| RNG streams are explicit and deterministic | `DeterministicRngTests` cover same-input sequences, scoped stream separation, bounded values, and invalid inputs. The integration test derives a named stream from world seed, generator version, command stream name, and simulation tick. | PASS |
| Version ownership is defined | `GameState.SaveVersion` owns the save schema version; `GameState.Versions.SimulationVersion` owns simulation rules; `GeneratorVersion` owns deterministic world-generation inputs; `ContentVersion` owns content definitions. `GameStateSaveDto` persists all four and `SaveMigrations` validates `SaveVersion`. | PASS |
| No global uncontrolled RNG is used by simulation code | Repository scan of `src`, `tests`, and `tools` excluding build artifacts found no `Random`, `Random.Shared`, `System.Random`, or `new Random` usage. Core exposes only explicit `DeterministicRng` streams. | PASS |

## TEL-001 through TEL-006 review

- TEL-001: `GameState` is the authoritative renderer-independent state model,
  with stable world and version metadata.
- TEL-002: deterministic named/scoped RNG streams are derived from stable
  inputs and have no shared global generator.
- TEL-003: commands enter through `CommandDispatcher`; handlers return the
  candidate state and events, and failed commands do not commit.
- TEL-004: `DomainEventBus` publishes committed events in order and supports
  typed and broad subscriptions.
- TEL-005: explicit save DTOs and a version validation/migration boundary
  round-trip authoritative state and reject unsupported or malformed saves.
- TEL-006: `SimulationTestHarness` executes scripted commands, supports
  save/reload checkpoints, and compares final saves and event signatures.

## Verification commands

```text
.\.dotnet\dotnet.exe test Telengard.sln --configuration Release --no-restore --logger "console;verbosity=detailed"
.\.dotnet\dotnet.exe build Telengard.sln --configuration Release --no-restore
.\.dotnet\dotnet.exe format Telengard.sln --verify-no-changes --no-restore
git diff --check
```

Results: 23 tests passed, 0 failed; build succeeded with 0 warnings and 0
errors; format and diff checks passed.

## Scope confirmation

- No Phase 1 gameplay functionality was implemented.
- Non-goals remain party management, dialogue-heavy towns, cinematic quest
  campaigns, base building, crafting-material economies, giant incremental
  skill trees, inventory-Tetris, MMO rarity clutter, linear story progression,
  and chosen-one narratives.
- Authoritative state remains in simulation; presentation is a consumer of
  state/events and does not own simulation rules.
- Save impact is limited to the existing version-1 DTO boundary; no new save
  fields or migrations were introduced.
- The test-only commands consume and emit synthetic state/events solely to
  prove the Phase 0 boundary; they are not gameplay features.
