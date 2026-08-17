# Phase 2 acceptance gate

Date verified: 2026-08-16

## Result

**FAIL** — the successful expedition loop and suspend/save resume path are
implemented, and TEL-037 now provides a failure/death transition. The gate
still fails because mode-specific death wealth policy and the remaining
expedition-statistic producers are not implemented.

## Evidence

| Acceptance condition | Evidence | Result |
| --- | --- | --- |
| Start at the inn and start an expedition | `Phase2AcceptanceTests.Successful_loop_secures_gold_finishes_and_allows_another_expedition` and `ExpeditionStateTests.Entering_the_dungeon_starts_a_deterministic_expedition`. | PASS |
| Acquire underground gold into `carried_gold`, not `secured_gold` | `CarriedGoldTests.Acquiring_gold_updates_the_carried_pool_and_emits_a_committed_event` and the end-to-end Phase 2 test assert carried mirrors change while existing secured gold remains unchanged. | PASS |
| Reach safety and transfer eligible wealth | `DungeonWalkingTests.Returning_to_the_inn_secures_carried_gold` and `Phase2AcceptanceTests.Successful_loop_secures_gold_finishes_and_allows_another_expedition` verify the floor-1 entrance boundary, zero carried gold after commit, and the exact secured total/event. | PASS |
| Dungeon wealth remains unsecured before safety | Acquisition changes only player/expedition carried gold; securing occurs only after `DungeonWalkingResolver.Leave` validates the safety boundary. | PASS |
| Failed expedition does not secure carried wealth | `Phase2AcceptanceTests.Failed_safety_boundary_attempt_does_not_secure_carried_gold` proves a rejected return does not mutate wealth. `DeathTests.Death_marks_the_player_and_fails_the_expedition_after_commit` proves death ends the expedition without changing secured wealth; carried-gold loss/retention by mode remains a later policy decision. | PARTIAL |
| Suspend/resume preserves expedition state without securing it | `ExpeditionSuspensionTests.Suspension_is_deterministic_and_the_saved_state_resumes_identically` and the Phase 2 save/load test preserve active state, carried gold, and secured gold across suspension/save/load. | PASS |
| Expedition statistics remain consistent | `Phase2AcceptanceTests.Floor_statistics_remain_consistent_through_a_return` verifies starting floor, deepest floor, visited-floor history, and current zero-valued counters through return; the remaining counters have no gameplay update producers yet. | PARTIAL |
| Save/load during an expedition reproduces state | `Phase2AcceptanceTests.Save_load_during_an_expedition_preserves_the_authoritative_resume_state` verifies byte-stable serialization after underground acquisition, including active state and both wealth pools. | PASS |
| Finish the expedition after returning safely | `DungeonWalkingResolver.Leave` sets `Inn.IsAtInn`, clears carried gold, secures it, sets `Expedition.Active = false`, and emits `ExpeditionSucceededEvent`. | PASS |
| Start another expedition | The end-to-end Phase 2 test enters again after successful return and verifies a fresh active expedition at floor 1 with zero carried gold. | PASS |
| Do not start encounter implementation | Review found no encounter implementation added by this gate work. | PASS |

## Invariants and scope

- Authoritative transitions remain in `Telengard.Core`; presentation and save
  code do not own gameplay state.
- Carried and secured wealth remain distinct, and securing occurs only inside
  the validated return-to-inn resolver.
- Existing expedition IDs and floor tracking remain deterministic.
- No encounter, death-mode, XP formula, or anti-save-scumming policy was
  invented during this review. TEL-037's common death transition does not
  choose a mode-specific carried-wealth formula.
- TEL-024 completion and TEL-025 suspension are implemented and verified. The
  remaining Phase 2 blockers are mode-specific death wealth policy and runtime
  producers for the remaining expedition counters.

## Verification

Command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 test Telengard.sln --configuration Release --logger "console;verbosity=normal"
```

Result: 101 tests passed, 0 failed at the time of the original gate. A green
suite does not constitute a Phase 2 pass because mode-specific death policy
and the remaining expedition counters have no runtime update path.
