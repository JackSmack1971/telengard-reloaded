# Audit P0 Remediation Gate

Date verified: 2026-08-18

Baseline: `34c7a4d00d8b7588869875f34edee1c1adfcdeaf`

Historical snapshot: this gate records the P0 state verified on 2026-08-18.
The verified implementation commits for that P0 result were AUD-001
`1c1a1262` and AUD-002 `d976c4c4`; the earlier wording's AUD-001 working-tree
reference is stale. At that snapshot, P1 remediation had not yet started.
This statement is historical and is not the current P1 status.
Subsequent current-`HEAD` records verify AUD-006, AUD-007, and AUD-005 work;
the canonical ticket/exec-plan ledger is authoritative for the remaining P1
status.

## Result

**PASS** — AUD-001 and AUD-002 meet their complete P0 acceptance criteria.

## AUD-001 — Canonical deterministic RNG encoding

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Canonical, culture-independent seed encoding | `src/Telengard.Core/Rng/DeterministicRng.cs:10-64` writes the world seed as little-endian `Int64`, strings as UTF-8 with little-endian `UInt32` byte lengths, and the scope count plus fields in caller order. Strict UTF-8 rejects malformed UTF-16 instead of applying a lossy fallback. | PASS |
| Field boundaries cannot collide with payload text | Length prefixes and the explicit scope count replace the former NUL delimiter. `DeterministicRngTests.Canonical_stream_encoding_distinguishes_embedded_nul_from_scope_boundaries` and `Canonical_stream_encoding_preserves_scope_cardinality_and_order` pass. | PASS |
| Culture changes do not change streams | `DeterministicRngTests.Canonical_stream_encoding_is_invariant_to_current_culture` compares `en-US` and `fa-IR` streams for the same negative seed and scope. | PASS |
| Invalid inputs remain rejected | `DeterministicRngTests.Invalid_stream_inputs_are_rejected` covers null/blank inputs, null scope elements, malformed UTF-16, and invalid draw ranges. | PASS |
| Explicit fixed vectors exist | `DeterministicRngTests.Fixed_seed_stream_has_a_stable_sequence_and_double_projection` records the post-remediation `0.2` stream values. `FloorLayoutGeneratorTests.Generator_output_is_stable_for_compatibility_version_inputs` records post-remediation layout fingerprints. Encounter tests record the post-remediation roll and instance-id vectors. | PASS |
| Generator, simulation, and content version decisions are documented | `GameVersions.Current` is `0.2` for simulation, generator, and content in `src/Telengard.Core/Simulation/GameState.cs:17`. The compatibility decision and version-bounded replay rule are recorded in `docs/exec-plans/completed/AUD-001.md` and `src/Telengard.Core/Rng/README.md`. Save schema remains unchanged. | PASS |
| Affected consumers and replay implications are covered | The focused run covered RNG, generation, encounter, flee, feature, item, altar, pit, game-state, and monster tests: **79 passed**. Replay and save-checkpoint coverage also passed in `SimulationTestHarnessTests`, `FleeTests`, feature tests, and the full suite. | PASS |

### AUD-001 version impact

- Deterministic vectors intentionally changed because the seed bytes changed.
- Current simulation, generator, and content compatibility versions advanced
  from `0.1` to `0.2`.
- `GameState.CurrentSaveVersion` remains `13`; no save DTO or migration change
  was required.
- Existing saves retain their persisted version set, so replay compatibility is
  version-bounded rather than silently claiming old and new vectors are equal.

## AUD-002 — Defensive ownership of authoritative collections

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Caller-owned mutable collections cannot mutate state after assignment | The Core scan found authoritative collection storage in `PlayerState`, `ExpeditionState`, `DungeonState`, `PersistentMapState`, `KnowledgeState`, and `LegacyState`; each copies inputs and exposes a read-only snapshot. `FloorLayout`, `ItemInstance`/`ItemObservedState`, and `MonsterInstance` also copy collection inputs. | PASS |
| Representative aliasing and read-only tests exist | `GameStateTests.Player_and_expedition_collections_do_not_alias_mutable_inputs`, `Persistent_map_order_and_hash_include_every_coordinate`, `FloorLayoutGeneratorTests.Generated_rooms_are_not_mutable_through_the_read_only_view`, `ItemInstanceTests.Instance_does_not_expose_a_mutable_affix_snapshot`, and `MonsterSchemaTests.Instance_does_not_expose_a_mutable_effect_snapshot` pass. | PASS |
| Null and domain-value rules are preserved | `StateCollections.Copy` rejects null elements; specialized setters preserve existing uniqueness checks for equipment, features, knowledge entries, and teleporter relationships. Focused and full boundary tests pass. | PASS |
| Ordering and value semantics remain correct | Player/expedition ordering is copied without normalization; persistent map ordering remains deterministic and value-based; generated rooms and item/monster values remain unchanged. Relevant focused tests pass. | PASS |
| Save round-trip behavior remains correct | `SaveGameSerializerTests.Serialize_and_deserialize_preserves_authoritative_state`, `Save_round_trip_is_deterministic`, migration tests, and the full Release suite pass. | PASS |
| No broad public API redesign was introduced | Public collection property types and save/event contracts remain unchanged. The remediation adds private backing storage and defensive copies at existing boundaries only. | PASS |

### AUD-002 version impact

- Save, simulation, generator, and content versions are unchanged by AUD-002.
- Ordering, values, DTO shapes, events, and migrations are unchanged.
- No migration is required; the change removes aliasing without changing valid
  serialized state.

## Verification evidence

The repository-local SDK was verified by `eng/doctor.ps1` as SDK `8.0.100`.

Commands run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\doctor.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 test tests\Telengard.Architecture.Tests\Telengard.Architecture.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DeterministicRngTests|FullyQualifiedName~FloorLayoutGeneratorTests|FullyQualifiedName~EncounterTriggerTests|FullyQualifiedName~FleeTests|FullyQualifiedName~FeatureOutcomeEngineTests|FullyQualifiedName~ItemAffixTests|FullyQualifiedName~ItemCurseTests|FullyQualifiedName~AltarTests|FullyQualifiedName~PitTests|FullyQualifiedName~GameStateTests|FullyQualifiedName~MonsterSchemaTests"
$env:GIT_CONFIG_COUNT='1'; $env:GIT_CONFIG_KEY_0='core.autocrlf'; $env:GIT_CONFIG_VALUE_0='false'; powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Results:

- Focused audit consumer/ownership tests: **79 passed, 0 failed**.
- Canonical full verification: restore passed, format verification passed,
  Release build passed with **0 warnings and 0 errors**, and **309 Release
  tests passed**.

## Unresolved risks and caveats

- The old text-delimited RNG vectors are intentionally not compatible with the
  new encoder; compatibility is explicit through the version set rather than a
  dual decoder.
- The worktree contains unrelated pre-existing artifacts and a large
  line-ending-only `docs/BUILD_STATUS.md` diff. They are outside this gate's
  scope and must not be folded into the P0 remediation change accidentally.
- The host rejects unsigned local PowerShell scripts under its default policy;
  verification used a process-scoped execution-policy bypass. The repository
  gate itself passed.
