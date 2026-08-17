# Headless production coverage to 100%

## Purpose / user-visible outcome

The repository can run one repeatable, repository-local coverage command that
executes the complete xUnit suite and reports 100% line and branch coverage for
all hand-written production code in the solution's non-test projects.

## Scope and non-goals

In scope: `Telengard.Core`, `Telengard.Content`, `Telengard.Save`,
`Telengard.Terminal`, and `Telengard.TestHarness`; the xUnit test project;
coverage tooling under `eng/`; and review documentation recording baseline and
final evidence.

Out of scope: `src/Telengard.Godot`, production behavior changes, SDK changes,
production NuGet dependencies, coverage suppression used only to inflate the
metric, and generated/compiler-owned code.

## Sources of truth

- `AGENTS.md`, `docs/AGENT_INDEX.md`, `docs/PLANS.md`
- `Directory.Build.props`, `global.json`, `Telengard.sln`, and `eng/*.ps1`
- hand-written source under `src/` and `tools/`
- xUnit tests under `tests/Telengard.Architecture.Tests/`
- `docs/modern-telengard-spec.md` and `docs/INVARIANTS.md` for behavior

## Current state

The solution contains four source projects, one test project, and the
`Telengard.TestHarness` tool project. The initial coverage baseline for the
current combat-enabled solution was 152 passing xUnit tests, 1,369/1,383 lines
(98.99%), and 584/616 branches (94.81%). The baseline per-file table and final
results are recorded in `docs/BUILD_STATUS.md` and emitted under
`TestResults/coverage/`.

## Invariant impact

No authoritative runtime behavior, public contract, event ordering,
deterministic RNG behavior, renderer boundary, hidden-information rule, or
content separation rule may change. Tests must exercise existing behavior only.

## Save/version impact

None. Coverage configuration is test/tooling metadata and does not change save
DTOs, migrations, or version fields.

## Implementation plan

1. Add a pinned test-only coverage collector and a repository-local PowerShell
   command that emits machine-readable and human-readable line/branch results
   by project and source file, with production/test assembly boundaries clear.
2. Run the clean baseline and record its measured per-project/per-file gaps.
3. Add the smallest behavior-focused tests for every uncovered line and
   branch, including tool/presentation entry points where they are in scope.
4. Iterate with focused tests, then run the complete suite and coverage command.
5. Run the full repository verification gate and document commands and final
   results; inspect the final diff for scope drift.

## Validation

- focused tests through `eng/dotnet.ps1`
- repeatable coverage command under `eng/`
- complete xUnit suite through `eng/dotnet.ps1`
- `eng/verify.ps1 -Mode Full`

## Progress

- [x] Read the task and repository routing instructions.
- [x] Confirm current worktree and solution scope.
- [x] Establish that the unmodified coverage command has no collector.
- [x] Add coverage tooling and capture numeric baseline.
- [x] Close all hand-written production line/branch gaps.
- [x] Run final clean coverage, full tests, and verification gate.
- [x] Document baseline-to-final evidence.

## Surprises / discoveries

- PowerShell script execution policy blocks direct `./eng/*.ps1` invocation in
  this environment; a process-level `-ExecutionPolicy Bypass` is needed to run
  the repository wrapper without changing machine policy.
- `Telengard.Godot` is not part of `Telengard.sln`.

## Decision log

- Use a pinned test-only collector; adding production dependencies is not
  required and is prohibited by scope.
- Keep the coverage boundary explicit and fail the command when an in-scope
  hand-written production file is not represented by the report.

## Results / remaining work

Completed 2026-08-16. `eng/coverage.ps1` runs the complete 156-test suite and
reports 1,383/1,383 lines and 616/616 branches for the in-scope hand-written
production code. `eng/verify.ps1 -Mode Full` passed restore, format, Release
build, and Release tests with zero warnings/errors. The no-commit checkout
case was fixed in `eng/common.ps1` so the verification fingerprint works
before the first commit. No required follow-up remains.
