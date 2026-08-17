# Phase 3 acceptance gate

Date verified: 2026-08-16

## Result

**PASS** — exploration can create a configured, deterministic encounter and
retain it as active combat. The headless simulation covers the fast encounter
path through public commands and domain events: contact, threat assessment,
player action, resolution, enemy-action transition, state check, and
termination by defeat, flee, or death.

## Evidence

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Exploration creates an encounter | `Phase3AcceptanceTests.Exploration_command_commits_encounter_state_before_publishing_start` dispatches `EnterDungeonCommand` and `MoveCommand` with a configured trigger, then verifies active `CombatState` and `EncounterStartedEvent`. | PASS |
| Encounter state is committed before event delivery | The same test observes `DomainEventBus` delivery and confirms `CommandDispatcher.CurrentState.Combat` already contains the started encounter. | PASS |
| Monster instance state is retained | The exploration test verifies instance ID linkage, definition ID, level, hit points, position, and contact phase. `MonsterSchemaTests.Instance_contains_runtime_state_separate_from_definition` covers runtime effects and behavior state. | PASS |
| Attack | `Phase3AcceptanceTests.Public_combat_commands_cover_defend_attack_flee_and_termination` dispatches `SelectCombatActionCommand(Attack)` and `AttackCommand`; `AttackTests` covers damage, lethal closure, validation, replay, and save round trip. | PASS |
| Defend | The public lifecycle test dispatches `SelectCombatActionCommand(Defend)` and `DefendCommand`; `DefendTests` verifies the committed enemy-action transition and validation boundaries. | PASS |
| Flee and disengagement path | `FleeTests` verifies both successful termination and failed flee continuation. Flee is an explicit combat action and cannot be bypassed or removed by the phase machine unless an explicitly defined effect is added later. | PASS |
| Threat classification | `ThreatAssessmentTests.Classification_exposes_only_configured_approximate_levels` verifies `TRIVIAL`, `DANGEROUS`, `DEADLY`, and `UNKNOWN`; command resolution commits the category before emitting `ThreatAssessedEvent`. | PASS |
| Player death | `Phase3AcceptanceTests.Lethal_state_check_command_publishes_death_and_closes_encounter` dispatches `AdvanceCombatCommand` at a lethal state check and verifies player death, expedition failure, and encounter closure. `DeathTests` covers validation, replay, save, and post-death movement/floor restrictions. | PASS |
| Encounter termination | Lethal attack clears combat and increments defeated-monster state; successful flee emits `EncounterEndedEvent`; death clears combat and emits `PlayerDiedEvent` followed by `ExpeditionFailedEvent`. | PASS |
| Deterministic outcomes | Encounter replay uses the scoped `encounter` stream derived from world seed, generator version, expedition, tick, floor, and position. Flee replay uses the scoped `flee` stream derived from world seed, generator version, encounter, and round. `EncounterTriggerTests` and `FleeTests.Equal_flee_inputs_replay_to_equal_state_and_events` verify equal state/event results for equal controlled inputs. | PASS |
| Approximate threat information only | Threat assessment emits a `ThreatLevel` category, not monster statistics or `MonsterDefinition` fields. Unknown definition IDs resolve to `UNKNOWN`; content definitions and runtime instances remain separate in `MonsterSchemaTests`. | PASS |

## Invariants and scope

- Authoritative encounter and combat state remains in `Telengard.Core`; the
  dispatcher accepts commands and the event bus publishes committed facts.
- Movement now commits the encounter resolver's returned state instead of only
  appending its events. This prevents exploration from reporting an encounter
  that is absent from authoritative state.
- The same seed, version set, initial state, and command inputs reproduce the
  same encounter and flee outcomes. No process-global or uncontrolled RNG was
  introduced.
- The player always has an explicit `Flee` action path in the combat action
  contract. No immobilizing or escape-preventing effect is defined in this
  phase.
- Threat output is category-only. Monster definitions are not used as an
  implicit player-knowledge channel, and the threat event does not carry exact
  hidden statistics.
- No dungeon feature system, new encounter catalog, canonical balance formula,
  enemy-damage formula, or mode-specific death-loss policy was introduced.
  Spawn, flee, damage, and threat thresholds remain explicit configuration
  where the specification does not define canonical tuning.

## Verification

Commands:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 test Telengard.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 format Telengard.sln --verify-no-changes --no-restore
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Focused Phase 3 verification: 44 tests passed, 0 failed. The complete
solution test run before the final gate: 152 tests passed, 0 failed.

## Scope confirmation

- No dungeon feature work was implemented.
- The only production correction was the encounter-state commit in movement;
  the remaining changes are acceptance coverage and this gate document.
- No save schema or version field changed in this review.
