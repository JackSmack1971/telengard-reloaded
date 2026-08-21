# Current test-quality audit

## Scope and environment

- Checkout state: no committed `HEAD`; repository files are untracked in the
  current workspace.
- SDK pin: `global.json` specifies `8.0.100`.
- Observed SDK: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\eng\dotnet.ps1 --version` → `8.0.100`.
- Production metric scope: `Telengard.Core`, `Telengard.Content`,
  `Telengard.Save`, `Telengard.Terminal`, and the existing
  `Telengard.TestHarness` coverage scope. The Godot placeholder is outside
  `Telengard.sln`.
- Test project: `tests/Telengard.Architecture.Tests`.
- No production code or production dependency was changed in this pass.

## Commands and results

Focused test checkpoint:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\dotnet.ps1 test tests\Telengard.Architecture.Tests\Telengard.Architecture.Tests.csproj --configuration Release --no-restore --logger "console;verbosity=minimal"`

Result: 266 passed, 0 failed.

Coverage checkpoint:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\coverage.ps1 -Configuration Release`

Result: test execution passed, but the coverage gate correctly failed at
`eng/coverage.ps1:172` because the current checkout is not yet at 100%.
The latest report is under `TestResults/coverage/`.

| Boundary | Lines | Branches |
| --- | ---: | ---: |
| In-scope aggregate | 2,756/2,758 (99.93%) | 1,194/1,206 (99.00%) |

The uncovered production lines are two defensive fallthrough lines. All
remaining deficits are branch probes in `Equipment.cs`, `FeatureSystem.cs`,
`FeatureOutcomeEngine.cs`, and `SaveMigrations.cs`.

## Evidence-driven test changes

`tests/Telengard.Architecture.Tests/CoverageBoundaryTests.cs` adds focused
behavior tests for item/result validation, state collection uniqueness,
feature variants and events, knowledge/mapping transitions, content-engine
stream overloads, DTO/converter compatibility, and save validation/migration
boundaries. Coverage improved from the fresh pre-change result of 2,706/2,758
lines (98.11%) and 1,093/1,206 branches (90.63%) to the current result above.

## Remaining blockers

These are recorded rather than reached through reflection, malformed
stateful test doubles, production changes, or coverage exclusions:

- `src/Telengard.Content/Features/FeatureOutcomeEngine.cs:67` and the
  loop branch at `:58`: eligible outcomes are required to be non-empty and
  `NextLong(0, totalWeight)` guarantees a value below the cumulative total.
  Reaching the throw requires violating the public RNG/list contract.
- `src/Telengard.Core/world/features/FeatureSystem.cs:264` and the missing
  switch branch at `:231`: `FeatureOutcomeKind` is private and all enum values
  are selected by the public activation methods; the default is unreachable
  through the public API.
- `src/Telengard.Core/world/features/FeatureSystem.cs:287`: public
  teleporter activation rejects a null destination before `ApplyOutcome`.
  Reaching the null-coalescing throw requires bypassing that validation.
- `src/Telengard.Save/Migrations/SaveMigrations.cs:79`: nullable source and
  destination key components are rejected by the preceding mapping predicate,
  so the corresponding null-propagation alternatives cannot reach the later
  `GroupBy` through a valid save DTO.
- Residual compiler/LINQ branch probes remain at
  `src/Telengard.Core/items/Equipment.cs:10` and `:128`, and the compound
  equipment predicate at `src/Telengard.Save/Migrations/SaveMigrations.cs:53`.
  Direct null/empty/non-empty, first-match, later-match, duplicate, and
  valid-null-equipment cases were exercised; the remaining probes do not
  represent a distinct supported behavior.

Mutation hardening is not advanced in this checkout because the required
100% line-and-branch coverage prerequisite is not satisfied. The existing
`TestResults/mutation-baseline/` artifacts remain untouched.

## TEL-117 tooling-scope verification

- Implementation anchor: commit `70c8f51b415c45d9b98928dc0478304ae94ac947`,
  anchored with `IsDirty: true`. Implementation started from a clean isolated
  worktree at that commit; the original dirty worktree was not used as the
  implementation base.
- Coverage command: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\tests\Tooling\TEL-117.Tooling.Tests.ps1`.
- Coverage evidence on the merge-target branch: production totals are reported
  separately as 3,344/3,393 lines and 1,475/1,556 branches; TestHarness is
  visible as test-support at 42/42 lines and 32/32 branches and is excluded
  from the production gate. The production gate therefore returned nonzero,
  preserving the existing failure behavior for incomplete production coverage.
- Mutation evidence: both guarded default-baseline forms returned nonzero.
  A separate Basic `Telengard.Terminal` run succeeded with
  `--since=dadb01c253ee7a4e05c5cc385a1f13ed45493b94` in
  `TestResults/mutation-tel-117-scoped`; the generated manifest recorded the
  argument and effective project scope.
- The current operational documentation now describes the production versus
  test-support coverage boundary and the scoped mutation-directory rule.
- Default mutation command: `powershell.exe -NoProfile -ExecutionPolicy
  Bypass -File .\eng\mutation.ps1 -MutationLevel Basic` completed for all four
  production projects in `TestResults/mutation-baseline`; its manifest recorded
  an empty additional-arguments list and the unchanged default scope.
- Final merge-target repository gate: `powershell.exe -NoProfile
  -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full` passed with a
  zero-warning Release build and 338/338 Release tests. The clean worktree required the process-scoped
  `core.autocrlf=false` override and an ignored `.codex` stamp directory.
