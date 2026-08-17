# Item-system architecture gate (TEL-060–TEL-065)

Date verified: 2026-08-16

## Result

**PASS** — item definitions, runtime instances, deterministic affix/curse
generation, identification redaction, and fixed equipment slots remain within
the renderer-independent simulation/content boundaries.

## Evidence

| Gate condition | Evidence | Result |
| --- | --- | --- |
| `ItemDefinition` and `ItemInstance` are distinct | `ItemDefinition` in `src/Telengard.Content/Items/ItemDefinition.cs` contains content identity, base properties, pools, rarity/depth rules, and unidentified naming. `ItemInstance` in `src/Telengard.Core/items/ItemInstance.cs` contains instance identity and generated runtime state. `ItemSchemaTests.Definition_copies_mutable_inputs_and_has_no_runtime_state` and `ItemInstanceTests.Instance_serializes_runtime_state_without_content_definition_fields` verify the separation. | PASS |
| Generated instance state can serialize | `ItemInstanceDto` in `src/Telengard.Save/Dto/ItemInstanceDto.cs` is the explicit persistence boundary for generated affixes, curse, identification, and durability. `ItemInstanceTests.Generated_instance_state_round_trips_through_the_explicit_save_dto` verifies a JSON round trip without serializing the runtime object as the save contract. | PASS |
| Unidentified state does not leak known properties | `ItemInstance.ToObservedState()` returns an opaque observation with no definition id, generated affixes, curse, or durability until identification. `ItemIdentifiedEvent`, `ItemAffixesGeneratedEvent`, and `ItemCursedEvent` expose only instance identity. `ItemInstanceTests.Unidentified_observation_redacts_generated_properties_until_identification` and the affix/curse event assertions verify the boundary. | PASS |
| Affixes use deterministic RNG | `ItemAffixEngine` selects without replacement from the definition pool using either a supplied scoped `DeterministicRngStream` or a stream derived from world seed, content version, definition id, and item instance id under the `item-affixes` scope. `ItemAffixTests.Content_selection_is_without_replacement_and_replays_from_stable_inputs` and generation integration tests verify stable output and pool membership. | PASS |
| Curses use the same deterministic rules | `ItemCurseEngine` derives the equivalent stable inputs under the separate `item-curses` scope and applies the result through the Core resolver. `ItemCurseTests.Content_selection_replays_from_stable_inputs_and_uses_the_curse_pool` and `Generated_curse_replays_with_affixes_from_the_same_stable_item_inputs` verify replay and separation from affix selection. | PASS |
| Equipment slots do not create inventory-Tetris behavior | `EquipmentSlotState` stores only a stable slot id and an optional item instance id. There are no dimensions, coordinates, packing, weight-grid, or placement calculations. `EquipmentTests.Slot_state_validates_identity_and_is_immutable`, `Player_state_rejects_duplicate_slots_and_duplicate_equipped_items`, and command-boundary tests cover fixed-slot behavior. | PASS |
| Content definitions remain data-driven | Item names, categories, base properties, affix/curse pools, rarity rules, depth rules, and unidentified names are supplied through `ItemDefinition`; generation engines consume those definitions rather than duplicating content tables. `ItemSchemaTests.Definition_preserves_the_specified_content_fields` and `ItemAffixTests`/`ItemCurseTests` verify definition-driven selection. | PASS |
| Item generation does not depend on renderer code | Generation is split between `Telengard.Content` definitions/selection and `Telengard.Core` commands/resolvers. Core has no Godot, Terminal, or renderer reference; `DependencyBoundaryTests.Core_does_not_reference_presentation_or_engine_assemblies` passes. | PASS |

## Invariants and scope

- Authoritative item transitions are immutable Core transitions and emit only
  committed, opaque domain events.
- Stable generation uses the existing deterministic RNG service. Affix and
  curse streams are named separately, so unrelated consumption cannot alter
  the other result.
- The new item DTO is additive and is not wired into the existing `GameState`
  save shape; no save-version migration was required by this gate. Existing
  save DTOs and version metadata remain unchanged.
- Equipment behavior is slot-based only. Inventory capacity, item stacking,
  and other balance/formula decisions remain outside this gate and are
  `CONFIGURATION/TUNING DECISION REQUIRED` where applicable.
- Curse and affix identifiers are content tags. Their gameplay effects remain
  unspecified by the cited design sections and are not invented here.

## Verification

Commands:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 test Telengard.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Results:

- Focused item filter: **31 passed, 0 failed**.
- Full solution test run: **258 passed, 0 failed**.
- Full verification: **passed** — restore, format verification, Release build
  with 0 warnings/0 errors, and Release tests.
