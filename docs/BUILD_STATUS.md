# Modern Telengard build status

Last verified: 2026-08-22

This document is append-only verification history, not the current TEL-ticket
status ledger. Use [the task ledger](tasks/README.md) for current TEL status.
Audit-remediation status is maintained in the canonical ledger and its
generated views described in [the audit-status engineering guide](engineering/audit-status.md).

## AUD-004 verification

- Status: implemented; `.github/workflows/verification.yml` runs the
  canonical `./eng/verify.ps1 -Mode Full` gate on pull requests and pushes to
  `main` using a Windows runner.
- Workflow controls: the job grants `contents: read`, uses concurrency
  cancellation for superseded pull-request runs, and declares no secrets or
  mutation-testing steps.
- SDK provisioning: `actions/setup-dotnet` reads `global.json`, so CI follows
  the repository's existing SDK policy without duplicating a version string in
  workflow YAML. The canonical wrapper runs against that provisioned SDK on
  `PATH` when the ignored local SDK directory is absent.
- Compatibility impact: save, simulation, generator, and content versions are
  unchanged; deterministic vectors, save migrations, and replay compatibility
  are unchanged.
- Verification evidence: workflow structure and the local canonical gate were
  validated in the implementation environment. GitHub Actions PR #27 then
  passed on commit `3957084`, completing restore, format verification, a
  zero-warning Release build, and 329 Release tests. The workflow creates the
  ignored `.codex` stamp directory required by the canonical verifier on a
  clean checkout.
- Coverage evidence: the required separate coverage gate ran all 334 tests but
  failed its strict 100% target at 3,229/3,243 lines (99.57%) and 1,491/1,564
  branches (95.33%). AUD-004 changes no production code; coverage remediation
  remains outside this packet.
- Repository setting: a maintainer must add the `Full verification` check from
  the `Repository verification` workflow as a required status check for
  `main`; the workflow file cannot configure branch protection. At
  implementation time, the GitHub API reported no required status checks for
  `main`.

## Implementation status through Phase 4

Implementation is complete and gated through Phase 4. Phase 1 — Dungeon
Walking Prototype is implemented and gated. The headless,
renderer-independent simulation enters seeded floors, walks valid space,
rejects walls, changes floors, leaves, and revisits identical geography. See
[the Phase 1 gate](gates/PHASE-1.md).

Phase 2 acceptance review completed 2026-08-16 and failed. The implemented
slice covers the successful inn → expedition → carried gold → safety → secured
gold → completion → next expedition loop, plus dedicated suspension/save
resume. TEL-037 now supplies the renderer-independent death/failure transition,
TEL-080 adds the Classic character-deletion policy, TEL-081 adds the Legacy
death policy, TEL-082 adds the Adventure return-to-inn policy, TEL-083
persists Legacy dead-hero records, TEL-084 persists Legacy grave markers, and
TEL-085 persists Legacy heirlooms.
Runtime
producers for the remaining expedition counters also remain absent; see [the
Phase 2 gate](gates/PHASE-2.md).

TEL-030 through TEL-037 are complete as the initial Phase 3 encounter slices.

TEL-085 now persists inventory-derived Legacy heirlooms through the explicit
save contract. Equipment-instance recovery, heirloom selection/balance rules,
and future dungeon retrieval remain intentionally undefined follow-up work.

Phase 4 acceptance review passed 2026-08-16; see [the Phase 4 gate](gates/PHASE-4.md).
The review verified the shared generic feature activation/event path, weighted
deterministic outcome selection, opaque probability boundaries, renderer
independence, and the fountain, altar, pit, and teleporter vertical slices.
It also corrected the shared outcome overload so generic and altar outcomes
cannot apply fountain-specific effects. Full verification passed with 198
Release tests and zero build warnings.
Monster content/runtime contracts, deterministic configured encounter triggers,
the renderer-independent combat state machine, combat actions, threat
classification, and the player death/failure transition now exist.

Phase 3 acceptance review passed 2026-08-16; see
[the Phase 3 gate](gates/PHASE-3.md). The review found and corrected one
movement integration defect: encounter evaluation emitted its start event but
the movement result did not retain the resolver's active combat state. Public
command/domain acceptance coverage now exercises encounter creation, monster
instance state, defend, attack, flee, all four threat categories, lethal state
check/death, deterministic replay, and encounter termination. TEL-040 now adds
the generic feature definition/runtime/activation foundation, TEL-041 adds
deterministic weighted outcome selection, TEL-042 adds fountain outcomes,
TEL-043 adds altar outcome resolution, TEL-044 adds configured pit drops, and
TEL-045 adds configured teleporter relocation; feature-specific knowledge,
enemy-damage production, canonical encounter balance, and heirlooms remain
later scope.

## Core Alpha gap review — 2026-08-17

The task ledger was reviewed against specification §26 Character Creation,
§48 Recommended First Vertical Slice, and §51 Definition of Core Alpha.
Existing TEL-010–TEL-093 tickets cover the dungeon, expedition, encounter,
combat, feature, knowledge, item, save, and renderer-boundary primitives.

Project-local extension tickets were added without changing the source
specification:

- TEL-100–TEL-103: common character creation plus ROLLED,
  POINT_ALLOCATION, and DAILY_SEED modes.
- TEL-104: initial player setup and deterministic world-seed selection.
- TEL-105: treasure acquisition/loot resolution into unsecured expedition
  state.
- TEL-106: Legacy knowledge handoff between dead and new characters.
- TEL-107: deterministic Core Alpha vertical-slice integration evidence.
- TEL-108: §43–§44 deterministic developer debug command surface.

The §48 floor/monster/item/spell/biome counts are intentionally not a
content-expansion ticket. TEL-107 is an integration/evidence task using
fixtures. Monster appearance and feature/monster journal ownership remain with
the existing TEL-030/TEL-031 and TEL-051/TEL-054/TEL-055 tickets.

This review changed documentation only. TEL-100 was subsequently implemented
and verified; TEL-101–TEL-108 remain Not started.

## TEL-040 verification

- Status: implemented and verified; generic dungeon feature definitions remain
  in `Telengard.Content`, while runtime instances, activation validation, and
  committed discovery/activation events remain in `Telengard.Core`.
- Tests added: feature schema field/copy/validation coverage; active/living/
  non-combat/current-position/known-feature activation boundaries; first versus
  repeat activation event behavior; deterministic replay; and explicit save
  round-trip coverage.
- Files/modules affected: `src/Telengard.Content/Features/FeatureDefinition.cs`,
  `src/Telengard.Core/world/features/FeatureSystem.cs`,
  `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`, and
  `tests/Telengard.Architecture.Tests/FeatureSystemTests.cs`.
- New public APIs: `FeatureType`, `FeatureDefinition`, `FeatureOutcome`,
  `FeatureInstance`, `ActivateFeatureCommand`, and
  `FeatureActivationResolver`.
- New events: `FeatureDiscoveredEvent` on first activation and
  `FeatureActivatedEvent` after each committed activation; payloads contain
  only the opaque instance id, observed position, and activation count.
- Save-schema impact: current save version advanced from 6 to 7. Dungeon
  feature instances and activation/discovery state use explicit DTOs; saves
  through version 6 migrate with an empty feature collection. Simulation,
  generator, and content versions are unchanged.
- Known follow-up work: weighted outcome selection (TEL-041), fountain,
  altar, pit, teleporter effects, feature knowledge, and tuning/formula
  decisions remain out of scope. No next TEL ticket was started.
- Invariants: activation validates before mutation; runtime state does not
  contain content outcome tables; no uncontrolled randomness, renderer
  authority, or unobserved outcome payload was added.
- Acceptance: full Release verification passed with restore, format
  verification, zero-warning Release build, and 172 Release tests.

## TEL-041 verification

- Status: implemented and verified; eligible feature outcomes are selected by
  positive integer weights after exact opaque-condition matching.
- Tests added: condition filtering, zero-weight exclusion, missing-outcome
  validation, stable feature/position/activation replay, selection-context
  validation/copying, and deterministic long-range RNG coverage.
- Files/modules affected:
  `src/Telengard.Content/Features/FeatureOutcomeEngine.cs`,
  `src/Telengard.Core/Rng/DeterministicRng.cs`,
  `tests/Telengard.Architecture.Tests/FeatureOutcomeEngineTests.cs`, and
  `tests/Telengard.Architecture.Tests/DeterministicRngTests.cs`.
- New public APIs: `FeatureOutcomeSelectionContext`,
  `FeatureOutcomeEngine`, and `DeterministicRngStream.NextLong`.
- New events: none; selected outcomes remain simulation-internal and are not
  exposed through the existing feature activation events.
- Save-schema impact: none; no authoritative runtime state was added.
- Known follow-up work: fountain, altar, pit, teleporter effects, knowledge
  observations, and condition/formula tuning remain later scope. Condition
  strings are intentionally opaque tags until those systems define their
  semantics. No next TEL ticket was started.
- Invariants: selection is deterministic for stable seed/content-version/
  feature inputs, uses scoped RNG, filters before mutation/integration, keeps content
  definitions separate from runtime state, and does not expose hidden outcome
  probabilities or effects through events.
- Acceptance: focused Release tests (11 passed), formatter verification, and
  `./eng/verify.ps1 -Mode Full` passed restore, format, zero-warning Release
  build, and 177 Release tests.

## TEL-042 verification

- Status: implemented; fountain outcomes now resolve through the existing
  feature command boundary after deterministic weighted selection.
- Tests added: five headless tests covering spell-power restoration, poison
  cleansing, blindness, the explicitly unresolved unknown transformation,
  condition-aware deterministic replay, invalid definitions/effects, and
  save round-trip behavior.
- Files/modules affected: `src/Telengard.Core/world/features/FeatureSystem.cs`,
  `src/Telengard.Content/Features/Fountain.cs`, and
  `tests/Telengard.Architecture.Tests/FountainTests.cs`.
- New public APIs: `FeatureOutcomeResolution`, `FountainEffectIds`,
  `FountainOutcomeResolvedEvent`, and `FountainResolver`.
- New events: `FountainOutcomeResolvedEvent`, emitted after the existing
  discovery and activation events, with selected effects and observations but
  no outcome weights.
- Save-schema impact: none. Fountain effects use existing player spell-power,
  temporary-effect, and feature activation fields; save, simulation,
  generator, and content versions remain unchanged.
- Design choices: content supplies the fountain definition and opaque
  condition context; the simulation applies only the four specified effect
  identifiers. Unknown transformation is retained as an observed no-op until
  its mechanics are defined (`CONFIGURATION/TUNING DECISION REQUIRED`). No
  default fountain weights were invented.
- Known follow-up work: feature-specific condition semantics and the unknown
  transformation's mechanics remain undefined; no next TEL ticket was
  started.
- Invariants: validation remains in Core before mutation; outcome selection
  uses the existing scoped deterministic RNG; no renderer authority or raw
  probability disclosure was added.
- Acceptance: focused FountainTests (5 passed), full Release tests (183
  passed), formatter verification, and `./eng/verify.ps1 -Mode Full` passed
  restore, format, zero-warning Release build, and Release tests.

## TEL-043 verification

- Status: implemented and verified; altar outcomes now resolve through the
  existing feature command boundary after deterministic weighted selection.
- Tests added: four headless tests covering committed outcome/event ordering,
  opaque condition-aware deterministic replay, validation before mutation, and
  explicit save round-trip behavior.
- Files/modules affected:
  `src/Telengard.Core/world/features/FeatureSystem.cs`,
  `src/Telengard.Content/Features/Altar.cs`,
  `tests/Telengard.Architecture.Tests/AltarTests.cs`, this status document,
  and the task ledger.
- New public APIs: `AltarResolver` and `AltarOutcomeResolvedEvent`.
- New events: `AltarOutcomeResolvedEvent`, emitted after the existing
  discovery and activation events with selected effect and observation tags;
  outcome weights are not exposed.
- Save-schema impact: none. Altar activation uses the existing persisted
  feature activation state; save, simulation, generator, and content versions
  remain unchanged.
- Design choices: altar effect and condition semantics are intentionally kept
  opaque because the specification defines no canonical altar mechanics,
  formula, or balance. The resolver commits activation and reports configured
  observed outcome tags without inventing state-changing effects
  (`CONFIGURATION/TUNING DECISION REQUIRED`).
- Known follow-up work: altar effect semantics and feature knowledge remain
  later decisions/tickets. No next TEL ticket was started.
- Invariants: validation remains in the simulation; outcome selection uses the
  existing scoped deterministic RNG; no renderer authority, hidden weights, or
  save-schema drift was added.
- Acceptance: focused AltarTests (4 passed), formatter verification, zero-warning
  Release build, and `./eng/verify.ps1 -Mode Full` passed restore, format,
  build, and all 187 Release tests.

## TEL-044 verification

- Status: implemented and verified; pit outcomes now resolve through the
  existing feature command boundary and the configured `drop_two_floors`
  effect moves the player two floors while updating expedition floor history.
- Tests added: five headless tests covering the committed drop and event
  ordering, condition-aware deterministic replay, validation before mutation,
  invalid destination boundaries, and the existing explicit save round trip.
- Files/modules affected:
  `src/Telengard.Core/world/features/FeatureSystem.cs`,
  `src/Telengard.Content/Features/Pit.cs`,
  `tests/Telengard.Architecture.Tests/PitTests.cs`, and this status document.
- New public APIs: `PitEffectIds`, `PitOutcomeResolvedEvent`,
  `FeatureActivationResolver.ActivatePit`, and `PitResolver`.
- New events: `PitOutcomeResolvedEvent`, emitted after the existing discovery
  and activation events with the configured effects and observed tags.
- Save-schema impact: none. Pit movement uses the existing player position,
  expedition floor history, and feature activation fields; save, simulation,
  generator, and content versions remain unchanged.
- Design choices: the specification explicitly defines a pit dropping the
  player two floors, so that effect is implemented. Other trap effects,
  destination geometry rules, and balance/formula policy remain
  `CONFIGURATION/TUNING DECISION REQUIRED`; drops beyond floor 50 are rejected
  before mutation.
- Known follow-up work: other trap/pit effects, feature knowledge, and later
  teleporter behavior remain out of scope. No next TEL ticket was started.
- Invariants: validation remains in the simulation; outcome selection uses the
  existing scoped deterministic RNG; committed state and events are replayable;
  no renderer authority, hidden outcome weights, or save-schema drift was added.
- Acceptance: focused PitTests (5 passed), formatter verification, zero-warning
  Release build, and `./eng/verify.ps1 -Mode Full` passed restore, format,
  build, and all 192 Release tests.

## TEL-045 verification

- Status: implemented and verified; teleporter outcomes now resolve through
  the existing feature command boundary and move the player to a configured
  destination while updating expedition floor tracking.
- Tests added: four headless tests covering committed relocation and event
  ordering, condition-aware deterministic replay, validation before mutation,
  and the existing explicit save round trip.
- Files/modules affected:
  `src/Telengard.Core/world/features/FeatureSystem.cs`,
  `src/Telengard.Content/Features/Teleporter.cs`,
  `tests/Telengard.Architecture.Tests/TeleporterTests.cs`, and this status
  document/task ledger.
- New public APIs: `TeleporterResolver`,
  `FeatureActivationResolver.ActivateTeleporter`, and
  `TeleporterOutcomeResolvedEvent`.
- New events: `TeleporterOutcomeResolvedEvent`, emitted after the existing
  discovery and activation events with source/destination positions and
  configured effect/observation tags.
- Save-schema impact: none. Teleporter relocation uses the existing player
  position, expedition floor history, and feature activation fields; save,
  simulation, generator, and content versions remain unchanged.
- Design choices: destination is caller-supplied configuration because the
  specification defines teleporter nodes and destination rules but does not
  define a canonical network or mapping algorithm. Outcome tags remain
  content-defined until feature knowledge and mapping work establish their
  semantics (`CONFIGURATION/TUNING DECISION REQUIRED`).
- Known follow-up work: teleporter mapping/knowledge remains TEL-056 and
  later knowledge tickets. No next TEL ticket was started.
- Invariants: validation remains in the simulation; weighted selection uses
  the existing scoped deterministic RNG; committed state and events replay;
  no renderer authority, hidden destination data, or save-schema drift was
  added.
- Acceptance: focused TeleporterTests (4 passed), full Release tests (196
  passed), formatter verification, zero-warning Release build, and
  `./eng/verify.ps1 -Mode Full` all passed. The verification script required a
  process-scoped PowerShell execution-policy bypass because the host policy
  rejected unsigned local scripts.

## Phase 2 acceptance review

- Result: **FAIL**; 101 Release tests passed. The successful loop is implemented,
  but failure/death resolution is not.
- Verified: underground acquisition changes carried gold only; validated return
  transfers the exact carried amount to secured gold and finishes the
  expedition; a subsequent expedition can start; suspension and active-state
  save/load preserve carried wealth without securing it.
- Missing: an executable failed-expedition/death transition with its
  mode-specific carried-wealth policy, plus runtime update producers for the
  remaining expedition statistics.
- Scope confirmation: no encounter implementation was started.

## TEL-050 verification

- Status: implemented and verified; the renderer-independent simulation now
  has an immutable player-observed knowledge-entry model and a unique
  subject-keyed knowledge collection.
- Tests added: model validation/copy-boundary tests, explicit knowledge save
  round-trip coverage, and version-7 migration coverage for an empty journal.
- Files/modules affected: `src/Telengard.Core/knowledge/KnowledgeEntry.cs`,
  `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/KnowledgeEntryTests.cs`, and this
  status/task documentation.
- New public APIs: `KnowledgeEntry`, `KnowledgeState.Entries`,
  `KnowledgeStateDto`, and `KnowledgeEntryDto`.
