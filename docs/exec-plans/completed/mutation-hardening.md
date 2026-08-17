# Mutation-hardening test audit

## Purpose / user-visible outcome

The hand-written production simulation and save code is protected by behavior-level tests strong enough that no actionable mutant survives Stryker.NET at Complete mutation level. The clean xUnit suite, full line/branch coverage, and repository verification remain green.

## Scope and non-goals

- Scope: `Telengard.Core` and `Telengard.Save` hand-written production code covered by the existing Stryker runner; `Telengard.Terminal` is checked and has no applicable mutants.
- Strengthen or add xUnit tests only when a surviving mutant represents an observable behavior or invariant gap.
- Document equivalent, unobservable, and tooling-only survivors individually; do not alter production behavior, public contracts, mutation exclusions, or coverage gates.
- No new gameplay, persistence schema, RNG semantics, dependencies, or renderer behavior.

## Sources of truth

- `AGENTS.md`, `docs/AGENT_INDEX.md`, `docs/INVARIANTS.md`, and `docs/ARCHITECTURE.md`.
- `TestResults/mutation/mutation-audit.json` and project Stryker reports from the Standard baseline.
- Existing production code and `tests/Telengard.Architecture.Tests`.
- `eng/mutation.ps1`, `eng/coverage.ps1`, and `eng/verify.ps1`.

## Current state

The 2026-08-15 Standard baseline reports Core 366/667 killed with 159 survivors and Save 84/117 killed with 17 survivors. The audit classifies 135 as actionable, but that classification is provisional and includes statement, exact-boundary, and deterministic-output mutants requiring review. Coverage artifacts report 883/883 lines and 388/388 branches with 79 passing tests.

## Invariant impact

Tests must preserve and verify simulation authority, validate-before-mutate behavior, deterministic RNG and generation replay, stable map ordering, floor/walking boundaries, carried-versus-secured wealth, and explicit save DTO/migration compatibility. No production authority or event contract changes are in scope.

## Save/version impact

None. Save DTOs, migrations, and version values remain unchanged; migration tests may be strengthened only to verify existing compatibility behavior.

## Implementation plan

1. Establish the baseline report and map each survivor to source, current tests, and an observable contract.
2. Add the smallest focused test assertions for actionable survivors, reusing existing fixtures and `Telengard.TestHarness` where appropriate.
3. Rerun focused tests and Standard mutation scopes, then classify remaining survivors individually.
4. Rerun Advanced and Complete mutation levels with the same audit process.
5. Run coverage and `./eng/verify.ps1 -Mode Full`, review the final diff, and record baseline/final mutation results plus justified survivors.

## Validation

- Focused xUnit filters for each changed behavior.
- `./eng/mutation.ps1 -Project Telengard.Core` and `./eng/mutation.ps1 -Project Telengard.Save` during iteration.
- Standard, Advanced, and Complete Stryker runs using the pinned SDK and repository-local tool.
- `./eng/coverage.ps1`.
- `./eng/verify.ps1 -Mode Full`.

## Progress

- [x] Read the objective, repository guidance, and Standard baseline.
- [x] Audit and classify every baseline survivor.
- [x] Strengthen/add behavior-level tests for actionable survivors.
- [x] Complete Advanced and Complete mutation validation.
- [x] Run final gates and publish the audit report.

## Surprises / discoveries

- The checkout has no Git `HEAD`; all repository files are currently untracked, so no existing user changes can be distinguished by history.
- The baseline’s generated audit labels all non-killed non-string survivors as actionable; this is not sufficient evidence for equivalence or observability and must be corrected by source/test review.

## Decision log

- Keep the existing mutation runner and test framework; the requested hardening is a test-quality task and adding infrastructure would widen scope.
- Treat exact output tests as valid only where the output is a documented deterministic or persistence contract; otherwise assert durable invariants.

## Results / remaining work

Completed 2026-08-15. The suite increased from 79 to 92 tests and retained
100% line and branch coverage. Final Complete mutation evidence is Core
483/683 killed and Save 96/127 killed, with every remaining non-killed result
classified in `docs/mutation-hardening-report.md` and the generated audit.
`eng/verify.ps1 -Mode Full` passed with zero build warnings/errors and 92
passing Release tests. Move this plan to `docs/exec-plans/completed/` when
repository plan housekeeping is performed.
