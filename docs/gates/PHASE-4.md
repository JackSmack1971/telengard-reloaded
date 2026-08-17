# Phase 4 acceptance gate

Date verified: 2026-08-16

## Result

**PASS** — the renderer-independent feature framework supports the required
fountain, altar, pit, and teleporter vertical slices. Feature outcomes are
selected deterministically from weighted content definitions, committed
through the shared simulation activation boundary, and reported as domain
events without exposing outcome weights.

## Evidence

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Generic feature definitions and runtime state are separate | `FeatureDefinition`/`FeatureOutcome` live under `src/Telengard.Content/Features/`; `FeatureInstance`, `ActivateFeatureCommand`, activation validation, and domain events live under `src/Telengard.Core/world/features/FeatureSystem.cs`. `FeatureSystemTests.Definition_preserves_feature_schema_without_owning_runtime_state` verifies the boundary. | PASS |
| Shared feature activation/event infrastructure | `FeatureActivationResolver` validates active, living, non-combat, known-feature, and current-position boundaries, commits activation/discovery state, then returns events. Generic activations use `FeatureOutcomeResolvedEvent`; typed outcome events share the same committed activation path. `FeatureSystemTests.Activation_validates_position_and_commits_discovery_and_activation_events` and `Generic_outcome_activation_commits_opaque_outcome_without_content_specific_state_changes` verify this. | PASS |
| Required feature types exist | `FountainResolver`, `AltarResolver`, `PitResolver`, and `TeleporterResolver` each select through `FeatureOutcomeEngine` and resolve through Core activation infrastructure. `FountainTests`, `AltarTests`, `PitTests`, and `TeleporterTests` cover each slice. | PASS |
| Outcomes may be weighted | `FeatureOutcome.Weight` is a validated non-negative integer; `FeatureOutcomeEngine.Select` filters eligible positive-weight outcomes and selects across the checked total weight. `FeatureOutcomeEngineTests.Select_uses_each_outcomes_weight_as_its_share_of_the_roll_range` and `Select_filters_by_conditions_and_ignores_zero_weight_outcomes` verify this. | PASS |
| Equivalent deterministic inputs replay equivalent outcomes/state/events | Selection scope includes content version, definition, feature id, position, and activation count in addition to the world seed. Feature outcome and all four content test classes cover stable replay; altar replay compares event payloads structurally. | PASS |
| Raw probability tables are not sent to the player | Outcome events expose only the selected effect/observation tags and committed positions/counts. They contain no `Weight` or `OutcomeTable`; selection remains in `FeatureOutcomeEngine`. | PASS |
| Features emit domain observations/events | The shared path emits `FeatureDiscoveredEvent` on first activation and `FeatureActivatedEvent` after commit. Outcome resolution emits `FeatureOutcomeResolvedEvent`, `FountainOutcomeResolvedEvent`, `AltarOutcomeResolvedEvent`, `PitOutcomeResolvedEvent`, or `TeleporterOutcomeResolvedEvent` with configured observations. Event ordering is asserted in the feature tests. | PASS |
| Outcomes can materially alter expedition state | Fountain outcomes update spell power/temporary effects; pit and teleporter outcomes update player position and expedition floor history. Altar outcomes remain configured observations/effect tags because canonical altar state effects are unspecified. | PASS |
| Pits may alter floor/position | `PitEffectIds.DropTwoFloors` moves the player two floors at the same coordinates, updates deepest/visited floor state, and emits `PitOutcomeResolvedEvent`; `PitTests.Pit_resolution_drops_the_player_two_floors_and_updates_expedition_depth` verifies it. Invalid floor destinations fail before mutation. | PASS |
| Teleporters may alter position | `TeleporterResolver` commits the configured destination, updates expedition floor tracking, and emits source/destination in `TeleporterOutcomeResolvedEvent`; `TeleporterTests.Teleporter_resolution_moves_the_player_and_updates_expedition_depth` verifies it. | PASS |
| Feature simulation is renderer-independent | Core has no reference to Godot, Terminal, or renderer assemblies; `DependencyBoundaryTests.Core_does_not_reference_presentation_or_engine_assemblies` passes. Content calls simulation commands/resolvers rather than owning authoritative state. | PASS |
| Generic infrastructure is not fountain-specific | The generic outcome overload now uses an opaque `Generic` path. Fountain state effects are selected only by explicit `ActivateFountain`; altar uses explicit `ActivateAltar` and does not apply fountain effects. `FeatureSystemTests.Generic_outcome_activation_commits_opaque_outcome_without_content_specific_state_changes` and `AltarTests.Altar_outcomes_do_not_apply_fountain_effects` cover the boundary. | PASS |

## Invariants and scope

- State-changing activation is validated and committed in `Telengard.Core`;
  events are produced after the committed state is constructed.
- Authoritative randomness uses the existing scoped deterministic RNG. No
  process-global randomness or renderer-dependent input was introduced.
- Feature runtime state remains explicit save state; the Phase 4 outcome
  routing change adds no persisted fields or save-version migration.
- Effects and conditions remain content-defined tags unless the specification
  defines a state transition. Altar mechanics, feature knowledge, teleporter
  mapping, and unresolved fountain transformation mechanics remain
  `CONFIGURATION/TUNING DECISION REQUIRED` or later-scope work.
- No default content weights or canonical balance formulas were invented.

## Verification

Commands:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 test Telengard.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 format Telengard.sln --verify-no-changes --no-restore
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Results:

- Complete solution test run: **198 passed, 0 failed**.
- Format verification: passed.
- Full verification: restore passed, format verification passed, Release
  build passed with 0 warnings and 0 errors, and 198 Release tests passed.

The host rejects unsigned local PowerShell scripts under its default policy;
verification used a process-scoped execution-policy bypass and the mandated
repository scripts.

## Review change

The acceptance review corrected a shared-path defect: the outcome overload
previously defaulted to fountain semantics, allowing an altar outcome to
apply a fountain effect. Generic, fountain, and altar activation paths are
now explicit, with regression coverage for opaque generic outcomes and
cross-type effect isolation.