- New events: none; observation ingestion remains TEL-051.
- Save-schema impact: current save version advanced from 7 to 8. Explicit
  knowledge DTOs persist subject IDs, observed tags, sample counts,
  hypotheses, confidence, and confirmed facts. Version 1–7 saves migrate to
  an empty knowledge collection; simulation, generator, and content versions
  are unchanged.
- Design choices: confidence is stored as a 0–100 observation, but no
  confidence progression formula or threshold was introduced. Entry string
  collections are copied and require unique, nonblank values; repeated sample
  behavior remains TEL-052.
- Known follow-up work: sample-count updates, confidence progression,
  monster/feature knowledge, teleporter mapping, and all later TEL tickets
  remain out of scope. No next TEL ticket was started.
- Invariants: knowledge stores only player-observed opaque values; hidden
  definitions are not exposed, Core remains renderer-independent, no command,
  event, or RNG behavior changed, and explicit DTO/migration handling
  preserves save compatibility.
- Acceptance: focused knowledge tests (4 passed), full Release tests (202
  passed), formatter verification, zero-warning Release build, and
  `./eng/verify.ps1 -Mode Full` passed. The verification script required a
  process-scoped PowerShell execution-policy bypass because the host policy
  rejected unsigned local scripts.

## TEL-051 verification

- Status: implemented and verified; the renderer-independent simulation now
  accepts opaque player observations through a validated command boundary,
  merges new observations into the journal, and emits a committed observation
  event.
- Tests added: new-entry and event ordering, merge preservation of sample/
  confidence/fact fields, duplicate idempotence, validation and deterministic
  replay, and explicit save round-trip coverage.
- Files/modules affected: `src/Telengard.Core/knowledge/KnowledgeObservation.cs`,
  `tests/Telengard.Architecture.Tests/KnowledgeObservationTests.cs`, this
  status document, and the task ledger.
- New public APIs: `AddKnowledgeObservationCommand`,
  `KnowledgeObservationResolver.Add`, and `KnowledgeObservationAddedEvent`.
- New events: `KnowledgeObservationAddedEvent`, emitted only after a new
  opaque observation is committed; duplicate observations produce no event.
- Save-schema impact: none. TEL-051 uses the existing explicit knowledge DTO
  and save version 8; no migration or version-field change was required.
- Known follow-up work: sample counts, confidence progression, and
  feature/monster/teleporter-specific subject mapping remain later tickets.
  No next TEL ticket was started.
- Non-goals: sample-count updates, confidence formulas, hidden definition
  disclosure, feature-specific knowledge, presentation, and persistence schema
  redesign.
- Invariants: active/living expedition validation occurs before mutation;
  only caller-supplied observed tags are persisted; existing journal fields
  are preserved; no randomness or renderer authority was added.
- Acceptance: focused TEL-051 tests, formatter verification, Release build,
  and full verification completed successfully.

## TEL-052 verification

- Status: implemented and verified; each accepted journal observation now
  records one sample while preserving unique observed tags.
- Tests added: first-sample initialization, repeated-sample counting without
  duplicate tags, committed sample-count event ordering, validation and
  overflow boundaries, deterministic replay, and existing save round-trip
  coverage.
- Files/modules affected: `src/Telengard.Core/knowledge/KnowledgeObservation.cs`,
  `tests/Telengard.Architecture.Tests/KnowledgeObservationTests.cs`, and this
  status/task ledger documentation.
- New public APIs: `KnowledgeSampleCountedEvent`; the existing observation
  command/resolver now owns the sample-count transition.
- New events: `KnowledgeSampleCountedEvent`, emitted after the incremented
  sample count commits; `KnowledgeObservationAddedEvent` remains limited to
  newly observed tags.
- Save-schema impact: none. `KnowledgeEntry.SampleCount` already has an
  explicit save DTO field; save version and simulation, generator, and content
  versions remain unchanged.
- Known follow-up work: confidence progression, monster/feature knowledge,
  teleporter mapping, and all later TEL tickets remain out of scope. No next
  TEL ticket was started.
- Non-goals: confidence formulas, hidden-definition disclosure, presentation,
  persistence redesign, and unrelated gameplay.
- Invariants: active/living validation occurs before mutation; repeated samples
  remain deterministic; only player-supplied observations and counts are
  persisted; no renderer authority or uncontrolled randomness was added.
- Acceptance: focused knowledge tests (10 passed), formatter verification,
  and `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\eng\verify.ps1 -Mode Full` passed restore, format, zero-warning Release
  build, and 207 Release tests.

## TEL-053 verification

- Status: implemented and verified; journal samples now advance the existing
  confidence field through an explicit, configurable threshold policy.
- Tests added: configured threshold progression, custom tuning values,
  unchanged-confidence event suppression, configuration validation, and
  deterministic replay/save round-trip coverage. Existing observation tests
  cover event ordering and updated confidence values.
- Files/modules affected: `src/Telengard.Core/knowledge/KnowledgeConfidence.cs`,
  `src/Telengard.Core/knowledge/KnowledgeObservation.cs`,
  `tests/Telengard.Architecture.Tests/KnowledgeConfidenceTests.cs`,
  `tests/Telengard.Architecture.Tests/KnowledgeObservationTests.cs`,
  `docs/tasks/README.md`, and this status document.
- New public APIs: `KnowledgeConfidenceConfiguration` and
  `KnowledgeConfidenceUpdatedEvent`; `KnowledgeObservationResolver.Add` now
  accepts optional confidence configuration.
- New events: `KnowledgeConfidenceUpdatedEvent`, emitted after a committed
  sample transition only when the resolved confidence changes.
- Save-schema impact: none. Confidence was already persisted through the
  explicit `KnowledgeEntryDto`; save version and simulation, generator, and
  content versions remain unchanged.
- Design choice: the specification supplies sample-count bands but no
  canonical numeric confidence formula. The provisional 25/50/75/100 values
  and sample thresholds are therefore constructor configuration and remain a
  `CONFIGURATION/TUNING DECISION REQUIRED`, not hidden simulation constants.
- Known follow-up work: monster/feature knowledge, teleporter mapping, and all
  later TEL tickets remain out of scope. No next TEL ticket was started.
- Non-goals: hidden-definition disclosure, content catalogs, presentation,
  persistence redesign, anti-save-scumming policy, and unrelated gameplay.
- Invariants: active/living expedition validation occurs before mutation;
  confidence uses only the observed sample count and configuration; equal
  inputs reproduce equal state and event types; no randomness or renderer
  authority was added.
- Acceptance: focused knowledge tests (10 passed), formatter verification,
  full Release tests (212 passed), zero-warning Release build, and
  `powershell.exe -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full`
  all passed. The host required a process-scoped execution-policy bypass for
  unsigned local scripts.

## TEL-054 verification

- Status: implemented and verified; monster knowledge now enters the existing journal
  pipeline through a typed simulation command and uses a stable
  `monster:<definition-id>` subject namespace.
- Tests added: monster subject mapping, observation/sample/confidence
  integration, validation, deterministic replay, explicit save round trip,
  and threat assessment using persistent monster knowledge.
- Files/modules affected: `src/Telengard.Core/knowledge/MonsterKnowledge.cs`,
  `src/Telengard.Core/combat/ThreatAssessment.cs`,
  `tests/Telengard.Architecture.Tests/MonsterKnowledgeTests.cs`,
  `docs/tasks/README.md`, and this status document.
- New public APIs: `MonsterKnowledgeSubject`, `AddMonsterKnowledgeCommand`,
  and `MonsterKnowledgeResolver`; threat classification accepts optional
  persistent knowledge through an overload while retaining the existing
  configuration-based API.
- New events: none. Monster knowledge reuses the existing committed
  `KnowledgeObservationAddedEvent`, `KnowledgeSampleCountedEvent`, and
  `KnowledgeConfidenceUpdatedEvent` events.
- Save-schema impact: none. Monster knowledge uses the existing explicit
  knowledge DTO and save version 8; simulation, generator, and content
  versions are unchanged.
- Known follow-up work: feature knowledge, teleporter mapping, content and
  balance policy, and all later TEL tickets remain out of scope. Subject
  observations remain caller-supplied opaque facts; no automatic hidden
  monster traits or permanent formula were introduced. No next TEL ticket
  was started.
- Invariants: active/living validation remains inside the simulation;
  knowledge stores only opaque observations and a namespaced subject ID;
  threat events expose only an approximate category; no randomness,
  renderer authority, or hidden monster definition data was added.
- Acceptance: focused MonsterKnowledgeTests (4 passed), formatter
  verification, zero-warning Release build, and
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\eng\verify.ps1 -Mode Full` passed with 216 Release tests. The host
  required a process-scoped execution-policy bypass for unsigned local
  scripts.

## TEL-055 verification

- Status: implemented and verified; feature knowledge now enters the existing journal
  pipeline through a typed simulation command and uses a stable
  `feature:<definition-id>` subject namespace.
- Tests added: `FeatureKnowledgeTests` covers subject mapping, observation,
  sample/confidence integration, validation, deterministic replay, and the
  existing explicit save round trip.
- Files/modules affected: `src/Telengard.Core/knowledge/FeatureKnowledge.cs`,
  `tests/Telengard.Architecture.Tests/FeatureKnowledgeTests.cs`, this status
  document, and the task ledger.
- New public APIs: `FeatureKnowledgeSubject`, `AddFeatureKnowledgeCommand`,
  and `FeatureKnowledgeResolver`.
- New events: none. Feature knowledge reuses the committed
  `KnowledgeObservationAddedEvent`, `KnowledgeSampleCountedEvent`, and
  `KnowledgeConfidenceUpdatedEvent` events.
- Save-schema impact: none. Feature knowledge uses the existing explicit
  knowledge DTO and save version 8; simulation, generator, and content
  versions are unchanged.
- Known follow-up work: teleporter mapping, content and balance policy, and
  all later TEL tickets remain out of scope. Observations remain caller-
  supplied opaque facts; no hidden feature definition data or permanent
  formula was introduced. No next TEL ticket was started.
- Invariants: active/living validation remains inside the simulation;
  knowledge stores only opaque observations and a namespaced subject ID; no
  randomness, renderer authority, or hidden-information disclosure was added.
- Acceptance: focused `FeatureKnowledgeTests` (3 passed), formatter
  verification, zero-warning Release build, and full verification (219
  Release tests) passed using the repository local SDK and the process-scoped
  execution-policy bypass required by this host.

## TEL-056 verification

- Status: implemented and verified; configured teleporter nodes now record
  observed source/destination relationships in simulation-owned knowledge and
  promote a repeated identical relationship from `Observed` to `Mapped`.
- Tests added: `TeleporterTests` covers mapping status progression, committed
  event payload/order, active/living validation, deterministic save round trip,
  and version-8 migration with an empty mapping collection.
- Files/modules affected: `src/Telengard.Core/knowledge/TeleporterMapping.cs`,
  `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Content/Features/Teleporter.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/TeleporterTests.cs`, this status
  document, the task ledger, and the TEL-056 ExecPlan.
- New public APIs: `TeleporterNode`, `TeleporterMappingStatus`,
  `TeleporterMapping`, `KnowledgeState.TeleporterMappings`,
  `AddTeleporterMappingCommand`, `TeleporterMappingResolver`,
  `TeleporterMappingResolver.GetStatus`, `TeleporterMappingObservedEvent`,
  `TeleporterMappingDto`, and the node-aware `TeleporterResolver.Resolve`
  overload.
- New events: `TeleporterMappingObservedEvent`, emitted after the committed
  teleporter relocation and carrying only the observed relationship and its
  current status.
- Save-schema impact: current save version advanced from 8 to 9. Structured
  teleporter mappings use explicit DTOs; version-8 and earlier saves migrate
  with no mappings. Simulation, generator, and content versions remain
  unchanged.
- Design choices: each network/node/source/destination relationship is stored
  independently; the first qualifying observation is `Observed` and the next
  identical observation confirms it as `Mapped`. Destination rules and network
  generation remain caller/content configuration because the specification does
  not define their mechanics (`CONFIGURATION/TUNING DECISION REQUIRED`).
- Known follow-up work: destination-rule semantics, generated teleporter
  networks, richer journal hypotheses, and all later TEL tickets remain out of
  scope. No next TEL ticket was started.
- Invariants: commands validate active/living state before mutation; content
  metadata remains separate from runtime/save state; only observed positions and
  relationships are retained; no randomness, renderer authority, hidden
  destination rule, or wealth behavior was added.
- Acceptance: focused TeleporterTests (7 passed), formatter verification,
  zero-warning Release build, and `./eng/verify.ps1 -Mode Full` passed with 222
  Release tests using the repository-local SDK and the process-scoped
  execution-policy bypass required by this host.

## TEL-060 verification

- Status: implemented and verified; item templates now exist as immutable,
  validated content definitions separate from runtime item instances.
- Tests added: `ItemSchemaTests` covers all specification fields, invalid
  identity/rule/pool values, defensive copies, and the absence of runtime state.
- Files/modules affected: `src/Telengard.Content/Items/ItemDefinition.cs`,
  `tests/Telengard.Architecture.Tests/ItemSchemaTests.cs`, this status
  document, and the task ledger.
- New public APIs: `ItemDefinition`, `ItemProperties`, `ItemRarityRules`, and
  `ItemDepthRules`.
- New events: none; this definition-only slice has no state transition.
- Save-schema impact: none. Definitions are content metadata; save version,
  simulation version, generator version, and content version are unchanged.
- Design choices: category and all rule/property values remain caller/content
  supplied strings because the specification does not define canonical item
  categories, formulas, rarity, depth, affix, or curse mechanics. Those choices
  remain `CONFIGURATION/TUNING DECISION REQUIRED`.
- Known follow-up work: item instances, unidentified behavior, affix/curses,
  equipment slots, catalogs, and all later TEL tickets remain out of scope.
  No next TEL ticket was started.
- Invariants: content definitions are immutable and defensive-copied; no
  runtime state, renderer authority, randomness, hidden-information exposure,
  wealth behavior, or knowledge mutation was added.
- Acceptance: focused `ItemSchemaTests`, formatter verification, zero-warning
  Release build, and `./eng/verify.ps1 -Mode Full` passed with 225 Release
  tests using the repository-local SDK and the process-scoped execution-policy
  bypass required by this host.

## TEL-061 verification

- Status: implemented; item instances now exist as validated runtime state
  separate from content definitions.
- Tests added: `ItemInstanceTests` covers all specification fields, invalid
  identity/durability/affix boundaries, defensive collection copying,
  optional-curse normalization, and runtime-state serialization.
- Files/modules affected: `src/Telengard.Core/items/ItemInstance.cs`,
  `tests/Telengard.Architecture.Tests/ItemInstanceTests.cs`, and this status
  document.
- New public APIs: `ItemInstance`.
- New events: none; this schema-only slice does not add an authoritative
  transition.
- Save-schema impact: none. Item instances are not yet attached to persisted
  player or expedition state; save version and simulation, generator, and
  content versions remain unchanged.
- Design choices: `IdentifiedState` is stored as runtime state for the later
  unidentified-item slice; no identification transition, affix/cursing rule,
  durability formula, maximum, or content catalog was invented. A negative
  durability is rejected, while its supplied nonnegative value remains caller
  or content configuration (`CONFIGURATION/TUNING DECISION REQUIRED`).
- Known follow-up work: unidentified behavior, affixes, curses, equipment
  slots, item acquisition, persistence integration, and all later TEL tickets
  remain out of scope. No next TEL ticket was started.
- Invariants: content definitions remain separate from runtime instances;
  collections are defensively copied; no renderer authority, randomness,
  hidden-information disclosure, wealth behavior, or knowledge mutation was
  added.
- Acceptance: focused `ItemInstanceTests` (4 passed), formatter verification,
  zero-warning Release build, full Release tests (229 passed), and
  `./eng/verify.ps1 -Mode Full` all passed with the repository-local SDK and
  process-scoped execution-policy bypass.

## TEL-062 verification

- Status: implemented and verified; unidentified item instances now have an
  immutable identification transition through a validated Core command and
  committed opaque event result.
- Tests added: `ItemIdentificationTests` covers the transition and preserved
  runtime fields, idempotent re-identification, target/command validation, and
  deterministic replay.
- Files/modules affected: `src/Telengard.Core/items/ItemInstance.cs`,
  `src/Telengard.Core/items/ItemIdentification.cs`,
  `tests/Telengard.Architecture.Tests/ItemIdentificationTests.cs`, this
  status document, and the task ledger.
- New public APIs: `ItemInstance.Identify`, `IdentifyItemCommand`,
  `ItemIdentificationResult`, and `ItemIdentificationResolver.Identify`.
- New events: `ItemIdentifiedEvent`, emitted only when an unidentified item
  transitions to identified state; it carries only the opaque item instance ID.
- Save-schema impact: none. Item instances remain standalone runtime state as
  established by TEL-061; player inventory/acquisition and persistence
  integration remain later work. Save and simulation version fields are
  unchanged.
- Design choices: no identification cost, formula, location gate, content
  catalog, or anti-save-scumming policy was invented because the cited
  specification sections do not define one. Re-identification is an
  event-free idempotent no-op.
- Known follow-up work: item acquisition/inventory integration, persistence,
  display-name presentation, affixes, curses, equipment slots, and all later
  TEL tickets remain out of scope. No next TEL ticket was started.
- Invariants: authoritative mutation remains in Core; the command validates
  before creating the new immutable instance; event payloads do not reveal
  hidden item definitions; no randomness or renderer authority was added.
- Acceptance: focused `ItemIdentificationTests` (4 passed), formatter
  verification, zero-warning Release build, and
  `./eng/verify.ps1 -Mode Full` passed with 233 Release tests using the
  repository-local SDK and process-scoped execution-policy bypass.

## TEL-063 verification

- Status: implemented and verified; content-defined affix pools now produce
  deterministic, non-repeating selections and apply them through an immutable
  Core item transition.
- Tests added: `ItemAffixTests` covers valid generation, validation before
  mutation, opaque event payloads, deterministic replay, without-replacement
  selection, count/pool boundaries, definition matching, and Content-to-Core
  integration; `ItemInstanceTests` covers immutable affix replacement and
  duplicate-affix rejection.
- Files/modules affected: `src/Telengard.Core/items/ItemInstance.cs`,
  `src/Telengard.Core/items/ItemAffixes.cs`,
  `src/Telengard.Content/Items/ItemAffixEngine.cs`,
  `tests/Telengard.Architecture.Tests/ItemInstanceTests.cs`,
  `tests/Telengard.Architecture.Tests/ItemAffixTests.cs`, this status
  document, and the task ledger.
- New public APIs: `GenerateItemAffixesCommand`,
  `ItemAffixesGeneratedEvent`, `ItemAffixGenerationResult`,
  `ItemAffixGenerationResolver`, `ItemInstance.WithGeneratedAffixes`, and
  `ItemAffixEngine.Select`/`Generate`.
- New events: `ItemAffixesGeneratedEvent`, carrying only the item instance ID
  so generated hidden affixes are not disclosed by the event contract.
- Save-schema impact: none. Item instances remain standalone runtime state;
  save version and simulation, generator, and content versions are unchanged.
- Design choices: affix count is caller-supplied because the specification
  defines no canonical rarity or affix-count formula. Selection uses the
  ordered content pool without replacement and a scoped deterministic stream
  derived from world seed, content version, definition ID, and item instance
  ID. No balance rule or affix catalog was invented.
- Known follow-up work: curses, equipment slots, item inventory/acquisition,
  persistence integration, and all later TEL tickets remain out of scope. No
  next TEL ticket was started.
- Invariants: content definitions remain separate from runtime state; command
  inputs validate before the immutable transition; equal inputs reproduce
  equal item state and events; no renderer authority, hidden-information
  disclosure, uncontrolled randomness, wealth behavior, or knowledge mutation
  was added.
- Acceptance: focused TEL-063 tests (7 passed), formatter verification,
  zero-warning Release build, and `./eng/verify.ps1 -Mode Full` passed with
  241 Release tests using the repository-local SDK and process-scoped
  execution-policy bypass.

## TEL-064 verification

- Status: implemented and verified; curses now apply through an immutable Core
  item transition and content-defined curse pools select deterministically.
- Tests added: `ItemCurseTests` covers committed application, validation before
  mutation, opaque event payloads, deterministic replay, content-pool
  selection, definition matching, and empty-pool/identifier boundaries;
  `ItemInstanceTests` covers immutable curse replacement and preservation of
  other runtime fields.
- Files/modules affected: `src/Telengard.Core/items/ItemInstance.cs`,
  `src/Telengard.Core/items/ItemCurses.cs`,
  `src/Telengard.Content/Items/ItemCurseEngine.cs`,
  `tests/Telengard.Architecture.Tests/ItemInstanceTests.cs`,
  `tests/Telengard.Architecture.Tests/ItemCurseTests.cs`, this status
  document, and the task ledger.
- New public APIs: `ItemInstance.WithCurse`, `ApplyItemCurseCommand`,
  `ItemCursedEvent`, `ItemCurseResult`, `ItemCurseResolver`, and
  `ItemCurseEngine.Select`/`Generate`.
- New events: `ItemCursedEvent`, carrying only the opaque item instance ID so
  the curse value remains outside the event contract.
- Save-schema impact: none. Item instances remain standalone runtime state;
  save version and simulation, generator, and content versions are unchanged.
- Design choices: one caller/content-supplied curse is applied to the existing
  singular runtime curse field. The ordered content pool and item-scoped
  deterministic stream provide selection; no curse effect, probability,
  stacking, cleansing, balance, or catalog rule was invented.
- Known follow-up work: equipment slots, item inventory/acquisition,
  persistence integration, and later TEL tickets remain out of scope. No next
  TEL ticket was started.
- Invariants: content definitions remain separate from runtime state; command
  inputs validate before the immutable transition; equal inputs reproduce
  equal item state and events; no renderer authority, hidden-information
  disclosure, uncontrolled randomness, wealth behavior, or knowledge mutation
  was added.
- Acceptance: focused TEL-064 tests (7 passed), formatter verification,
  `./eng/verify.ps1 -Mode Full`, zero-warning Release build, and full Release
  tests (249 passed) all succeeded with the repository-local SDK and the
  process-scoped execution-policy bypass required by this host.

## TEL-065 verification

- Status: implemented and verified; caller/content-configured equipment slots
  now hold item instance assignments in renderer-independent player state.
- Tests added: `EquipmentTests` covers slot validation and immutable updates,
  duplicate slot/item invariants, equip/unequip command validation and event
  ordering, deterministic replay, save round trips, and legacy slot migration.
- Files/modules affected: `src/Telengard.Core/items/Equipment.cs`,
  `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Dto/EquipmentSlotDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/EquipmentTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`, this
  status document, and the task ledger.
- New public APIs: `EquipmentSlotState`, `EquipItemCommand`,
  `UnequipItemCommand`, `EquipmentResult`, and `EquipmentResolver`.
- New events: `ItemEquippedEvent` and `ItemUnequippedEvent`, emitted only after
  the corresponding slot assignment commits and carrying the slot ID and item
  instance ID.
- Save-schema impact: current save version advanced from 9 to 10. Explicit
  equipment slot DTOs persist slot IDs and optional item instance IDs. Version
  9 and earlier saves migrate through the compatibility reader; legacy string
  slot entries become empty configured slots. Simulation, generator, and
  content versions are unchanged.
- Design choices: slot IDs are caller/content supplied because the
  specification defines no canonical slot catalog. Equipping requires a
  configured empty slot and an item not already equipped; replacement,
  inventory ownership, item acquisition, effects, and balance rules remain
  out of scope (`CONFIGURATION/TUNING DECISION REQUIRED` where applicable).
- Known follow-up work: inventory/acquisition integration and item effects
  remain later work. No next TEL ticket was started.
- Invariants: simulation validates before mutation; runtime slots reference
  item instance IDs without embedding content definitions; equal inputs replay
  equally; no randomness, renderer authority, or hidden-information disclosure
  was added.
- Acceptance: focused `EquipmentTests` (6 passed), full Release tests (255
  passed), formatter verification, zero-warning Release build, and
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
  -Mode Full` passed.

