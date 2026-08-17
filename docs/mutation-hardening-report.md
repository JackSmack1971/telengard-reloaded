# Mutation-hardening report

> Historical hardening evidence for the earlier 166-test checkout. The
> current checkout contains additional production code and tests; its current
> coverage result and mutation prerequisite are recorded in
> [test-quality-current-audit.md](test-quality-current-audit.md). Do not use
> the historical 100% coverage statement below as the current gate result.

## Outcome

The hand-written production projects were mutation-tested at Standard,
Advanced, and Complete levels with Stryker.NET 4.14.2, SDK 8.0.100, Release
configuration, the full xUnit project, and no mutation exclusions. The suite
now has 166 passing tests. Only tests and documentation changed; production
behavior, public contracts, save versions, deterministic algorithms, and
architecture boundaries were preserved.

## Baseline versus final

The fresh Standard baseline was run before the hardening changes against 156
tests. The final Complete evidence uses the current Core rerun plus the
all-project Complete run for Content, Save, and Terminal.

| Project | Standard baseline | Final Complete |
| --- | --- | --- |
| Telengard.Core | 1,020 total; 663 killed; 159 survived; 11 timeout; 56 compile error; 131 ignored | 1,046 total; 739 killed; 104 survived; 11 timeout; 60 compile error; 132 ignored; score 87.82% |
| Telengard.Content | 41 total; 23 killed; 8 survived; 10 ignored | 41 total; 27 killed; 4 survived; 10 ignored; score 87.10% |
| Telengard.Save | 141 total; 108 killed; 16 survived; 2 compile error; 15 ignored | 151 total; 122 killed; 12 survived; 2 compile error; 15 ignored; score 91.04% |
| Telengard.Terminal | 0 mutants | 0 mutants |

The Complete score is Stryker's score and excludes compile-error and
covered-block ignored results from its denominator. Reports and the generated
per-mutant audit are under `TestResults/mutation-*`; the final Core audit is
`TestResults/mutation-core-final-complete-3/mutation-audit.json`, and the
all-project audit for the other projects is
`TestResults/mutation-final-complete/mutation-audit.json`.

The Advanced run completed before the final fixed-vector assertion with Core
735/1,046 killed, Content 27/41, Save 122/151, and Terminal 0. The final Core
Complete rerun killed four additional Core survivors, including the expedition
ID mutation after the compatibility-vector test was added.

## Test-quality changes

- Added null, inactive, dead-player, phase, round-boundary, overflow, and
  state-before-event assertions across combat, movement, transitions, and
  death.
- Added exact probability-boundary and deterministic stream-scope tests for
  encounter, flee, and combat behavior.
- Added a fixed expedition-ID vector, preserving the deterministic ID as an
  observable compatibility result.
- Added persistent-map cross-floor retention and visited-implies-observed
  coverage, visibility promotion coverage, and floor-transition boundary
  coverage.
- Added generator floor/size/geometry/connectivity boundary tests.
- Added monster-schema whitespace/normalization validation and save-migration
  tests for both preserving and materializing nullable collections.

## Individually classified remaining survivors

The generated audits contain one row for every mutation, including project,
source line/column, replacement, status, category, and rationale. The 27
survivors below were the only rows conservatively labelled as possible test
weaknesses after the final Core rerun and the final Save run. None is a
defensible observable production-behavior gap.

| Location and mutation | Classification and rationale |
| --- | --- |
| Core `combat/CombatState.cs:85` statement; `:101` statement | Equivalent validation path. Removing either explicit guard still throws before a state change through the constructor/switch boundary; only exception provenance or wording differs. |
| Core `combat/CombatState.cs:146` conditional-false and logical mutations | Equivalent under valid combat states. Early phases have no threat, and the public advance path rejects changing from threat assessment; supported states cannot distinguish the branch. |
| Core `combat/ThreatAssessment.cs:23` string | Exception wording only; diagnostic text is not a game, save, or API contract. |
| Core `Events/DomainEventBus.cs:29` statement | Removing the enumerable item guard immediately reaches the single-event null guard with the same public failure and no mutation. |
| Core `Rng/DeterministicRng.cs:52`, `:53`, `:54`, `:73` bitwise mutations | `>>` and `>>>` are identical for the unsigned `ulong`/`uint` operands used here. |
| Core `Rng/DeterministicRng.cs:68` equality mutation | The differing exact rejection-limit value is private stream state, not injectable through the public deterministic-RNG contract; testing it would couple the suite to implementation state. |
| Core `world/generation/DungeonWalking.cs:86` two equality mutations | The generated layout reserves the outer border as walls, so the changed in-bounds boundary still rejects the destination through the same walkability rule. |
| Core `DungeonWalking.cs:155` `Concat`→`Except` | Retained positions are from other floors and updated positions are from the current floor; the sequences are disjoint, so `Except` cannot change the result. |
| Core `DungeonWalking.cs:155` equality mutation | `PersistentMapState` always promotes visited positions into observed positions. The supported state invariant makes changing only the observed filter unobservable; the cross-floor retention test covers the public result. |
| Core `world/generation/FloorLayoutGenerator.cs:14` statement | Invalid floors still fail at bounded `DungeonPosition` construction; valid floors are covered. |
| Core `FloorLayoutGenerator.cs:18`, `:20` equality mutations | New tile arrays are already initialized to `Wall`, so the mutated fill-loop conditions do not change generated layouts. |
| Core `FloorLayoutGenerator.cs:28` equality mutation | With exactly two rooms, the optional room-0-to-last corridor duplicates the mandatory room-0-to-room-1 corridor. |
| Core `FloorLayoutGenerator.cs:35` statement | The current construction algorithm establishes connectivity; the guard only repeats an internal invariant and removing it does not alter supported generated output. |
| Core `FloorLayoutGenerator.cs:82` equality mutation | The extra failed-placement attempt is an internal failure boundary and does not alter any supported successful layout. |
| Core `FloorLayoutGenerator.cs:139`, `:144` equality mutations | The corridor endpoint is already carved by its room, so excluding it produces the same layout. |
| Core `FloorLayoutGenerator.cs:155`, `:156` equality mutations | Stable multi-seed fingerprints and room/door invariants show no observable difference for supported adjacent-room geometry. |
| Save `Migrations/SaveMigrations.cs:111` `First`→`FirstOrDefault` | `DefaultIfEmpty(1)` guarantees a value, making the operators equivalent. |
| Save `SaveGameSerializer.cs:25` null-coalescing removal | Null migration still becomes the same public `SaveFormatException`; only internal exception provenance/message changes. |

The remaining survived mutations are intentionally unobservable validation
strings: four Content strings at `MonsterDefinition.cs:38`, `:70`, `:95`, and
`:132`, and ten Save strings at `SaveMigrations.cs:20`, `:29`, `:38`, `:48`,
`:54`, `:60`, `:70` and `SaveGameSerializer.cs:26`, `:31`, `:35`.

The other non-killed statuses are explicitly tool categories in the audits:
157 covered-block ignored mutations (Stryker optimization), 11 timeouts, and
62 compile-error mutations. Timeouts are execution/resource outcomes, while
compile errors are mutations that do not produce a runnable program; neither
is evidence of an untested supported behavior. There were no no-coverage
mutations.

## Verification evidence

- Standard, Advanced, and Complete mutation runs completed with the repository
  scripts; final artifacts are retained under `TestResults/`.
- Coverage: 166 tests passed; 1,383/1,383 lines and 616/616 branches (100%).
- Full gate: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\eng\verify.ps1 -Mode Full` passed with a zero-warning Release build and
  166 passing tests.
- No production source file was changed by this hardening pass.
