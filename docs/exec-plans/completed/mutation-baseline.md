# Mutation-testing baseline

## Purpose / user-visible outcome

The repository has a repeatable, repository-local Stryker.NET command that
measures the existing tests against every applicable hand-written production
project and preserves raw machine-readable and human-readable reports.

## Scope and non-goals

- Scope: `Telengard.Core`, `Telengard.Content`, `Telengard.Save`, and
  `Telengard.Terminal`, using `tests/Telengard.Architecture.Tests` as the
  test project.
- `Telengard.Content` is included because `Monsters/MonsterDefinition.cs` is
  hand-written production C#; `tools/Telengard.TestHarness` remains a tool,
  not a production mutation target.
- The first baseline must not change production code or strengthen tests.
- No mutation threshold is enforced yet; surviving mutants are evidence for a
  later test-hardening phase.

## Sources of truth

- `AGENTS.md`, `docs/AGENT_INDEX.md`, `docs/INVARIANTS.md`, and
  `docs/ARCHITECTURE.md`.
- `eng/coverage.ps1`, `eng/verify.ps1`, the solution, project files, and the
  existing architecture tests.
- Official Stryker.NET 4.x configuration, operating-mode, reporter, and
  migration documentation.

## Current state

The repository pins SDK `8.0.100`, has one xUnit test project, and reports
100% line and branch coverage for its existing coverage scope. The local
Stryker.NET 4.14.2 tool, per-project configurations, runner, and baseline
reports now exist under `TestResults/mutation-baseline/`.

## Invariant impact

This is test/tooling and documentation metadata only. It does not change
simulation authority, command/event ordering, deterministic RNG, hidden
information, content boundaries, or renderer behavior.

## Save/version impact

None.

## Implementation plan

1. Pin the local Stryker.NET 4.14.2 tool and add explicit configs for each
   applicable production source project.
2. Add `eng/mutation.ps1` to restore the local tool, run Standard mutation
   testing per project, and aggregate the resulting reports without imposing a
   score gate.
3. Run the baseline against the existing suite; record every project’s
   report, score, mutant statuses, and justified scope decisions.
4. Run the full build, xUnit suite, and 100% coverage gate.

## Validation

- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\mutation.ps1`
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\coverage.ps1`
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full`

## Progress

- [x] Read the task and repository instructions.
- [x] Confirm the pinned SDK and existing project/test graph.
- [x] Select and install the .NET 8-compatible local Stryker tool.
- [x] Add configurations and runner.
- [x] Run and audit the Standard baseline across Core, Content, Save, and
  Terminal.
- [x] Run final verification and close the plan.

## Surprises / discoveries

- The repository has no `.gitignore`; generated reports must therefore be
  placed deliberately and must not be mistaken for source changes.
- Stryker’s source-project mode supports one or more explicit test projects,
  which matches the current one-test-project topology.
- Terminal has no mutatable statements. Its project configuration disables
  coverage analysis because Stryker cannot capture coverage for a zero-mutant
  project; a clean Terminal-only run still emits all three reports.

## Decision log

- Use Stryker.NET 4.14.2 rather than the current 5.x line because the pinned
  repository environment contains only the .NET 8 SDK/runtime.
- Keep mutation score thresholds at a non-failing baseline and do not add
  ignore rules; the first run must expose existing weaknesses.

## Results / remaining work

Completed 2026-08-16. `eng/mutation.ps1` ran Stryker.NET 4.14.2 Standard
mutation testing against Core, Content, Save, and Terminal with the pinned
SDK's MSBuild path and produced per-project JSON/Markdown/HTML reports,
machine-readable summary/audit/manifest files, and a 408-entry audit. Core
reported 1,020 mutants (663 killed, 159 survived, 11 timeouts, 56 compile
errors, 131 ignored); Content reported 41 (23 killed, 8 survived, 10
ignored); Save reported 141 (108 killed, 16 survived, 2 compile errors, 15
ignored); Terminal reported 0. The run used the existing 156-test suite and
no mutation exclusions. Coverage remains 1,383/1,383 lines and 616/616
branches; follow-up test strengthening is intentionally outside this
baseline, and the audit retains actionable survivors for later review.