## TEL-070 verification

- Status: implemented and verified; caller-supplied experience can now be
  committed through the renderer-independent simulation at the inn.
- Tests added: `ExperienceTests` covers committed state and event payload,
  inn/expedition and dead-player validation, non-positive amounts, negative
  state and overflow boundaries, deterministic replay, and explicit save
  round-trip preservation.
- Files/modules affected: `src/Telengard.Core/progression/Experience.cs`,
  `tests/Telengard.Architecture.Tests/ExperienceTests.cs`, this status
  document, and the task ledger.
- New public APIs: `AwardExperienceCommand`, `ExperienceAwardedEvent`, and
  `ExperienceResolver.Award`.
- New events: `ExperienceAwardedEvent`, emitted after the authoritative
  experience total commits.
- Save-schema impact: none. `PlayerState.Experience` was already persisted by
  the existing explicit player DTO; save, simulation, generator, and content
  versions are unchanged.
- Design choices: the command accepts an explicit positive amount because the
  specification defines no XP source or formula. Awards are restricted to a
  living player at the inn after expedition completion, so active-expedition
  progress is not silently secured. No level-up, balance, death-mode, or
  automatic award policy was introduced.
- Known follow-up work: XP sources and mode-specific loss/retention policy
  remain configuration or later gameplay decisions. No next TEL ticket was
  started.
- Invariants: command validation precedes mutation; XP remains authoritative
  in Core; equal inputs reproduce equal state/events; explicit save/load
  preserves the total; no randomness, hidden-information disclosure, or
  presentation authority was added.
- Acceptance: focused XP tests (6 passed), formatter verification, full
  Release tests (272 passed), zero-warning Release build, and
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
  -Mode Full` all passed.

## TEL-071 verification

- Status: implemented and verified; configurable level thresholds now resolve
  one level-up at a time through the renderer-independent simulation at the
  inn.
- Tests added: `LevelTests` covers threshold validation and defensive copying,
  inn/expedition/dead-player and insufficient-XP boundaries, repeated eligible
  level-ups, committed event payload, deterministic replay, and save round
  trip.
- Files/modules affected: `src/Telengard.Core/progression/Levels.cs`,
  `tests/Telengard.Architecture.Tests/LevelTests.cs`, this status document,
  and the task ledger.
- New public APIs: `LevelConfiguration`, `LevelUpCommand`,
  `PlayerLeveledUpEvent`, and `LevelResolver.LevelUp`.
- New events: `PlayerLeveledUpEvent`, emitted after the player's level
  increments and carrying only the previous level, new level, and current
  experience.
- Save-schema impact: none. `PlayerState.Level` and `PlayerState.Experience`
  already use the explicit player save DTO; save version 10 and the
  simulation, generator, and content versions are unchanged.
- Design choice: XP thresholds are ordered caller/content configuration and
  must begin at zero; the explicit inn command commits one eligible level at
  a time. No canonical threshold formula, maximum-level balance, stat growth,
  bulk-leveling behavior, or automatic XP-award policy was invented. These
  remain `CONFIGURATION/TUNING DECISION REQUIRED`.
- Known follow-up work: spell definitions/casting, talent constellations,
  progression balance, and all later TEL tickets remain out of scope. No next
  TEL ticket was started.
- Invariants: level-up validation occurs before mutation and only at the inn;
  the configuration is defensively copied; equal inputs reproduce equal state
  and events; no randomness, hidden-information disclosure, renderer
  authority, or save-schema drift was added.
- Acceptance: focused LevelTests (5 passed), formatter verification, zero-warning
  Release build, and `./eng/verify.ps1 -Mode Full` passed with 277 Release
  tests.

## TEL-072 verification

- Status: implemented and verified; spell definitions now exist as validated
  content metadata separate from simulation state and runtime spell casting.
- Tests added: `SpellSchemaTests` covers all specification fields, invalid
  identity/cost/targeting/tag values, defensive collection copies, optional
  collection defaults, and serialization without runtime or renderer state.
- Files/modules affected: `src/Telengard.Content/Magic/SpellDefinition.cs`,
  `tests/Telengard.Architecture.Tests/SpellSchemaTests.cs`, this status
  document, and the task ledger.
- New public APIs: `SpellDefinition` with the specification-defined identity,
  descriptions, cost, targeting rule, effects, and interactions.
- New events: none; this definition-only slice has no state transition.
- Save-schema impact: none. Spell definitions are content metadata; save,
  simulation, generator, and content versions are unchanged.
- Design choices: cost is a validated nonnegative caller/content value, while
  targeting rules, effects, interactions, and discovered descriptions remain
  content-supplied values because the specification defines no canonical
  formula or catalog. No casting, notebook progression, or balance rule was
  invented.
- Known follow-up work: spell casting, spell notebook/knowledge integration,
  content catalogs, and all later TEL tickets remain out of scope. No next TEL
  ticket was started.
- Invariants: content definitions are separate from authoritative runtime
  state and defensively copied; no commands, events, randomness, renderer
  authority, hidden-information disclosure, or persistence changes were
  introduced.
- Acceptance: focused `SpellSchemaTests` (4 passed), formatter verification,
  zero-warning Release build, and `./eng/verify.ps1 -Mode Full` passed with
  281 Release tests using the repository-local SDK and process-scoped
  execution-policy bypass.

## TEL-073 verification

- Status: implemented and verified; known spells can now be cast through the
  renderer-independent combat resolution boundary.
- Tests added: `SpellCastingTests` covers committed resource/state and event
  ordering, exact and zero-cost boundaries, validation-before-mutation,
  deterministic replay, and explicit save round-trip preservation.
- Files/modules affected: `src/Telengard.Core/magic/SpellCasting.cs`,
  `src/Telengard.Content/Magic/SpellDefinition.cs`,
  `tests/Telengard.Architecture.Tests/SpellCastingTests.cs`, this status
  document, and the task ledger.
- New public APIs: `ISpellDefinition`, `CastSpellCommand`,
  `SpellCastEvent`, and `SpellCastResolver.Resolve`.
- New events: `SpellCastEvent`, followed by the existing
  `CombatPhaseChangedEvent` after spell power and combat phase commit.
- Save-schema impact: none. Spell power and active combat state already use
  the explicit player/combat DTOs; save version 10 and the simulation,
  generator, and content versions are unchanged.
- Design choices: casting requires an active living expedition, a selected
  `CastSpell` combat action, a learned spell, and sufficient configured spell
  power. Effects, targeting consequences, notebook progression, and balance
  formulas remain `CONFIGURATION/TUNING DECISION REQUIRED`; the event exposes
  no hidden effect or resistance data. No next TEL ticket was started.
- Invariants: command validation precedes mutation; the Core interface keeps
  content definitions separate from simulation logic; equal inputs reproduce
  equal state/events and save/load preserves the result; no randomness,
  hidden-information disclosure, or renderer authority was added.
- Acceptance: focused `SpellCastingTests` (5 passed), formatter
  verification, zero-warning Release build, and
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
  -Mode Full` passed with 286 Release tests.

## TEL-074 verification

- Status: implemented and verified; talent constellation definitions now
  exist as validated content metadata separate from authoritative simulation
  state.
- Tests added: `TalentSchemaTests` covers the specification fields, invalid
  identity/cost/tag values, defensive collection copies, optional collection
  defaults, and serialization without runtime or renderer state.
- Files/modules affected: `src/Telengard.Content/Progression/TalentDefinition.cs`,
  `tests/Telengard.Architecture.Tests/TalentSchemaTests.cs`, this status
  document, and the task ledger.
- New public APIs: `TalentDefinition` with the specification-defined ID,
  constellation, prerequisites, effects, and cost fields.
- New events: none; this definition-only slice has no state transition.
- Save-schema impact: none. `PlayerState.Talents` and its explicit save DTO
  already exist; save version 10 and the simulation, generator, and content
  versions are unchanged.
- Design choices: constellation identifiers, prerequisites, effects, and cost
  remain content-supplied values. The specification defines no canonical
  talent currency, purchase formula, unlock timing, or effect resolver, so no
  talent-spending mechanic or balance rule was invented.
- Known follow-up work: talent acquisition/effect resolution and content
  catalogs remain configuration or later gameplay decisions. No next TEL
  ticket was started.
- Invariants: content definitions are separate from authoritative runtime
  state and defensively copied; no commands, events, randomness, renderer
  authority, hidden-information disclosure, or persistence changes were
  introduced.
- Acceptance: focused `TalentSchemaTests` (4 passed), formatter verification,
  zero-warning Release build, and
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
  -Mode Full` passed with 290 Release tests.

## Historical production coverage gate — 2026-08-16

Verified 2026-08-16 with the repository-local pinned SDK 8.0.100, Coverlet
collector 6.0.4 (test-only), and the complete xUnit suite. Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 clean Telengard.sln --configuration Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\coverage.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

`coverage.ps1` recreates `TestResults/coverage` on every run; the clean
checkpoint additionally removes Release build outputs before coverage rebuilds
the solution.

The earlier pre-hardening baseline was 152 passing tests, 1,369/1,383 lines
(98.99%), and 584/616 branches (94.81%). Baseline coverage
by source file was:

| Project | Source file | Lines | Branches |
| --- | --- | ---: | ---: |
| Core | `combat/Attack.cs` | 46/46 | 14/14 |
| Core | `combat/CombatState.cs` | 76/85 | 42/56 |
| Core | `combat/Death.cs` | 27/27 | 6/6 |
| Core | `combat/Defend.cs` | 16/16 | 10/10 |
| Core | `combat/EncounterTrigger.cs` | 57/58 | 24/28 |
| Core | `combat/Flee.cs` | 30/30 | 20/20 |
| Core | `combat/MonsterInstance.cs` | 34/34 | 8/8 |
| Core | `combat/ThreatAssessment.cs` | 42/42 | 16/18 |
| Core | `economy/CarriedGold.cs` | 21/21 | 12/12 |
| Core | `Events/DomainEventBus.cs` | 29/29 | 6/6 |
| Core | `meta/GameSuspension.cs` | 10/10 | 6/6 |
| Core | `Rng/DeterministicRng.cs` | 31/31 | 4/4 |
| Core | `Simulation/AssemblyBoundary.cs` | 0/0 | 0/0 |
| Core | `Simulation/CommandDispatcher.cs` | 25/25 | 16/16 |
| Core | `Simulation/GameState.cs` | 110/110 | 24/24 |
| Core | `world/generation/DungeonWalking.cs` | 110/110 | 47/49 |
| Core | `world/generation/FloorLayout.cs` | 52/52 | 24/24 |
| Core | `world/generation/FloorLayoutGenerator.cs` | 83/83 | 74/74 |
| Core | `world/generation/FloorTransition.cs` | 41/41 | 33/34 |
| Core | `world/visibility/TileVisibility.cs` | 79/79 | 46/46 |
| Content | `Monsters/MonsterDefinition.cs` | 69/71 | 24/26 |
| Save | `SaveGameSerializer.cs` | 15/15 | 2/2 |
| Save | `Dto/GameStateSaveDto.cs` | 246/246 | 12/12 |
| Save | `Migrations/SaveMigrations.cs` | 77/79 | 82/89 |
| Terminal | `Program.cs` | 1/1 | 0/0 |
| TestHarness | `Program.cs` | 9/9 | 16/16 |
| TestHarness | `SimulationTestHarness.cs` | 33/33 | 16/16 |

Final coverage is 1,383/1,383 lines and 616/616 branches (100.00% each), across
166 passing tests. Final per-file results are emitted to
`TestResults/coverage/coverage-summary.md` and
`TestResults/coverage/coverage-summary.json`; every measured in-scope file is
100%. `src/Telengard.Godot` remains outside scope because it is not included
in `Telengard.sln`. No hand-written production code is excluded and no
coverage-suppression attribute is used. The Core assembly-boundary marker has
0/0 coverable lines and branches.

The mutation-hardening pass strengthened the suite without changing production
behavior. Final Complete-level evidence is recorded in
[docs/mutation-hardening-report.md](mutation-hardening-report.md). The final
Complete evidence killed 739/1,046 Core mutants, 27/41 Content mutants, and
122/151 Save mutants; it produced 11 Core timeouts, 60 Core compile-error
results, 132 Core covered-block ignored results, 2 Save compile-error results,
and 15 Save covered-block ignored results. All remaining survivors are
individually classified in the report and generated audits; no hand-written
production code is excluded.

## Mutation-testing baseline

Verified 2026-08-15 with repository-local `dotnet-stryker` 4.14.2, the pinned
SDK/runtime 8.0.100, Standard mutation level, Release configuration, the
single xUnit project, and the pinned SDK's `MSBuild.dll`. Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\mutation.ps1
```

The baseline was run before changing production code or strengthening tests.
It covered every applicable hand-written production project: `Telengard.Core`,
`Telengard.Save`, and `Telengard.Terminal`. `Telengard.Content` has no
hand-written C# source; `Telengard.TestHarness` is tooling and is outside the
production mutation scope. No mutation categories, methods, files, or source
projects were excluded by repository configuration.

| Project | Score | Total | Killed | Survived | No coverage | Timeout | Compile error | Ignored |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Telengard.Core | 70.34% | 667 | 366 | 159 | 0 | 11 | 48 | 83 |
| Telengard.Save | 83.17% | 117 | 84 | 17 | 0 | 0 | 2 | 14 |
| Telengard.Terminal | n/a (0 mutants) | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Raw Stryker JSON plus HTML and Markdown reports are under
`TestResults/mutation/<project>/reports/`. The aggregate machine-readable and
human-readable summary and the per-mutant category audit are
`TestResults/mutation/mutation-summary.json`,
`TestResults/mutation/mutation-summary.md`,
`TestResults/mutation/mutation-audit.json`, and
`TestResults/mutation/mutation-audit.md`. The audit records 135 actionable
test weaknesses, 41 intentionally unobservable exception-message details, 97
Stryker covered-block ignored/equivalent results, 11 timeouts, and 50
compile-error tooling results. No mutation score gate is enforced by the
baseline command.

## TEL tickets

The complete ordered implementation ledger is documented in [docs/tasks/README.md](tasks/README.md). It contains 61 tickets explicitly defined by specification §49; numeric gaps are intentional.

- Completed: TEL-001 — GameState domain model; TEL-002 — deterministic RNG
  service; TEL-003 — command dispatcher; TEL-004 — domain event bus; TEL-005 —
  save/load DTO system; TEL-006 — deterministic simulation test harness;
  TEL-010 — dungeon coordinate types; TEL-011 — procedural floor layout
  generator; TEL-012 — connectivity validation; TEL-013 — tile visibility;
  TEL-014 — fog-of-war map; TEL-015 — stairs and floor transitions; TEL-016 —
  discovered map persistence; and the unnumbered Phase 1 walking boundary.
- Completed: TEL-020 — expedition state tracking at dungeon entry and floor
  transitions.
- Completed: TEL-021 — explicit persisted inn boundary state, dungeon entry /
  return integration, and save migration to schema version 3.
- Completed: TEL-022 — carried-gold acquisition through the simulation command
  boundary, mirrored expedition/player state, and committed event emission.
- Completed: TEL-023 — secured-gold persistence and automatic transfer at the
  validated inn return boundary with committed event emission.
- Completed: TEL-024 — expedition completion at the validated inn return
  boundary with committed success event emission.
- Completed: TEL-025 — active expedition suspension through the simulation
  command/event boundary with deterministic explicit save/load coverage.
- Completed: TEL-030 — monster definition and runtime instance schemas with
  structural validation and no encounter behavior.
- Completed: TEL-031 — deterministic configured encounter evaluation,
  movement integration, and committed encounter-start event emission.
- Completed: TEL-032 — combat phases and action-intent state, encounter
  integration, and explicit active-combat save support.
- Active: None assigned.
- Blocked: None recorded.
- Ledger documents: the specification-defined TEL-001–TEL-006,
  TEL-010–TEL-016, TEL-020–TEL-025, TEL-030–TEL-037, TEL-040–TEL-045,
  TEL-050–TEL-056, TEL-060–TEL-065, TEL-070–TEL-074, TEL-080–TEL-085, and
  TEL-090–TEL-093 series, plus project-local TEL-100–TEL-119 extensions.
- Open project-local work: TEL-101–TEL-117 remains not started in
  the task ledger; TEL-118 is implemented and verified by the documentation
  reconciliation recorded in its task. Depth ecology, broad content counts,
  and other post-alpha systems remain intentionally outside this extension
  set. Encounter probability and spawn options remain explicit configuration
  until their content/balance owners define them.

## TEL-001 verification

- Tests added: GameState creation defaults, mode validation, dungeon-boundary
  validation, and fixed-identity reproducibility.
- Files/modules affected: `Telengard.Core/Simulation/GameState.cs` and the
  architecture test project.
- New public APIs: `GameState`, its state records, `GameMode`, `GameVersions`,
  and `DungeonPosition`.
- New events: none.
- Save-schema impact: no serializer or save schema was added; the model carries
  the version fields required for the later explicit save DTO task.
- Known follow-up work: numeric formulas, content, and persistence are
  configuration/future-ticket decisions.
- Non-goals: commands, dispatch, events, RNG, persistence, content, and
  gameplay mechanics.
- Invariants: creation validates game mode and dungeon floor bounds; state
  carries stable world and generator version metadata; presentation has no
  dependency on the model implementation.
- Determinism: creation has no random calls; a fixed seed and player identity
  produce equal state. Default identity is stable until character creation is
  specified.
- Acceptance: the model and focused tests are authored and verified.

## TEL-002 verification

- Status: implemented and verified with the locally provisioned .NET 8 SDK.
- Tests added: deterministic same-input sequence, scoped stream separation,
  bounded integer range, and invalid-input validation.
- Files/modules affected: `Telengard.Core/Rng/DeterministicRng.cs`,
  `Telengard.Core/Rng/README.md`, and the architecture test project.
- New public APIs: `DeterministicRng`, `DeterministicRngStream`,
  `CreateStream`, `NextUInt`, `NextInt`, and `NextDouble`.
- New events: none.
- Save-schema impact: none; the service is derived from existing world seed
  and generator-version inputs and does not add authoritative state.
- Known follow-up work: integrate named streams into later simulation systems;
  no balance or gameplay formula was chosen here.
- Non-goals: commands, events, persistence, content, gameplay, and renderer
  integration.
- Invariants: stream values are derived from stable seed, generator version,
  name, and scope; bounded draws are range-safe; core has no presentation
  dependency.
- Determinism: SHA-256 derives a stable stream seed and SplitMix64 advances
  each stream without a shared global RNG.
- Acceptance: implementation and focused tests are authored and verified.

## AUD-002 remediation

- Status: remediated and verified against audit baseline `34c7a4d00d8b7588869875f34edee1c1adfcdeaf`; current implementation was inspected at `ba18040778dc7128a0ad443b8c6db242a05a36ca`.
- Defect reproduced by aliasing and mutable-snapshot tests in `GameStateTests`, `ItemInstanceTests`, `MonsterSchemaTests`, and `FloorLayoutGeneratorTests`; the old implementation retained caller-owned player/expedition lists and exposed mutable array/list instances through read-only interfaces.
- Production change: authoritative player and expedition collections now copy inputs, reject null elements, and expose read-only snapshots. Persistent map, floor layout rooms/tiles, item affixes, and monster effects now retain defensive/read-only snapshots at their boundaries.
- Compatibility impact: save version unchanged; simulation, generator, and content versions unchanged; deterministic vectors unchanged; no migration required; replay compatibility preserved because ordering and values are unchanged and only aliasing/mutation behavior is corrected.
- Focused verification: the pre-fix regression run failed 5 tests; the post-fix focused suite passed 34 tests. Formatter verification passed.
- Coverage gate: not required by AUD-002; no coverage policy or production behavior requiring a coverage artifact changed.
- Scope confirmation: no other AUD packet was started.

## AUD-007 remediation

- Status: remediated against audit baseline `34c7a4d00d8b7588869875f34edee1c1adfcdeaf`; current implementation was inspected at `ebec46da09b2994d784e65aace57693f3fcb7002`.
- Defect reproduced by save serializer regressions: a null persistent-map position escaped validation as `NullReferenceException`, while negative counters, active-at-inn state, carried-gold mismatch, and combat without an active expedition were admitted as authoritative state.
- Production change: save validation now runs after migration in structural, scalar-domain, and cross-field stages; nested position/collection contracts, resolver-backed ranges, carried-gold mirrors, expedition/inn lifecycle, floor/deepest coherence, and combat lifecycle are rejected before DTO materialization. Serializer materialization failures are exposed as `SaveFormatException`.
- Compatibility impact: save version unchanged; simulation, generator, and content versions unchanged; deterministic vectors unchanged; no migration added; valid-save replay compatibility preserved. Supported save versions 1–13 continue to migrate and load successfully.
- Tests added/updated: malformed current-save coverage for null positions, negative gold/counters, active-at-inn, carried-gold mismatch, invalid combat lifecycle, and all supported save-version migrations; active round-trip fixtures now represent valid dungeon lifecycle state.
- Verification: focused save tests passed 40/40; full Release architecture tests passed 334/334; doctor, formatter verification, zero-warning Release build, and `./eng/verify.ps1 -Mode Full` passed with a process-scoped execution-policy bypass and `core.autocrlf=false` override for the host’s unsigned-script and dirty-worktree line-ending behavior.
- Coverage gate: not required by AUD-007; no coverage policy or coverage artifact behavior changed.
- Scope confirmation: no other AUD packet was started.

## TEL-003 verification

- Status: implemented and verified with the portable .NET 8.0.100 SDK.
- Tests added: typed dispatch commits the returned state and events; unregistered
  commands leave state unchanged; handler failures do not commit; duplicate
  handler registration is rejected.
- Files/modules affected: `Telengard.Core/Simulation/CommandDispatcher.cs` and
  `tests/Telengard.Architecture.Tests/CommandDispatcherTests.cs`.
- New public APIs: `ICommand`, `IDomainEvent`, `CommandResult`,
  `CommandDispatcher`, `Register`, `Dispatch`, and `CurrentState`.
- New events: none; handlers return committed `IDomainEvent` values for the
  later event-bus slice.
- Save-schema impact: none; the dispatcher changes runtime state references but
  adds no persisted fields or migrations.
- Known follow-up work: TEL-004 may add event publication/consumption; no event
  bus or gameplay command was implemented here.
- Non-goals: movement, combat, persistence, presentation integration, event
  bus, and any subsequent TEL ticket.
- Invariants: handlers receive authoritative state and produce the candidate
  next state; state commits only after successful handler return; unregistered
  or failing commands do not mutate dispatcher state; core remains renderer
  independent.
- Determinism: the dispatcher performs no random draws and preserves handler
  command order; deterministic behavior remains the responsibility of the
  command handlers.
- Acceptance: implementation and focused tests are authored and verified.

## TEL-004 verification

- Status: implemented and verified.
- Tests added: dispatcher publication after state commit, ordered typed and
  broad subscriptions with unsubscribe, and no publication after handler
  failure.
- Files/modules affected: `Telengard.Core/Events/DomainEventBus.cs`,
  `Telengard.Core/Simulation/CommandDispatcher.cs`, the dispatcher tests, and
  the event-boundary README.
- New public APIs: `DomainEventBus`, `Subscribe`, `Publish`, and the optional
  `CommandDispatcher` event-bus integration through `EventBus`.
- New events: none; TEL-004 provides delivery for existing `IDomainEvent`
  values.
- Save-schema impact: none; the bus is transient infrastructure and adds no
  authoritative state.
- Known follow-up work: concrete gameplay events and consumers belong to later
  TEL tickets.
- Non-goals: gameplay, persistence, content, rendering, and later TEL tickets.
- Invariants: handlers publish only after a command result becomes current;
  failed or unregistered commands publish nothing; core remains renderer
  independent.
- Determinism: publication preserves command event order and performs no random
  draws.
- Acceptance: typed event publication and dispatcher integration are covered by
  headless tests.

## TEL-005 verification

- Status: implemented and verified.
- Tests added: explicit save/load round-trip preservation, unsupported-version
  rejection, invalid-JSON rejection, and deterministic serialization round-trip.
- Files/modules affected: `Telengard.Save/Dto/GameStateSaveDto.cs`,
  `Telengard.Save/Migrations/SaveMigrations.cs`,
  `Telengard.Save/SaveGameSerializer.cs`, and save serializer tests.
- New public APIs: `GameStateSaveDto`, nested save DTOs,
  `SaveGameSerializer`, `SaveMigrations`, and `SaveFormatException`.
- New events: none; save/load is a persistence boundary and does not mutate
  simulation state or emit domain events.
- Save-schema impact: explicit save schema version 1 now serializes the current
  `GameState` fields and preserves save, simulation, generator, and content
  versions. Unsupported versions are rejected at the migration boundary.
- Known follow-up work: future state fields require a new DTO version and an
  explicit migration; no anti-save-scumming policy was selected.
- Non-goals: gameplay, renderer integration, content, formulas, and TEL-006 or
  any later ticket.
- Invariants: runtime state is not serialized directly; malformed documents are
  rejected; authoritative fields and version metadata survive a round trip;
  core remains renderer independent.
- Determinism: serialization has no random behavior, and a save round trip
  produces byte-stable JSON for the same state.
- Acceptance: the serializer and DTO boundary are covered by headless tests;
  the full solution test suite, formatter, and Release build pass.

## Scaffold verification

- Configuration added: `Telengard.sln`, `global.json`, `Directory.Build.props`,
  headless .NET projects, and separate Godot presentation placeholder.
- Architecture test added: core has no presentation or Godot assembly
  references.
- Formatter/build: `dotnet build Telengard.sln --configuration Release
  --no-restore`, `dotnet test Telengard.sln --configuration Release
  --no-restore`, and `dotnet format Telengard.sln --verify-no-changes
  --no-restore` pass. The solution-level test command executes all 13 tests.

## Known architectural decisions

- `docs/modern-telengard-spec.md` is authoritative.
- Simulation is renderer-independent and owns authoritative `GameState`.
- Commands are input intents; domain events are committed simulation facts.
- RNG uses deterministic streams rather than an uncontrolled global generator.
- Generator, simulation, content, and save versions are preserved.
- Content definitions remain separate from simulation logic.
- Carried and secured wealth are distinct.
- Player knowledge records observations and does not reveal hidden information merely because it exists internally.
- Modern, Retro+, and Terminal are presentations of one simulation.
- The explicit product non-goals in the specification remain out of scope.

## TEL-011 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: same-seed layout reproduction, room/door/stair presence, anchor
  reachability, and invalid floor/options validation.
- Files/modules affected: `Telengard.Core/world/generation/FloorLayout.cs`,
  `Telengard.Core/world/generation/FloorLayoutGenerator.cs`, and
  `FloorLayoutGeneratorTests.cs`.
- New public APIs: `DungeonTile`, `FloorLayoutOptions`, `DungeonRoom`,
  `FloorLayout`, and `FloorLayoutGenerator.Generate`.
- New events: none; generation is a pure simulation-side resolver.
- Save-schema impact: none; generated layouts are derived from world seed,
  generator version, floor, and tuning options and are not authoritative save
  state in this ticket.
- Known follow-up work: biome, features, encounters, loot, secrets, movement,
  and save persistence remain later tickets; room-generation tuning remains a
  configuration decision.
- Non-goals: presentation integration, gameplay commands, content catalogs,
  and any next TEL ticket.
- Invariants: generated tiles remain within the floor bounds, rooms are
  non-overlapping, stairs are on walkable rooms, and the generated anchors are
  connected by walkable geometry.
- Determinism: layout randomness uses the existing scoped `layout/floor:N`
  deterministic stream and includes the generator version.
- Acceptance: implementation, 27-test solution suite, formatter, Release build,
  and package creation pass.

## TEL-006 verification

- Status: implemented and verified.
- Tests added: scripted command execution through the dispatcher, save/reload
  checkpoint continuity, and same-seed state/event comparison.
- Files/modules affected: `tools/Telengard.TestHarness/`, solution wiring,
  `tools/README.md`, and the architecture test project.
- New public APIs: `SimulationTestHarness.Run`,
  `SimulationTestHarness.AssertDeterministic`, and `SimulationRunResult`.
- New events: none; the harness records events already emitted by command
  handlers and does not invent domain facts.
- Save-schema impact: none; checkpoints use the existing explicit version-1
  save DTO and migration boundary.
- Known follow-up work: future gameplay tickets provide concrete commands and
  handlers; no gameplay was added here.
- Non-goals: gameplay, content, formulas, presentation authority, and later
  TEL tickets.
- Invariants: commands still validate and mutate through `CommandDispatcher`;
  save reload recreates the dispatcher from DTO state; core remains renderer
  independent.
- Determinism: repeated scripts use the same seed, versions, command sequence,
  save checkpoints, final save, and event signatures.
- Acceptance: formatter, 22 headless tests, and Release build pass.

## TEL-010 verification

- Status: implemented and verified.
- Tests added: valid boundary and stable-value checks for `DungeonPosition`;
  existing save round-trip coverage verifies the coordinate DTO mapping.
- Files/modules affected: `Telengard.Core/Simulation/GameState.cs`,
  `Telengard.Save/Dto/GameStateSaveDto.cs`, and the architecture tests.
- New public APIs: `DungeonPosition` and `DungeonPositionDto`; `PlayerState.Position`
  now uses the domain coordinate type.
- New events: none.
- Save-schema impact: no JSON shape or save-version change; the explicit DTO
  continues to persist floor, x, and y and still preserves all version fields.
- Known follow-up work: layout generation, connectivity, visibility, map
  persistence, and floor transitions remain later TEL tickets; no formula or
  balance decision was made.
- Non-goals: movement, generation, rendering, gameplay, and all later TEL
  tickets.
- Invariants: floor coordinates are restricted to 1–50; x/y remain integer
  coordinates; state remains renderer-independent and save loading reconstructs
  the validated domain type.
- Determinism: the value type has no random behavior; stable coordinates remain
  available for later seeded generation and replay.
- Acceptance: focused tests, full solution tests (24 passed), formatter,
  Release build (0 warnings), and Release package creation pass.

## TEL-012 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: deterministic multi-seed validation that every generated
  walkable tile, room anchor, and both stairs belong to one connected region.
- Files/modules affected: `src/Telengard.Core/world/generation/FloorLayoutGenerator.cs`,
  `tests/Telengard.Architecture.Tests/FloorLayoutGeneratorTests.cs`, and this
  status document.
- New public APIs: none.
- New events: none; generation validation emits no simulation facts.
- Save-schema impact: none; connectivity is derived from the generated layout
  and adds no authoritative persisted state or migration.
- Known follow-up work: visibility, fog-of-war, stairs transitions, and map
  persistence remain later tickets; no connectivity tuning was made canonical.
- Non-goals: presentation integration, commands, events, content, gameplay,
  save changes, and all later TEL tickets.
- Invariants: every generated non-wall tile is reachable from the upper stairs,
  including the lower stairs; generation remains deterministic and
  renderer-independent.
- Acceptance: 27 architecture tests pass, formatter verification passes, and
  the Release build and package checks pass with zero build warnings.

## TEL-013 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: configurable neighborhood visibility, observed/visited state
  precedence, invalid position/radius validation, and deterministic snapshots.
- Files/modules affected: `src/Telengard.Core/world/visibility/TileVisibility.cs`,
  `tests/Telengard.Architecture.Tests/TileVisibilityTests.cs`, and this status
  document.
- New public APIs: `TileVisibility`, `TileVisibilityOptions`, and
  `TileVisibilityMap.Resolve`/`GetVisibility`.
- New events: none; visibility resolution is a pure simulation-side read and
  does not commit a domain fact.
- Save-schema impact: none; observed and visited inputs remain caller-owned
  until the fog-of-war and map-persistence tickets define authoritative state.
- Known follow-up work: movement, floor transitions, and map persistence remain
  later tickets; view radius is configuration/tuning rather than canonical game
  balance.
- Non-goals: movement, commands, events, rendering, map persistence, and all
  later TEL tickets.
- Invariants: hidden layout data is not exposed by visibility classification;
  positions are validated against the generated floor; core remains
  renderer-independent.
- Determinism: the resolver uses only its layout, position, knowledge inputs,
  and configured radius; equal inputs produce equal visibility snapshots.
- Acceptance: 31 solution tests pass, formatter verification passes, Release
  build has zero warnings, and Release package creation succeeds.

## TEL-014 verification

- Status: implemented; the map is an immutable simulation-side fog-of-war
  state over one generated floor.
- Tests added: observed versus visited tile tracking, hidden-tile preservation,
  immutable updates, invalid-position validation, and visibility projection.
- Files/modules affected: `src/Telengard.Core/world/visibility/TileVisibility.cs`
  and `tests/Telengard.Architecture.Tests/TileVisibilityTests.cs`.
- New public APIs: `FogOfWarMap`, `Create`, `Observe`, `Visit`, and `Resolve`.
- New events: none; this slice has no command or authoritative transition.
- Save-schema impact: none; persistence is explicitly deferred to TEL-016.
- Known follow-up work: movement and floor transitions remain TEL-015; map
  persistence remains TEL-016. View radius remains configuration/tuning.
- Non-goals: movement, commands, events, persistence, rendering, content, and
  all later TEL tickets.
- Invariants: unknown positions remain unknown, visited positions are always
  observed, updates validate floor/layout bounds, and core stays renderer-independent.
- Determinism: no randomness; equal layouts and map inputs produce equal
  observations, visits, and visibility projections.
- Acceptance: focused tests, formatter, Release tests, and Release build are
  required before completion.

## TEL-015 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: down-stair transition and event emission, up-stair return,
  wrong-stair rejection, and floor-boundary validation.
- Files/modules affected: `src/Telengard.Core/world/generation/FloorTransition.cs`,
  `tests/Telengard.Architecture.Tests/FloorTransitionTests.cs`, and this status
  document.
- New public APIs: `StairDirection`, `ChangeFloorCommand`,
  `FloorChangedEvent`, and `FloorTransitionResolver.Apply`.
- New events: `FloorChangedEvent`.
- Save-schema impact: none; the existing persisted player position already
  carries the authoritative floor, and no save version or DTO changed.
- Known follow-up work: movement, expedition tracking, map persistence, and
  presentation integration remain later tickets; no balance or formula was
  selected.
- Non-goals: movement, expedition completion, persistence changes, rendering,
  content, and all later TEL tickets.
- Invariants: transitions validate the current stair, direction, adjacent floor,
  and boundary before mutation; the resolver remains renderer-independent.
- Determinism: destination layouts are supplied by the existing seeded layout
  generator; the transition itself performs no random draws.
- Acceptance: 36 solution tests pass, formatter verification passes, and the
  Release build passes with zero warnings.

## TEL-016 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: persistent map conversion and invariant tests; save round-trip,
  v1 migration, deterministic ordering, and invalid visited-position tests.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/world/visibility/TileVisibility.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `src/Telengard.Save/SaveGameSerializer.cs`, and architecture tests.
- New public APIs: `PersistentMapState`, `LegacyState.PersistentMap`,
  `LegacyStateDto`, `PersistentMapStateDto`, `FogOfWarMap.ToPersistentState`,
  and `FogOfWarMap.Create` overload accepting persistent state.
- New events: none; map persistence is state/save infrastructure and does not
  invent a gameplay transition or event.
- Save-schema impact: save version 2 persists observed and visited dungeon
  positions under `legacy.persistentMap`; version 1 saves migrate to an empty
  map while preserving existing state.
- Known follow-up work: movement and expedition integration can update the
  persistent map in later scoped tasks; no tuning or anti-save-scumming policy
  was selected.
- Non-goals: movement, new commands/events, content, renderer integration,
  journal/knowledge entries, and all later TEL tickets.
- Invariants: visited positions are always observed; positions are normalized
  deterministically; invalid floor/coordinate values are rejected when a map is
  restored against a generated layout; unknown layout data is never persisted.
- Determinism: map state uses no randomness, has stable ordering and value
  equality, and save round trips remain byte-stable.
- Acceptance: focused and full solution tests, formatter verification, Release
  build, and package creation pass.

## TEL-020 verification

- Status: implemented and verified.
- Tests added: deterministic expedition start, active/dead entry rejection,
  deepest/floor-visited tracking across stair transitions, complete
  expedition DTO round-trip coverage, and version-1 expedition-field
  migration defaults.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Core/world/generation/FloorTransition.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`, and
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/ExpeditionStateTests.cs`, and
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`.
- New public APIs: the additional `ExpeditionState` and
  `ExpeditionStateDto` tracking fields, `ExpeditionStartedEvent`, and the
  entry integration that starts an expedition.
- New events: `ExpeditionStartedEvent`; existing dungeon-entry and floor-change
  events remain unchanged.
- Save-schema impact: save version and JSON migration remain unchanged;
  expedition tracking fields are explicitly serialized in the existing save
  DTO and version-1 migration receives their defaults.
- Known follow-up work: inn state, carried/secured wealth, expedition
  completion, suspension, and any balance or anti-save-scumming policy remain
  their own tickets. Room-count updates, objectives, and simulation-time
  advancement remain configuration/future integration decisions.
- Non-goals: completion/failure resolution, economy, suspend saves, combat,
  presentation integration, and all later TEL tickets.
- Invariants: only a living player without an active expedition can enter;
  entry establishes active state at floor 1; active floor transitions preserve
  deterministic visited-floor order and deepest-floor tracking; the resolver
  remains renderer-independent.
- Determinism: expedition IDs derive from world seed, simulation tick, and
  player identity; no random stream or uncontrolled clock is used.
- Acceptance: focused checks pass; the full verification steps completed
  restore, formatter verification, a zero-warning Release build, and 48
  passing architecture tests. The wrapper could not write its verification
  stamp because this checkout has no Git `HEAD` for its fingerprint command.

## TEL-021 verification

- Status: implemented; restore, formatter verification, zero-warning Release
  build, and 53 passing architecture tests completed with the repository-local
  .NET 8 SDK.
- Tests added: initial-at-inn state, entry/return boundary transitions,
  invalid non-inn entry and inactive return rejection, save round trip, and
  version-2 migration defaults.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/InnStateTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`, and the
  TEL-021 ExecPlan.
- New public APIs: `InnState`, `GameState.Inn`, and `InnStateDto`.
- New events: none; the existing `DungeonLeftEvent` remains the committed
  return transition event.
- Save-schema impact: current save version advanced from 2 to 3; version 2
  and version 1 saves migrate to `IsAtInn = true`, while simulation,
  generator, and content versions remain unchanged.
- Known follow-up work: expedition completion, carried/secured gold, rest,
  leveling, identification, loadouts, objectives, and suspension remain later
  tickets. Returning to the inn deliberately leaves `Expedition.Active` true
  until TEL-024 owns completion.
- Non-goals: banking, wealth rules, completion events, balance/formulas,
  presentation integration, and all later TEL tickets.
- Invariants: Core owns the boundary; entry and return validate before
  mutation; no random or hidden-information behavior was added; renderers do
  not gain simulation authority.
- Determinism: the state transition has no randomness and equal inputs produce
  equal state/events.
- Acceptance: focused TEL-021 tests, full solution tests, formatter
  verification, and Release build passed. `./eng/verify.ps1 -Mode Full`
  completed all functional stages but could not write its stamp because this
  checkout has no Git `HEAD` for its fingerprint command.

## TEL-022 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: positive acquisition, deterministic replay, non-positive and
  overflow boundaries, inactive/at-inn rejection, mirrored-state invariant,
  event payload, and explicit save round-trip coverage.
- Files/modules affected: `src/Telengard.Core/economy/CarriedGold.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `tests/Telengard.Architecture.Tests/CarriedGoldTests.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs` (existing carried-gold DTO
  coverage), and this status document.
- New public APIs: `AcquireGoldCommand`, `GoldAcquiredEvent`, and
  `CarriedGoldResolver.Acquire`.
- New events: `GoldAcquiredEvent`, emitted only after carried gold commits.
- Save-schema impact: none; carried gold was already explicitly persisted in
  the version-3 player and expedition DTOs, and existing migrations retain
  their zero default.
- Known follow-up work: expedition completion and death-loss rules remain later
  tickets. No balance formula or anti-save-scumming policy was selected.
- Non-goals: secured wealth, banking, loot generation, death handling,
  expedition completion, presentation integration, and all later TEL tickets.
- Invariants: acquisition validates active dungeon state before mutation;
  player and expedition carried-gold mirrors remain equal; inn state is not a
  valid acquisition boundary; core remains renderer-independent.
- Determinism: equal state and command inputs produce equal state and event
  results; no randomness or uncontrolled clock was introduced.
- Acceptance: focused TEL-022 tests (9 passed), full solution tests (62
  passed), formatter verification, and Release build passed. Full verification
  passed all functional stages; its stamp step could not fingerprint this
  checkout because it has no Git `HEAD`.

## TEL-023 verification

- Status: implemented and verified with the repository-local .NET 8 SDK.
- Tests added: inn-return secured-gold transfer, carried-gold mirror
  validation, negative/overflow boundaries, committed event payload, explicit
  secured-gold save round trip, and version-3 migration defaults.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/DungeonWalkingTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`, and the
  TEL-023 ExecPlan.
- New public APIs: `SecuredProgressState.SecuredGold` and
  `GoldSecuredEvent`.
- New events: `GoldSecuredEvent`, emitted after a positive carried-gold
  transfer commits at the inn boundary.
- Save-schema impact: current save version advanced from 3 to 4;
  version-3-and-earlier saves migrate with zero secured gold. Simulation,
  generator, and content versions are unchanged.
- Known follow-up work: expedition completion remains TEL-024; death-loss,
  banking UI, and other economy rules remain later work. No formula or
  anti-save-scumming policy was selected.
- Non-goals: expedition completion, death handling, loot generation,
  presentation integration, and all later TEL tickets.
- Invariants: the validated simulation return command transfers exactly the
  mirrored carried amount, clears carried gold, preserves active expedition
  state for TEL-024, and leaves presentation without simulation authority.
- Determinism: the transfer is pure state arithmetic with no randomness;
  equal state and command inputs produce equal state and event results.
- Acceptance: focused tests (67 passed), formatter verification, Release build
  (0 warnings), and Release tests (67 passed) passed. Full verification passed
  restore, format, build, and test stages; its stamp step could not fingerprint
  this checkout because it has no Git `HEAD`.

## TEL-024 verification

- Status: implemented; returning to the inn now completes the active
  expedition in the renderer-independent simulation.
- Tests added: completion deactivates the expedition, emits a stable
  `ExpeditionSucceededEvent`, preserves the existing return and gold events,
  rejects inactive returns through existing validation, and reproduces equal
  state/event results for equal inputs.
- Files/modules affected: `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `tests/Telengard.Architecture.Tests/DungeonWalkingTests.cs`,
  `tests/Telengard.Architecture.Tests/InnStateTests.cs`,
  `tests/Telengard.Architecture.Tests/ExpeditionStateTests.cs`, and this status
  document.
- New public APIs: `ExpeditionSucceededEvent`.
- New events: `ExpeditionSucceededEvent`, emitted after the committed return
  and secured-gold transitions.
- Save-schema impact: none; `Expedition.Active` was already explicitly
  persisted, and save/simulation/generator/content version fields are unchanged.
- Known follow-up work: death handling, progression, and all later TEL tickets
  remain out of scope. No formula, balance, content, or anti-save-scumming
  policy was selected.
- Invariants: command validation remains in the simulation; carried gold is
  secured before completion; renderers gain no simulation authority; no
  randomness or hidden-information behavior was added.
- Acceptance: focused Release tests (19 passed), formatter verification, and
  full verification passed restore, format, a zero-warning Release build, and
  93 Release tests.

## TEL-025 verification

- Status: implemented and verified.
- Tests added: valid suspension preserves the active resume state and emits a
  stable `GameSuspendedEvent`; inactive, inn, and dead states are rejected;
  equal inputs reproduce equal state/events; and explicit save/load preserves
  the suspended expedition.
- Files/modules affected: `src/Telengard.Core/meta/GameSuspension.cs`,
  `tests/Telengard.Architecture.Tests/ExpeditionSuspensionTests.cs`,
  `docs/exec-plans/completed/TEL-025.md`, and this status document.
- New public APIs: `SuspendExpeditionCommand`, `GameSuspendedEvent`, and
  `ExpeditionSuspensionResolver.Suspend`.
- New events: `GameSuspendedEvent`, emitted after successful validation without
  mutating the resume state.
- Save-schema impact: none; the existing version-4 explicit DTO already
  persists the complete active expedition and all save/simulation/generator/
  content version fields. No migration was required.
- Known follow-up work: anti-save-scumming policy, death handling, and later
  TEL tickets remain separate decisions and work.
- Invariants: simulation owns validation and the event boundary; the active
  expedition remains active for resume; no randomness, hidden-information,
  wealth, or presentation authority was added.
- Acceptance: focused TEL-025 tests (4 passed), full Release tests (97
  passed), formatter verification, zero-warning Release build, and
  `./eng/verify.ps1 -Mode Full` all passed.

## TEL-030 verification

- Status: implemented and verified; this ticket only defines monster content
  and runtime data contracts.
- Tests added: six headless schema tests covering all definition fields,
  identity/family/tag validation, defensive collection copies, runtime
  instance state and boundaries, and JSON serialization separation.
- Files/modules affected: `src/Telengard.Content/Monsters/MonsterDefinition.cs`,
  `src/Telengard.Core/combat/MonsterInstance.cs`, and
  `tests/Telengard.Architecture.Tests/MonsterSchemaTests.cs`.
- New public APIs: `MonsterFamily`, `MonsterStats`, `MonsterSpawnRules`,
  `MonsterDefinition`, and `MonsterInstance`.
- New events: none; spawning and encounters are explicitly deferred.
- Save-schema impact: none; no `GameState` or persisted authoritative state
  was added, so save version and migrations are unchanged.
- Known follow-up work: encounter triggers, monster spawning, combat, threat
  classification, and content loading remain later tickets. Stat keys,
  spawn-rule keys, loot-table catalogs, and balance values remain
  `CONFIGURATION/TUNING DECISION REQUIRED` until their owning systems define
  them.
- Non-goals: commands, events, spawning, combat, threat assessment,
  knowledge, rendering, persistence, and all later TEL tickets.
- Invariants: content definitions remain separate from simulation algorithms;
  runtime instances reference definition IDs; collections are copied at the
  boundary; no hidden facts are exposed to presentation; no uncontrolled
  randomness was introduced.
- Acceptance: focused schema tests passed; formatter verification passed; and
  `./eng/verify.ps1 -Mode Full` passed restore, format, zero-warning Release
  build, and 107 Release tests.

## TEL-031 verification

- Status: implemented and verified; encounter triggering remains a
  renderer-independent, configured simulation boundary.
- Tests added: four headless tests covering configured movement integration,
  deterministic event/state replay, no-trigger boundaries, validation, and
  configuration limits.
- Files/modules affected: `src/Telengard.Core/combat/EncounterTrigger.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`, and
  `tests/Telengard.Architecture.Tests/EncounterTriggerTests.cs`.
- New public APIs: `EncounterSpawnOption`,
  `EncounterTriggerConfiguration`, `EncounterStartedEvent`, and
  `EncounterTriggerResolver.Evaluate`; movement accepts optional encounter
  configuration for simulation-owned integration.
- New events: `EncounterStartedEvent`, emitted only when a configured,
  deterministic trigger commits an encounter result.
- Save-schema impact: none; this ticket does not retain combat/encounter state,
  so save version and migrations are unchanged.
- Known follow-up work: combat action resolution, content catalogs, threat
  classification, death handling, and balance/formula decisions remain later
  tickets. Spawn level, hit points, candidate ordering, and trigger chance are
  caller-supplied configuration rather than canonical game rules.
- Non-goals: combat actions/state, threat assessment, death, persistence,
  presentation, content loading, and all later TEL tickets.
- Invariants: Core validates active/living expedition state before evaluation;
  movement remains the command boundary; the RNG stream is scoped by stable
  expedition/tick/position inputs; no renderer dependency or uncontrolled
  randomness was added.
- Acceptance: focused TEL-031 tests (4 passed), formatter verification, and
  `./eng/verify.ps1 -Mode Full` passed restore, format, zero-warning Release
  build, and 111 Release tests.

## TEL-032 verification

- Status: implemented and verified; encounters now commit an active combat
  state at the contact phase and phase/action-intent transitions remain in the
  renderer-independent simulation.
- Tests added: five headless tests covering encounter initialization, the full
  phase cycle, command/phase validation, deterministic replay, and explicit
  active-combat save round trips.
- Files/modules affected: `src/Telengard.Core/combat/CombatState.cs`,
  `src/Telengard.Core/combat/EncounterTrigger.cs`,
  `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Core/world/generation/FloorTransition.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`, and
  `tests/Telengard.Architecture.Tests/CombatStateMachineTests.cs`.
- New public APIs: `CombatState`, `CombatPhase`, `CombatAction`,
  `AdvanceCombatCommand`, `SelectCombatActionCommand`,
  `CombatStateResolver`, and `CombatPhaseChangedEvent`.
- New events: `CombatPhaseChangedEvent`; existing `EncounterStartedEvent`
  now follows committed combat-state initialization.
- Save-schema impact: current save version advanced from 4 to 5. Active combat
  runtime state is explicitly serialized; version 1–4 saves migrate with no
  active combat. Simulation, generator, and content versions are unchanged.
- Known follow-up work: attack, defend, maneuver, spell/item effects, flee,
  threat classification, enemy behavior, damage, death, and combat-ending
  events remain TEL-033–037 or later. No formula or balance policy was chosen.
- Non-goals: action resolution, death handling, threat disclosure, content
  catalogs, presentation integration, and all later TEL tickets.
- Invariants: commands validate before mutation; the encounter event is emitted
  after combat state commits; no uncontrolled randomness or hidden definition
  data was introduced; renderers gain no simulation authority.
- Acceptance: focused tests, full Release tests, formatter verification,
  Release build, and `./eng/verify.ps1 -Mode Full` passed.

## TEL-033 verification

- Status: implemented and verified; selected attacks now resolve in the
  renderer-independent simulation.
- Tests added: surviving damage and phase advancement, lethal damage and
  combat closure, validation and boundary behavior, deterministic replay,
  save round trip, and prevention of bypassing attack resolution.
- Files/modules affected: `src/Telengard.Core/combat/Attack.cs`,
  `src/Telengard.Core/combat/CombatState.cs`,
  `src/Telengard.Core/combat/MonsterInstance.cs`,
  `tests/Telengard.Architecture.Tests/AttackTests.cs`,
  `tests/Telengard.Architecture.Tests/CombatStateMachineTests.cs`, and this
  status/ task ledger documentation.
- New public APIs: `AttackConfiguration`, `AttackCommand`,
  `AttackResolver`, `MonsterDamagedEvent`, and `MonsterKilledEvent`.
- New events: `MonsterDamagedEvent` and `MonsterKilledEvent`; surviving
  attacks also emit the existing `CombatPhaseChangedEvent`.
- Save-schema impact: none; attack updates existing persisted monster hit
  points, combat phase, and expedition defeat-count fields through the
  existing explicit DTOs. Save and simulation versions remain unchanged.
- Known follow-up work: damage formula, hit chance, weapon/content data,
  enemy action, defend, flee, threat classification, and player death remain
  configuration decisions or later TEL tickets.
- Non-goals: defend, maneuver, spell/item effects, flee, threat assessment,
  enemy behavior, player death, presentation, and all later TEL tickets.
- Invariants: commands validate before mutation; a selected attack cannot be
  skipped by phase advancement; deterministic replay and explicit save/load
  remain stable; no hidden definition data or renderer authority was added.
- Acceptance: focused attack/combat tests (13 passed), formatter
  verification, zero-warning Release build, and full verification (124
  Release tests) passed.

## TEL-034 verification

- Status: implemented and verified; selected defend actions now resolve in
  the renderer-independent simulation before enemy action.
- Tests added: five headless tests covering committed resolution and event
  payload, validation boundaries, deterministic replay, save round trip, and
  prevention of bypassing defend resolution; the combat state-machine test
  now exercises the defend resolver in the lifecycle.
- Files/modules affected: `src/Telengard.Core/combat/CombatState.cs`,
  `src/Telengard.Core/combat/Defend.cs`,
  `tests/Telengard.Architecture.Tests/CombatStateMachineTests.cs`,
  `tests/Telengard.Architecture.Tests/DefendTests.cs`, and this status/task
  ledger documentation.
- New public APIs: `DefendCommand` and `DefendResolver`.
- New events: none; defend resolution emits the existing
  `CombatPhaseChangedEvent` after committing the `Resolution` to
  `EnemyAction` transition.
- Save-schema impact: none; the selected defend action already uses the
  explicit combat save DTO and no new authoritative state was added.
- Known follow-up work: enemy-action resolution and any damage-mitigation
  formula remain later work or `CONFIGURATION/TUNING DECISION REQUIRED`;
  maneuver, flee, threat classification, death, presentation, and all later
  TEL tickets remain out of scope.
- Non-goals: mitigation balance, enemy behavior, new content, persistence
  schema changes, presentation integration, and all later TEL tickets.
- Invariants: validation occurs before mutation; selected defend cannot be
  skipped by generic phase advancement; equal inputs reproduce equal state and
  events; no randomness, hidden-information exposure, or renderer authority
  was added.
- Acceptance: focused DefendTests (5 passed), combat state-machine tests (5
  passed), formatter verification, zero-warning Release build, and full
  verification (129 Release tests) passed.

## TEL-035 verification

- Status: implemented and verified; flee now resolves through the
  renderer-independent combat simulation.
- Tests added: successful flee closes combat and emits a stable
  `EncounterEndedEvent`; failed flee advances to enemy action; validation,
  deterministic replay, selected-action bypass prevention, probability
  boundaries, and explicit save round-trip coverage.
- Files/modules affected: `src/Telengard.Core/combat/Flee.cs`,
  `src/Telengard.Core/combat/CombatState.cs`,
  `tests/Telengard.Architecture.Tests/FleeTests.cs`, this status document,
  and the task ledger.
- New public APIs: `FleeConfiguration`, `FleeCommand`, `FleeResolver`, and
  `EncounterEndedEvent`.
- New events: `EncounterEndedEvent`, emitted after a successful flee;
  failed attempts emit the existing `CombatPhaseChangedEvent` when moving
  to enemy action.
- Save-schema impact: none; flee uses the existing persisted combat action
  and combat DTO. Save, simulation, generator, and content versions remain
  unchanged.
- Known follow-up work: enemy-action resolution, threat classification,
  player death, and remaining combat actions are later tickets. Flee chance
  is supplied by configuration; no balance formula was selected.
- Non-goals: enemy behavior, player death, threat disclosure, presentation,
  persistence schema changes, and all later TEL tickets.
- Invariants: commands validate before mutation; flee randomness uses a
  scoped deterministic stream from stable world/version/encounter/round
  inputs; equal inputs reproduce equal state/events; no hidden definition
  data or renderer authority was added.
- Acceptance: focused FleeTests (7 passed), formatter verification, Release
  build with 0 warnings, 136 Release tests, and `./eng/verify.ps1 -Mode Full`
  all passed. The verification script required a process-scoped PowerShell
  execution-policy bypass because the host policy rejected unsigned local
  scripts.

## TEL-036 verification

- Status: implemented and verified; threat assessment now resolves through the
  renderer-independent combat simulation.
- Tests added: approximate trivial, dangerous, deadly, and unknown
  classification; committed state/event ordering; active/living/phase
  validation; deterministic replay; configuration-copy boundaries; save round
  trip; and version-5 save migration without a threat category.
- Files/modules affected: `src/Telengard.Core/combat/CombatState.cs`,
  `src/Telengard.Core/combat/ThreatAssessment.cs`,
  `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/CombatStateMachineTests.cs`,
  `tests/Telengard.Architecture.Tests/ThreatAssessmentTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`, and this
  status/task ledger documentation.
- New public APIs: `ThreatLevel`,
  `ThreatClassificationConfiguration`, `AssessThreatCommand`,
  `ThreatAssessmentResolver`, and `ThreatAssessedEvent`; `CombatState` now
  retains the current encounter's assessed category.
- New events: `ThreatAssessedEvent`, emitted after the category and transition
  to player action commit.
- Save-schema impact: current save version advanced from 5 to 6; version-5
  active-combat saves migrate with no assessed category when that field is
  absent. Simulation, generator, and content versions are unchanged.
- Design choice: the level-difference thresholds and known-definition inputs
  are caller-supplied configuration/observed knowledge. The specification
  defines no canonical threat formula, so no balance constant was made
  permanent. The event exposes only the approximate category, never exact
  monster stats or hidden definition data.
- Known follow-up work: journal sample/confidence and persistent monster
  knowledge remain TEL-052–TEL-054,
  player death remains TEL-037, and content/balance policy remains a
  `CONFIGURATION/TUNING DECISION REQUIRED` where not yet defined. No next TEL
  ticket was started.
- Invariants: commands validate before mutation; threat assessment cannot be
  skipped by generic phase advancement; equal inputs reproduce equal state and
  events; explicit save DTO/migration handling preserves compatibility; no
  randomness, presentation authority, or hidden exact-stat disclosure was
  added.
- Acceptance: focused threat tests (6 passed), save tests (15 passed), full
  Release tests (143 passed), formatter verification, zero-warning Release
  build, and `./eng/verify.ps1 -Mode Full` all passed. The verification script
  required a process-scoped PowerShell execution-policy bypass because the
  host policy rejected unsigned local scripts.

## TEL-037 verification

- Status: implemented and verified; lethal player state now resolves through
  the renderer-independent simulation and closes the active encounter and
  expedition.
- Tests added: committed death/failure state and event ordering, combat
  state-check integration, validation boundaries, deterministic replay,
  explicit save round trip, and rejection of movement/floor changes after
  death.
- Files/modules affected: `src/Telengard.Core/combat/Death.cs`,
  `src/Telengard.Core/combat/CombatState.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Core/world/generation/FloorTransition.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`, and this status/task
  ledger documentation.
- New public APIs: `PlayerDeathCommand`, `PlayerDeathResolver`,
  `PlayerDiedEvent`, and `ExpeditionFailedEvent`.
- New events: `PlayerDiedEvent` followed by `ExpeditionFailedEvent` after the
  authoritative death transition commits.
- Save-schema impact: none; existing `Player.Alive`, HP, expedition-active,
  inn, carried-gold, and combat DTO fields already persist the resulting state;
  save and simulation versions are unchanged.
- Design choice: the state-check hook treats `HitPoints <= 0` as lethal.
  Carried gold remains in its unsecured pools and secured gold is unchanged;
  Classic deletion is implemented by TEL-080, while Legacy/Adventure loss and
  retention policies remain configuration/follow-up work owned by TEL-081–082
  rather than an invented formula here.
- Known follow-up work: enemy damage production, Legacy/Adventure death,
  legacy, grave, and heirloom behavior remain later tickets. No next TEL ticket
  was started.
- Invariants: commands validate before mutation; death clears active combat,
  ends the expedition, preserves persistent map knowledge and secured wealth,
  and prevents dead-player movement/floor changes; no randomness, hidden
  information, or presentation authority was added.
- Acceptance: focused death/combat tests (11 passed), formatter verification,
  full Release tests, Release build, and `./eng/verify.ps1 -Mode Full` passed.

## TEL-080 verification

- Status: implemented; Classic death now deletes the character state and
  unsecured expedition assets through the existing death command boundary.
- Tests added: Classic character deletion clears character-owned progression,
  inventory/equipment, carried gold, and acquired items; secured progress and
  existing death/failure event ordering remain stable; deterministic replay,
  explicit save round trip, and post-death movement/floor restrictions remain
  covered.
- Files/modules affected: `src/Telengard.Core/combat/Death.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`, and this status/task
  ledger documentation.
- New public APIs: none; the existing `PlayerDeathCommand` and
  `PlayerDeathResolver` boundary now applies Classic deletion.
- New events: none; `PlayerDiedEvent` followed by `ExpeditionFailedEvent`
  remains the committed event sequence.
- Save-schema impact: none; the existing explicit player and expedition DTOs
  already persist the deleted/dead state, and save/simulation/generator/content
  versions remain unchanged.
- Known follow-up work: Legacy and Adventure death policies, dead-hero
  records, graves, and heirlooms remain later tickets. No next TEL ticket was
  started.
- Invariants: validation remains in the simulation; secured gold and
  persistent profile knowledge are unchanged; no randomness, hidden-information
  disclosure, or presentation authority was added.
- Acceptance: focused Release death tests (6 passed), formatter verification,
  full Release tests (290 passed), zero-warning Release build, and
  `./eng/verify.ps1 -Mode Full` all passed. The verification script required a
  process-scoped PowerShell execution-policy bypass because the host policy
  rejected unsigned local scripts.

## TEL-081 verification

- Status: implemented and verified; Legacy death now preserves the dead hero's identity and
  character progression, preserves persistent map/journal state and secured
  wealth, and loses unsecured gold and carried equipment/loot references.
- Tests added: Legacy death state/policy coverage and deterministic replay;
  existing death event ordering, save round-trip, and dead-state movement
  restrictions remain covered.
- Files/modules affected: `src/Telengard.Core/combat/Death.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`, and this status document.
- New public APIs: none; the existing `PlayerDeathCommand`,
  `PlayerDeathResolver`, `PlayerDiedEvent`, and `ExpeditionFailedEvent` remain
  the command/event boundary.
- New events: none.
- Save-schema impact: none; the existing explicit player and expedition DTOs
  already persist the resulting dead-hero and unsecured-asset state. Save,
  simulation, generator, and content versions remain unchanged.
- Known follow-up work: the specification leaves the exact retained-equipment
  and heirloom policy as a `CONFIGURATION/TUNING DECISION REQUIRED`; this slice
  does not invent that policy. Dead-hero records, graves, heirlooms, and
  Adventure death remain TEL-082–TEL-085. No next TEL ticket was started.
- Invariants: validation remains in the simulation; persistent knowledge and
  secured wealth are unchanged; carried/unsecured wealth is not secured by
  death; no randomness, hidden-information disclosure, or presentation
  authority was added.
- Acceptance: focused `DeathTests` (9 passed), formatter verification,
  zero-warning Release build, and full verification passed with 293 Release
  tests using the repository-local SDK and a process-scoped Git line-ending
  warning suppression for the unrelated dirty `CHANGELOG.md`.

## TEL-082 verification

- Status: implemented and verified; Adventure death now returns the expedition
  to the inn, restores the retained character to a living state, and discards
  expedition-carried gold and acquired loot.
- Tests added: Adventure state/event behavior, deterministic replay, and
  explicit save round-trip coverage; the existing death validation and combat
  state-check coverage remains green.
- Files/modules affected: `src/Telengard.Core/combat/Death.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`,
  `docs/tasks/README.md`, `CHANGELOG.md`, and this status document.
- New public APIs: none; the existing `PlayerDeathCommand`,
  `PlayerDeathResolver`, `PlayerDiedEvent`, and `ExpeditionFailedEvent` remain
  the command/event boundary.
- New events: none; Adventure uses the existing committed death/failure event
  sequence.
- Save-schema impact: none; the existing explicit player, expedition, inn,
  and version DTOs already persist the resulting state. Save, simulation,
  generator, and content versions remain unchanged.
- Design choice: Adventure retains player identity, progression, inventory,
  equipment, talents, spells, injuries, and effects; it clears the existing
  expedition-carried gold/acquired-loot pools and restores hit points at the
  inn. The specification does not define a percentage or random loss formula,
  so no such canonical tuning rule was introduced; finer partial-loss policy
  remains `CONFIGURATION/TUNING DECISION REQUIRED`.
- Known follow-up work: graves, heirlooms, and any finer Adventure loss tuning
  remain later work. No next TEL ticket was started.
- Invariants: validation occurs before mutation; persistent map knowledge and
  secured wealth remain unchanged; carried/unsecured wealth is not secured by
  death; equal inputs reproduce equal state/events; no randomness,
  hidden-information disclosure, or presentation authority was added.
- Acceptance: focused `DeathTests` (12 passed), formatter verification,
  zero-warning Release build, and `./eng/verify.ps1 -Mode Full` passed with
  296 Release tests using the repository-local SDK and a process-scoped
  `core.autocrlf=false` override for the host Git line-ending warning.

## TEL-083 verification

- Status: implemented and verified; Legacy death now appends a stable dead-hero
  record to persistent `LegacyState.PreviousHeroes`.
- Tests added: Legacy record creation and field capture, append behavior with
  existing records, explicit save round trip, version-10 migration to an empty
  collection, and invalid DTO validation.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/combat/Death.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`,
  `docs/tasks/README.md`, `CHANGELOG.md`, and this status/plan documentation.
- New public APIs: `DeadHeroRecord`, `LegacyState.PreviousHeroes`, and
  `DeadHeroRecordDto`.
- New events: none; the existing committed death/failure event sequence remains
  the command boundary.
- Save-schema impact: current save version advanced from 10 to 11. Explicit
  dead-hero DTOs are persisted, and version-10-and-earlier saves migrate to an
  empty record collection. Simulation, generator, and content versions are
  unchanged.
- Design choice: records capture stable hero identity, attributes, level, XP,
  death position, expedition ID, and deepest floor. Item loss, graves,
  heirlooms, and character replacement remain separate decisions/tickets; no
  balance or retention formula was introduced.
- Invariants: validation remains in the simulation; Legacy persistent map and
  secured wealth are unchanged; carried wealth is still lost on Legacy death;
  equal inputs reproduce equal state/events; no randomness, hidden-information
  disclosure, or presentation authority was added.
- Acceptance: focused death/save tests (32 passed), formatter verification,
  zero-warning Release build, and the full verification gate passed.

## TEL-084 verification

- Status: implemented and verified; Legacy death now appends a stable grave marker to
  persistent `LegacyState.Graves`.
- Tests added: grave creation and field capture, append behavior with existing
  graves, explicit save round trip, version-11 migration to an empty grave
  collection, and invalid DTO validation.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/combat/Death.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`,
  `docs/tasks/README.md`, `CHANGELOG.md`, and this status/plan documentation.
- New public APIs: `GraveRecord`, `LegacyState.Graves`, and `GraveRecordDto`.
- New events: none; the existing committed death/failure event sequence remains
  the command boundary.
- Save-schema impact: current save version advanced from 11 to 12. Explicit
  grave DTOs are persisted, and version-11-and-earlier saves migrate to an
  empty grave collection. Simulation, generator, and content versions are
  unchanged.
- Design choice: a grave stores only hero identity, death position, and
  expedition identity. The specification does not define grave encounters,
  contents, recovery, or balance, so loot, corpse, and heirloom behavior remain
  `CONFIGURATION/TUNING DECISION REQUIRED` or later ticket scope.
- Invariants: validation remains in the simulation; Legacy persistent map,
  dead-hero records, and secured wealth are unchanged; carried wealth is still
  lost on Legacy death; equal inputs reproduce equal state/events; no
  randomness, hidden-information disclosure, or presentation authority was
  added.
- Acceptance: focused death/save tests (33 passed), formatter verification,
  zero-warning Release build, and `./eng/verify.ps1 -Mode Full` passed with
  299 Release tests. The verification script required a process-scoped
  `core.autocrlf=false` override for unrelated dirty files and a
  process-scoped execution-policy bypass because the host rejects unsigned
  local scripts.

## TEL-085 verification

- Status: implemented and verified; Legacy death now appends one persistent
  heirloom record for each existing inventory identifier before the current
  character's inventory is cleared.
- Tests added: Legacy heirloom creation and append behavior, explicit save
  round trip, version-12 migration to an empty heirloom collection, invalid
  heirloom DTO validation, and existing deterministic death/save coverage.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/combat/Death.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`,
  `tests/Telengard.Architecture.Tests/DeathTests.cs`,
  `tests/Telengard.Architecture.Tests/SaveGameSerializerTests.cs`,
  `docs/tasks/README.md`, `CHANGELOG.md`, and this status/plan documentation.
- New public APIs: `HeirloomRecord`, `LegacyState.Heirlooms`, and
  `HeirloomRecordDto`.
- New events: none; the existing committed death/failure event sequence
  remains the command boundary.
- Save-schema impact: current save version advanced from 12 to 13. Explicit
  heirloom DTOs are persisted, and version-12-and-earlier saves migrate to an
  empty heirloom collection. Simulation, generator, and content versions are
  unchanged.
- Design choice: the current runtime exposes carried inventory as string
  identifiers but has no authoritative item-instance collection. The slice
  therefore preserves those identifiers, including duplicates, keyed to the
  dead hero. Equipment-instance retention, rarity/selection policy, and
  retrieval encounters remain `CONFIGURATION/TUNING DECISION REQUIRED` or
  later scope; no formula was invented.
- Known follow-up work: equipment-instance item storage, heirloom encounters,
  retrieval, and character replacement remain later work. No next TEL ticket
  was started.
- Invariants: validation occurs before mutation; persistent map, dead-hero
  records, graves, and secured wealth remain unchanged; carried/unsecured
  wealth is not secured by death; equal inputs reproduce equal state/events;
  no randomness, hidden-information disclosure, or presentation authority was
  added.
- Acceptance: focused death/save tests (34 passed), formatter verification,
  zero-warning Release build, and the full verification gate passed with 300
  Release tests. The gate required a process-scoped execution-policy bypass
  and `core.autocrlf=false` because the host rejects unsigned local scripts
  and reports an unrelated dirty-file line-ending warning.

## Mutation baseline

- Status: verified and persisted under `TestResults/mutation-baseline/`.
- Tooling: repository-local Stryker.NET 4.14.2 with SDK 8.0.100; Standard
  mutation level; no mutation exclusions or score gate.
- Scope: `Telengard.Core`, `Telengard.Content`, `Telengard.Save`, and
  `Telengard.Terminal`, each mapped to
  `tests/Telengard.Architecture.Tests`.
- Results: Core 663/1,020 killed, Content 23/41, Save 108/141, and Terminal
  0 mutants. The machine-readable audit contains 408 non-killed entries,
  including 80 Core actionables, 4 Content actionables, and 6 Save
  actionables; timeouts and compile errors remain explicitly recorded.
- Verification: the baseline ran against 156 discovered tests. The existing
  coverage artifacts report 1,383/1,383 lines and 616/616 branches.

## Known technical debt

- The implementation stack is selected in [ADR-001](adr/ADR-001-technology-stack.md), and the documented renderer-independent gameplay slices through Phase 4 are implemented. Mode-specific character creation, authored Core Alpha content, renderer prototypes, and later persistence/gameplay remain scaffolded or not started; the TEL-100 character-creation boundary is implemented.
- Gameplay remains incremental: expedition state, the inn boundary, carried
  gold, secured gold, encounters, combat, features, knowledge, items,
  progression, legacy death policies, and presentation-state projection are
  implemented in the completed slices; later content and policy decisions
  remain open.
- Undefined formulas and anti-save-scumming policy still require explicit design decisions before implementation.
- The architecture was initially validated against the Phase 0 synthetic
  command/replay slice and has since gained focused acceptance evidence for
  the completed gameplay slices documented above.

## AUD-006 verification

- Status: implemented; dungeon movement and floor transitions now reject
  inactive expeditions and at-inn states before state discovery, mutation, or
  committed events.
- Defect evidence: pre-remediation tests demonstrated that fresh inactive
  states could move and change floors; the new lifecycle tests fail without
  the guards and pass with them.
- Tests added/updated: inactive and at-inn movement rejection with unchanged
  persistent map, inactive and at-inn floor-transition rejection without
  mutation, and valid entered-dungeon movement/floor-transition fixtures.
- Files/modules affected: `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Core/world/generation/FloorTransition.cs`,
  `tests/Telengard.Architecture.Tests/DungeonWalkingTests.cs`, and
  `tests/Telengard.Architecture.Tests/FloorTransitionTests.cs`.
- Compatibility impact: save, simulation, generator, and content versions are
  unchanged. Valid active-expedition movement and transition vectors remain
  unchanged; invalid lifecycle commands now reject before mutation/event
  production.
- Acceptance: focused tests (17 passed), full Release solution tests (315
  passed), formatter verification, zero-warning Release build, and
  `./eng/verify.ps1 -Mode Full` passed. The gate required a process-scoped
  `core.autocrlf=false` override for unrelated dirty-file line-ending warnings
  and a process-scoped execution-policy bypass because the host rejects
  unsigned local scripts.

## AUD-005 verification

- Status: implemented and verified; expedition entry now advances an
  authoritative deterministic sequence before deriving the expedition ID.
  Sequential same-tick expeditions receive distinct IDs, and replay/save-load
  continuation reproduces the same ordered identities.
- Defect evidence: the pre-remediation `Phase2AcceptanceTests` flow completed
  and restarted an expedition without advancing `SimulationTick`; the added
  regression assertion failed against the old ID derivation.
- Tests added/updated: same-tick sequential identity and replay, save/load
  continuation, v13 migration seeding, dead-hero ID linkage, save-version
  expectations, and the post-remediation fixed expedition-ID vector.
- Files/modules affected: `src/Telengard.Core/Simulation/GameState.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Save/Dto/GameStateSaveDto.cs`,
  `src/Telengard.Save/Migrations/SaveMigrations.cs`, and the expedition,
  death, phase-2, and save serializer tests.
- Save-schema impact: current save version advanced from 13 to 14. The
  explicit root `ExpeditionSequence` field is persisted; v13-and-earlier saves
  with a retained expedition ID migrate to sequence one, while saves without
  one migrate to zero. Migrated saves advance the simulation compatibility
  version from 0.2 to 0.3. Generator and content versions remain 0.2.
- Compatibility impact: deterministic expedition-ID vectors intentionally
  changed; replay compatibility is version-bounded. Dungeon generation and
  content definitions are unchanged.
- Invariants: sequence advancement and ID derivation remain simulation-owned;
  no uncontrolled randomness, hidden-information disclosure, wealth semantic,
  event-boundary, or renderer-authority change was introduced. Dead-hero
  records continue to capture the committed expedition ID.
- Acceptance: focused remediation tests (67 passed), full Release solution
  tests (339 passed), repository doctor, formatter verification, zero-warning
  Release build, and `./eng/verify.ps1 -Mode Full` passed. The gate used a
  process-scoped execution-policy bypass because the host rejects unsigned
  local scripts and `core.autocrlf=false` to avoid unrelated dirty-file
  line-ending warnings. A separate coverage gate was not required by AUD-005.

## TEL-090 verification

- Status: implemented and verified; `PresentationStateAdapter.Create` now
  projects immutable renderer-facing state from authoritative `GameState`.
- Tests added: four focused adapter tests covering state projection,
  undiscovered-feature filtering, hidden monster-detail redaction, and
  read-only projection collections.
- Files/modules affected: `src/Telengard.Core/presentation/PresentationStateAdapter.cs`,
  `tests/Telengard.Architecture.Tests/PresentationStateAdapterTests.cs`,
  `docs/tasks/TEL-090.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: additive `PresentationStateAdapter.Create(GameState)` and
  the immutable `PresentationState`, `PresentationPlayerState`,
  `PresentationExpeditionState`, `PresentationFeatureState`,
  `PresentationCombatState`, and `PresentationMonsterState` projections.
- New events: none. Existing command and domain-event contracts are unchanged.
- Save-schema impact: none; no authoritative state or DTO changed, and save,
  simulation, generator, and content versions remain unchanged.
- Known follow-up work: renderer prototypes and renderer-independent save
  compatibility remain TEL-091–TEL-093. No next TEL ticket was started.
- Invariants: presentation only reads committed simulation state; hidden
  features are omitted, internal monster level/behavior/effects are not
  projected, and no randomness, save, wealth, knowledge, or renderer-authority
  boundary changed.
- Acceptance: focused adapter tests (4 passed), full Release tests (343
  passed), formatter verification, and zero-warning Release build passed via
  `./eng/verify.ps1 -Mode Full` using the repository-local SDK and a
  process-scoped execution-policy bypass for unsigned local scripts.

## TEL-117 verification

- Status: implemented and verified on a clean isolated branch anchored to
  `70c8f51b415c45d9b98928dc0478304ae94ac947`; the original anchored worktree
  was dirty and was preserved unchanged.
- Tests added: `tests/Tooling/TEL-117.Tooling.Tests.ps1` verifies role-based
  coverage aggregation, TestHarness visibility/exclusion, both default
  mutation-baseline guards, and an independent scoped Stryker invocation.
- Files/modules affected: `eng/coverage.ps1`, `eng/mutation.ps1`, the tooling
  regression script, `docs/DEVELOPMENT.md`,
  `docs/test-quality-current-audit.md`, this status document, the TEL task
  ledger, and `docs/tasks/TEL-117.md`.
- New public APIs: none. New events: none. Save-schema impact: none.
- Coverage behavior: production (`Core`, `Content`, `Save`, `Terminal`) is the
  gated aggregate; `TestHarness` remains measured and visible as test support.
- Mutation behavior: `-AdditionalStrykerArgs` is additive; scoped options are
  rejected for `mutation-baseline`, and scoped manifests record the forwarded
  arguments.
- Focused evidence on the merge-target branch: production coverage reported
  3,344/3,393 lines and 1,475/1,556 branches; TestHarness reported 42/42
  lines and 32/32 branches.
  Both guarded mutation forms returned nonzero, and the Basic Terminal scoped
  run succeeded in `TestResults/mutation-tel-117-scoped` with merge-target
  parent `3b9e01fe31317dbe447ec5f8b2fb06aca2bb7e44` recorded in the manifest.
- Default mutation evidence: `powershell.exe -NoProfile
  -ExecutionPolicy Bypass -File .\eng\mutation.ps1 -MutationLevel Basic`
  completed for Core, Content, Save, and Terminal in
  `TestResults/mutation-baseline`; the manifest recorded an empty additional
  argument list and the unchanged four-project production scope.
- Final merge-target gate: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\eng\verify.ps1 -Mode Full` passed with formatter verification, a zero-warning
  Release build, and 338/338 Release tests. The gate required a process-scoped
  `core.autocrlf=false` override and an ignored `.codex` stamp directory in the
  clean isolated worktree.
- Known follow-up work: production coverage deficits remain the existing
  coverage-remediation work; no gameplay or next TEL ticket was started.

## TEL-119 verification

- Status: implemented and verified; `docs/audit-status.json` is now the
  canonical machine-readable audit packet ledger, with deterministic derived
  status/provenance sections in the remediation playbook and P0 gate.
- Tests added: `eng/audit-status.tests.ps1` covers byte-stable repeated
  generation, canonical-field propagation, ticket-over-plan precedence,
  preservation of human-authored sections, stale-output failure, marker
  uniqueness, and active/completed plan/provenance validation.
- Files/modules affected: `docs/audit-status.json`,
  `eng/audit-status.ps1`, `eng/audit-status.tests.ps1`,
  `docs/engineering/audit-status.md`, the generated sections in
  `docs/AUDIT_REMEDIATION_PLAYBOOK.md` and `docs/gates/AUDIT-P0.md`,
  `.github/workflows/verification.yml`, and this status/changelog/task
  documentation.
- New public APIs: none.
- New events: none.
- Save-schema impact: none; simulation, generator, content, and save versions
  are unchanged.
- Known follow-up work: AUD-003, AUD-008, and AUD-009 remain explicitly open
  in the canonical ledger. TEL-119 does not implement any next TEL ticket and
  does not depend on TEL-117.
- Acceptance: focused audit-status tests, synchronized-output check,
  formatter verification, Release build, full Release tests, and the full
  repository verification gate passed on the merge-target branch.

## TEL-100 verification

- Status: implemented and verified; the renderer-independent character
  creation boundary validates one of the three named modes and delegates
  generation to a matching simulation-owned provider.
- Tests added: all three mode selections, invalid mode and provider mismatch
  rejection before provider/state change, committed event delivery after state
  commit, provider input forwarding, deterministic replay, null boundaries,
  and explicit save round-trip coverage.
- Files/modules affected: `src/Telengard.Core/Simulation/CharacterCreation.cs`,
  `tests/Telengard.Architecture.Tests/CharacterCreationTests.cs`,
  `docs/tasks/TEL-100.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: `CharacterCreationMode`, `CharacterCreationRequest`,
  `CreateCharacterCommand`, `CharacterCreationResult`,
  `ICharacterCreationInput`, `ICharacterCreationProvider`, and
  `CharacterCreationResolver`.
- New events: `CharacterCreatedEvent`, with only the committed player identity
  and selected mode.
- Save-schema impact: none; `PlayerState` is reused, the explicit DTOs and
  migrations are unchanged, and save version 14 remains current.
- Design choices: mode mechanics remain provider-owned for TEL-101–TEL-103;
  no rolled formula, point budget, daily-calendar rule, random source, or
  anti-reroll policy was invented. The event does not expose generation input
  or attributes.
- Known follow-up work: TEL-101–TEL-103 implement the three mode mechanics;
  no next TEL ticket was started.
- Invariants: mode validation precedes provider invocation and state
  replacement; equal provider inputs reproduce equal state/events; the
  simulation owns the transition and event; no renderer, hidden-information,
  wealth, or persistence boundary changed.
- Acceptance: focused TEL-100 tests (9 passed), formatter verification, and
  the final `./eng/verify.ps1 -Mode Full` gate passed with 352 Release tests,
  zero build warnings, and formatter verification. The pre-change baseline
  passed 343 Release tests.
## TEL-091 verification

- Status: implemented and verified; the Modern presentation now has a
  deterministic renderer frame/cue projection plus an optional Godot visual
  prototype.
- Tests added: `ModernRendererTests` covers known-map projection and stable
  ordering, current-tile marking, hidden-feature and hidden-monster-detail
  redaction, committed-event cue ordering, read-only collections, and null
  validation.
- Files/modules affected: `src/Telengard.Core/presentation/ModernRenderer.cs`,
  `tests/Telengard.Architecture.Tests/ModernRendererTests.cs`,
  `src/Telengard.Godot/ModernRenderer.gd`,
  `src/Telengard.Godot/ModernRenderer.tscn`,
  `src/Telengard.Godot/project.godot`, `src/Telengard.Godot/README.md`,
  `docs/tasks/TEL-091.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: additive `ModernRenderer.Create`, `ModernRenderFrame`, and
  the renderer-only frame, marker, HUD, combat, environment, cue, and enum
  types. Existing commands, event payloads, and `PresentationState` are
  unchanged.
- New events: none. Existing committed events are reduced to safe visual cues;
  hidden `EncounterStartedEvent` monster internals are not forwarded.
- Save-schema impact: none; no authoritative state or version marker changed.
- Known follow-up work: a future host integration can adapt the C# frame to the
  Godot dictionary contract and bind input to simulation commands. TEL-092 and
  TEL-093 remain not started; no next TEL ticket was started.
- Invariants: the renderer only reads immutable presentation state and
  committed events; hidden map/features/details remain filtered; no randomness,
  save, wealth, knowledge, command, or simulation-authority boundary changed.
- Acceptance: focused presentation tests passed (8 total), formatter
  verification passed, Release build passed with 0 warnings and 0 errors, the
  full Release suite passed 347 tests, and
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
  -Mode Full` passed all stages with a process-scoped
  `core.autocrlf=false` override.

## TEL-101 verification

- Status: implemented and verified; `ROLLED` character creation now generates six bounded attributes through a simulation-owned provider and the existing character-creation command boundary.
- Tests added: six-attribute/range coverage, named-stream deterministic replay, simulation-version versus generator-version scoping, invalid configuration and provider-boundary rejection, preservation of unrelated player state, committed event/state behavior, and explicit save round-trip coverage.
- Files/modules affected: `src/Telengard.Core/Simulation/CharacterCreation.cs`, `tests/Telengard.Architecture.Tests/CharacterCreationTests.cs`, `docs/tasks/TEL-101.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this status document.
- Save-schema impact: none; `PlayerAttributes` already use the explicit save DTO, save version 14 remains current, and no migration was required.
- Design choices: rolling is caller-supplied through six immutable inclusive ranges and an explicit policy version; the provider uses the named `character-creation` stream scoped by rolled mode, player identity, and policy version, with simulation version as the RNG compatibility version. No permanent roll formula, reroll limit, anti-reroll rule, or hidden RNG input was added to the product contract.
- Acceptance: focused TEL-101 tests, formatter verification, Release build, and the full Release suite passed; the final gate is rerun on this merged branch below.

## TEL-102 verification

- Status: implemented and verified; point-allocation character creation now
  validates a configured six-attribute budget and inclusive bounds through
  the TEL-100 simulation boundary.
- Tests added: exact-budget commit with event and save round trip, under/over
  budget rejection before mutation, bounds and malformed-input rejection,
  dispatcher no-event rejection, and equal-input replay.
- Files/modules affected: `src/Telengard.Core/Simulation/CharacterCreation.cs`,
  `tests/Telengard.Architecture.Tests/CharacterCreationTests.cs`,
  `docs/tasks/TEL-102.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: `PointAllocationCharacterCreationInput`,
  `PointAllocationCharacterCreationConfiguration`, and
  `PointAllocationCharacterCreationProvider`.
- New events: none; the existing `CharacterCreatedEvent` remains the
  committed boundary event with its stable minimal payload.
- Save-schema impact: none; player attributes continue to use the existing
  explicit DTO and save version 14 remains current.
- Design choices: the configured budget is the sum of the six supplied
  allocation values; no permanent budget, cost curve, derived-stat formula,
  renderer behavior, or other tuning policy was added.
- Known follow-up work: TEL-103 remains not started; daily-seed creation,
  starting loadout, and balance policy remain outside this slice.
- Acceptance: focused character-creation tests passed (19), formatter
  verification passed, and `./eng/verify.ps1 -Mode Full` passed with 361
  Release tests, zero build warnings, and zero errors.

## TEL-103 verification

- Status: implemented and verified; daily-seed character creation now derives
  six bounded attributes from a stable caller-supplied token through the
  renderer-independent simulation boundary.
- Tests added: cross-player replay independent of world seed and player ID,
  different-seed divergence, bounds and malformed-input rejection,
  validation-before-mutation, committed event behavior, and save round trip.
- Files/modules affected: `src/Telengard.Core/Simulation/CharacterCreation.cs`,
  `tests/Telengard.Architecture.Tests/CharacterCreationTests.cs`,
  `docs/tasks/TEL-103.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: `DailySeedCharacterCreationInput`,
  `DailySeedCharacterCreationConfiguration`, and
  `DailySeedCharacterCreationProvider`.
- New events: none; the existing `CharacterCreatedEvent` remains the
  committed boundary event with its stable minimal payload.
- Save-schema impact: none; generated attributes use the existing explicit
  player DTO and save version 14 remains current.
- Design choices: the explicit policy version is the RNG compatibility
  version, and daily-seed streams use a fixed world-seed-independent input so
  the same token produces equal results for all players. Calendar, timezone,
  reset-time, and anti-reroll policies remain deferred.
- Known follow-up work: TEL-101 remains not started on the remote baseline;
  TEL-104 and later Core Alpha setup/content work remain outside this slice.
- Acceptance: focused character-creation tests passed (22), formatter
  verification passed, Release build passed with 0 warnings and 0 errors, and
  the full Release suite passed 364 tests. The final
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
  -Mode Full` gate passed with a process-scoped `core.autocrlf=false` override.

## TEL-104 verification

- Status: implemented and verified; the new-game setup boundary now combines a
  supplied world seed, game mode, and completed character result into a
  ready-at-inn authoritative state.
- Tests added: valid state initialization and committed event, zero-seed
  support, invalid seed, mode, character, dead-player, non-initial-player,
  null-attribute, and scalar-state rejection, deterministic replay, explicit
  save round trip, and entry into the existing generated floor-1 dungeon.
- Files/modules affected: `src/Telengard.Core/Simulation/NewGameSetup.cs`,
  `tests/Telengard.Architecture.Tests/NewGameSetupTests.cs`,
  `docs/tasks/TEL-104.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: `NewGameSetupRequest`, `NewGameSetupResolver`, and
  `NewGameCreatedEvent`.
- Save-schema impact: none; existing explicit DTOs preserve the selected world
  seed, versions, mode, player, and initialized state; save version 14 remains
  current.
- Design choices: the caller must supply a stable `long` seed; nullable input
  is rejected so zero remains a valid seed, and no seed-generation, calendar,
  loadout, or balance policy was invented. Setup reuses `GameState.Create` as
  the canonical initializer and rejects dead or non-initial character results.
- Invariants: state remains simulation-owned, validation occurs before state
  creation, the committed event describes the resulting setup, equal inputs
  replay equally, and no hidden-information, wealth, content, or renderer
  boundary changes.
- Acceptance: focused TEL-104 tests (5 passed), formatter verification,
  Release build with 0 warnings and 0 errors, and the full Release suite (369
  passed) all passed. The final gate passed with a process-scoped `PATH`
  fallback to the pinned repository SDK because the isolated worktree did not
  contain the ignored `.dotnet` directory.

## TEL-105 verification

- Status: implemented and verified; active expeditions can now acquire
  content-resolved gold and item identifiers as unsecured treasure through a
  validated simulation command, and the existing return boundary secures item
  identifiers into player inventory.
- Tests added: `TreasureAcquisitionTests` covers gold and item acquisition,
  item-only treasure, committed opaque event summaries, invalid expedition and
  dead-player/overflow boundaries, deterministic replay, save and
  suspend/resume round trips, return-to-inn securing, and weighted content loot
  selection.
- Files/modules affected: `src/Telengard.Core/economy/TreasureAcquisition.cs`,
  `src/Telengard.Core/world/generation/DungeonWalking.cs`,
  `src/Telengard.Content/Items/LootTable.cs`,
  `tests/Telengard.Architecture.Tests/TreasureAcquisitionTests.cs`,
  `docs/tasks/TEL-105.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: `AcquireTreasureCommand`, `TreasureAcquiredEvent`,
  `TreasureItemsSecuredEvent`, `TreasureAcquisitionResolver`, `LootTable`,
  `LootTableEntry`, and `LootTableEngine`.
- Save-schema impact: none; `ExpeditionState.AcquiredItems` and carried gold
  already use the explicit expedition DTO, so save version 14 and existing
  migrations remain unchanged.
- Design choices: this slice consumes content-resolved item identifiers and
  does not add production loot files, item-instance inventory ownership, drop
  rates, rarity formulas, or balance policy. `LootTableEngine` performs only
  configured weighted selection using a named scoped deterministic stream;
  authored first-slice tables remain TEL-114.
- Invariants: acquisition requires a live active dungeon expedition, validates
  mirrored carried gold before mutation, never writes secured gold directly,
  and emits only gold/count facts rather than item definitions. Return moves
  acquired items into secured player inventory before completing the expedition.
  Content selection remains separate from Core state mutation and presentation.
- Acceptance: focused TEL-105 tests (8 passed) and formatter verification
  passed; the canonical full verification gate passed on the final diff.

## TEL-092 verification

- Status: implemented and verified; `Telengard.Terminal` now renders the
  existing immutable presentation state as stable ASCII/symbolic lines for
  inn and dungeon scenes, including known map positions, player/expedition
  status, discovered features, knowledge summaries, and redacted combat state.
  Committed domain events are projected to ordered safe cues without exposing
  hidden encounter payloads.
- Tests added: `TerminalRendererTests` covers deterministic dungeon projection
  and ordering, inn output, null boundaries, event-cue ordering, and hidden
  encounter-detail redaction.
- Files/modules affected: `src/Telengard.Terminal/TerminalRenderer.cs`,
  `tests/Telengard.Architecture.Tests/TerminalRendererTests.cs`,
  `docs/tasks/TEL-092.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: `TerminalRenderer.Render`.
- Save-schema impact: none; the renderer consumes existing
  `PresentationState` and does not change authoritative state, DTOs,
  migrations, or save/version markers.
- Design choices: output uses explicit invariant numeric formatting and `\n`
  separators; map, feature, and knowledge lines are deterministically sorted;
  encounter-start and other event cues summarize committed facts without
  forwarding hidden monster definition details. Command input, gameplay, and
  terminal-loop integration remain deferred.
- Invariants: the terminal boundary reads only renderer-facing state and
  committed events, never mutates `GameState`, consumes RNG, resolves commands,
  or exposes undiscovered features/internal monster fields. Carried and
  secured gold remain distinct in the projection.
- Acceptance: focused TEL-092 tests (3 passed), formatter verification,
  Release build with 0 warnings and 0 errors, the full Release suite (384
  passed), and `./eng/verify.ps1 -Mode Full` all passed.

## TEL-106 verification

- Status: implemented and verified; Legacy character replacement now commits
  through a renderer-independent simulation command after a recorded Legacy
  death.
- Tests added: `LegacyCharacterReplacementTests` covers retained knowledge and
  Legacy history, reset replacement/expedition state, save round trip,
  deterministic replay, validation-before-mutation, and post-commit event
  publication.
- Files/modules affected: `src/Telengard.Core/Simulation/LegacyCharacterReplacement.cs`,
  `tests/Telengard.Architecture.Tests/LegacyCharacterReplacementTests.cs`,
  `docs/tasks/TEL-106.md`, `docs/tasks/README.md`, `docs/BUILD_STATUS.md`, and
  `CHANGELOG.md`.
- New public APIs: `ReplaceLegacyCharacterCommand`,
  `LegacyCharacterReplacedEvent`, and `LegacyCharacterReplacementResolver`.
- Save-schema impact: none; existing explicit DTOs persist the retained
  `KnowledgeState` and `LegacyState`, and save version 14 remains current.
- Invariants: replacement requires Legacy mode, a recorded dead hero, an
  inactive expedition at the inn, and a fresh valid living character. The
  simulation preserves persistent knowledge and hidden-information boundaries,
  resets only the failed character/expedition run state, and publishes the
  committed event after state assignment.
- Acceptance: focused tests passed (6), formatter verification passed, Release
  build passed with 0 warnings and 0 errors, and the canonical
  `./eng/verify.ps1 -Mode Full` gate passed with 390 Release tests.

## TEL-107 verification

- Status: implemented and verified; the repository now has a deterministic,
  headless Core Alpha integration proof using configured test fixtures and the
  existing simulation command boundaries.
- Tests added: `CoreAlphaIntegrationTests` covers the setup-derived success
  loop through feature activation, encounter/threat/attack, explicit monster
  knowledge, carried treasure, suspension, save/reload continuation, retreat,
  and banking; a Legacy failure path covers death, treasure loss, save/reload,
  and knowledge-preserving character replacement.
- Files/modules affected: `tools/Telengard.TestHarness/SimulationTestHarness.cs`,
  `tests/Telengard.Architecture.Tests/CoreAlphaIntegrationTests.cs`,
  `docs/tasks/TEL-107.md`, `docs/tasks/README.md`, `CHANGELOG.md`, and this
  status document.
- New public APIs: initial-state overloads for the existing test harness only;
  no gameplay or renderer API changed.
- Save-schema impact: none; save version 14, DTOs, migrations, simulation,
  generator, and content versions are unchanged.
- Invariants: repeated scripts and uninterrupted versus save/reload scripts
  produce equal final saves and event signatures; hidden monster knowledge is
  added only by an explicit observation command; carried wealth remains
  unsecured until retreat; Legacy knowledge survives replacement.
- Acceptance: focused `CoreAlphaIntegrationTests` passed (2), formatter
  verification passed, and `./eng/verify.ps1 -Mode Full` passed on the final
  diff. Production content expansion and TEL-108 debug commands remain
  deferred.

## TEL-108 verification

- Status: implemented and verified; the headless test harness now executes
  deterministic line-oriented debug scripts with stable compact JSON Lines
  output for the §43 command surface and explicit save/load checkpoints.
- Tests added: `DeveloperDebugTests` covers simulation-routed teleport,
  setters, grants, spawns, reveal, and death commands; committed debug event
  output; deterministic replay; save/load equivalence; and invalid input or
  unavailable-system diagnostics.
- Files/modules affected: `src/Telengard.Core/Simulation/DeveloperDebug.cs`,
  `tools/Telengard.TestHarness/DebugScript.cs`,
  `tools/Telengard.TestHarness/Program.cs`,
  `tests/Telengard.Architecture.Tests/DeveloperDebugTests.cs`,
  `docs/tasks/TEL-108.md`, `docs/tasks/README.md`, `docs/DEVELOPMENT.md`,
  `CHANGELOG.md`, and this status document.
- New public APIs: additive simulation-owned developer debug commands and
  committed debug events for teleport, state setup, grants, spawning, and map
  reveal; the harness adds `DebugScriptSession` and `DebugScriptResult`.
- New events: `DebugTeleportedEvent`, `DebugHitPointsSetEvent`,
  `DebugLevelSetEvent`, `DebugItemGrantedEvent`, `DebugGoldGrantedEvent`,
  `DebugMonsterSpawnedEvent`, `DebugFeatureSpawnedEvent`, and
  `DebugMapRevealedEvent`.
- Save-schema impact: none; debug state uses existing persisted fields and
  save version 14 remains current. Save/load continues through the explicit
  DTO/migration boundary.
- Invariants: state-changing debug commands validate and commit in Core;
  normal movement, treasure, combat, and death use their existing public
  resolvers; RNG inspection uses a fresh scoped stream; the tool does not own
  authoritative state or renderer logic. `set danger` remains transient
  session configuration because no canonical persisted danger mechanic exists.
- Acceptance: focused `DeveloperDebugTests` passed (4), full Release tests
  passed (408), formatter verification passed, and the focused Release build
  passed with zero warnings and zero errors. The canonical
  `./eng/verify.ps1 -Mode Full` gate passed on the completed diff.

## TEL-093 verification

- Status: implemented and verified; the existing explicit save DTO/migration
  boundary now has a focused cross-boundary proof that a loaded authoritative
  state produces equivalent Modern and Terminal presentation output.
- Tests added: `RendererSaveCompatibilityTests` covers canonical save equality,
  command-dispatched enter/move/encounter events, deterministic continuation
  after reload, Modern projection equality, deterministic Terminal output,
  committed event cues, hidden feature and internal monster-detail redaction,
  and source-state immutability across rendering.
- Files/modules affected: `tests/Telengard.Architecture.Tests/RendererSaveCompatibilityTests.cs`,
  `docs/tasks/TEL-093.md`, `docs/tasks/README.md`, `CHANGELOG.md`,
  `docs/BUILD_STATUS.md`, and the active ExecPlan.
- Save-schema impact: none; save version 14, DTOs, migrations, simulation,
  generator, and content versions remain unchanged.
- Invariants: both renderers consume fresh presentation projections after
  reload; no renderer mutates authoritative state or receives undiscovered
  features or internal monster effects/behavior.
- Acceptance: `./eng/dotnet.ps1 test Telengard.sln --configuration Release
  --no-restore --filter FullyQualifiedName~RendererSaveCompatibilityTests`
  passed (1); `./eng/dotnet.ps1 format Telengard.sln --verify-no-changes`
  passed; and `./eng/verify.ps1 -Mode Full` passed with a Release build of
  0 warnings and 0 errors and 397 Release tests. Independent review and PR
  gates remain pending.
