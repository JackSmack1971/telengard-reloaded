---
name: telengard-next-slice
description: Select and execute the next safe, logical Telengard Reloaded implementation slice end to end. Use when asked to continue the project, determine what to build next, implement the next TEL ticket, advance the Five-Floor MVP Demo, or run the repository's autonomous development loop.
---

# Telengard Next Slice

Execute exactly one repository slice as a fresh transaction. Follow root `AGENTS.md` completely.

## Objective

Advance the nearest project milestone by selecting the best **eligible** next slice from repository evidence, implementing only that slice, validating it, independently reviewing it, updating durable knowledge, and creating/merging a PR only when every repository merge gate is satisfied.

Never continue into a second implementation slice in the same run.

The current convergence is the **Five-Floor MVP Demo** in `docs/MVP_DEMO.md`.
The intended critical path is TEL-129 → TEL-130 → TEL-131 → TEL-132. TEL-126,
TEL-127, and TEL-128 are post-MVP work and must not pre-empt that sequence unless
current evidence proves a hard prerequisite, the MVP chain is blocked, or the
user explicitly changes priority. The generated task index retains `core-alpha`
as a compatibility identifier; current product priority comes from this document,
`docs/MVP_DEMO.md`, the ledger, and ticket completion state. Do not create
systematic final-asset batches before TEL-128 passes.

When delegation is needed, follow `docs/CODEX_MODEL_POLICY.md` and use the
lowest role-appropriate effort.

## 1. Establish current state

From repository root:

1. Read `AGENTS.md` and this skill.
2. Read generated `docs/tasks/index.json` for status, dependencies, risk,
   decision state, context, review, and verification metadata.
3. While TEL-132 is unfinished, read `docs/MVP_DEMO.md` and treat the Five-Floor
   MVP Demo as the active product checkpoint.
4. Inspect current branch/worktree, remote `main`, recent commits, open PRs, and
   CI; detect overlapping/stale work.
5. If a candidate requires manual Godot verification, run
   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/godot-doctor.ps1`
   and use its discovered executable/version as the availability evidence.

Do not load the full ledger, invariants, architecture, or umbrella plans merely
to build the initial candidate set. If the generated index may be stale, run
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/task-index.ps1 -Mode Check`.

Do not overwrite, reset, stash, or absorb unrelated user changes without explicit authorization.

## 2. Build the candidate set

Extract plausible `not_started` work from `docs/tasks/index.json` plus explicit
active remediation. Narrow to at most two serious candidates before loading
full ticket prose.

While TEL-132 is unfinished and the repository documents the Five-Floor MVP Demo
as current, TEL-129–TEL-132 are the preferred candidate family. A ticket outside
that family may pre-empt only for a hard prerequisite, repository-health blocker,
active overlap, or explicit user reprioritization.

Reject a candidate when:

- dependencies are incomplete;
- an active PR already owns it;
- acceptance requires an undefined product/tuning decision;
- scope is too broad for one reviewable transaction;
- another prerequisite is more fundamental;
- repository health prevents trustworthy validation;
- required manual observation is unavailable with no approved substitute;
- it is post-MVP breadth while an eligible MVP ticket is ready; or
- it is a final-asset batch before TEL-128 passes.

Rank remaining work by active-milestone continuity, MVP blocker value, dependency
leverage, testability, reviewable size, then architecture/save risk. Record only
a concise evidence-based selection rationale.

## 3. Read the selected slice deeply

Read the selected ticket, its YAML context manifest when present, all
`context.required` paths, applicable conditional context from the generated
risk mapping, directly relevant spec/ADR material, adjacent code/tests, and
recent subsystem history when useful.

For manifest-backed tickets, missing or contradictory required context is a hard
stop. Legacy tickets use the index and existing ticket sections as fallback.
Discover extra evidence just in time; do not return to a read-everything default.

New/materially revised tickets use this shape:

```yaml
---
id: TEL-###
status: not_started
depends_on: []
context:
  required: []
  conditional: {}
risk: []
review:
  required: [correctness]
  conditional: [tests]
verification:
  headless: true
  godot_manual: false
---
```

Use `docs/PLANS.md` to decide whether an ExecPlan is required. TEL-129–TEL-132
use the existing `docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md`.

## 4. Plan the transaction

Create a short plan covering selected TEL/outcome, scope/non-goals, current gap,
likely modules/types/events, determinism/hidden-information impact, save/version
impact, focused tests, final verification, and durable documentation updates.

If implementation requires a new unresolved product decision, stop as blocked.

## 5. Prove the gap

For bugs, reproduce the failure where practical. For net-new domain work, add or
identify the smallest acceptance-level proof. Do not force a red-test ritual for
docs/tooling-only work when it adds no information.

## 6. Implement minimally

Implement only the selected slice and required support. Preserve:

- renderer-independent simulation authority;
- validation before mutation and committed event ordering;
- deterministic scoped RNG;
- explicit save DTO/migration/version discipline;
- content/runtime/presentation separation;
- hidden-information boundaries;
- carried versus secured progress; and
- configurable unresolved tuning instead of invented permanent policy.

For the Five-Floor MVP, fixed seed/demo setup and explicit temporary tuning are
allowed only where the MVP docs/ticket permit them; they must remain visible and
replaceable.

Do not bundle adjacent TEL tickets for convenience.

## 7. Test from narrow to broad

Run, as applicable:

1. directly affected tests;
2. subsystem/project tests;
3. formatter/build checks;
4. ticket-specific validation;
5. required Godot/manual observation; and
6. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/verify.ps1 -Mode Full`
   for code changes.

If generated status/audit views change, use their repository generator/check.
Capture exact pass/fail evidence.

## 8. Observe relevant surfaces

Exercise Terminal/Godot/UI/CLI/integration behavior when the selected ticket
changes it. Headless projection tests do not replace required manual Godot
observation. Use `godot-doctor.ps1` before reporting the runtime unavailable.

TEL-132 requires the real fixed-seed floor-1-through-floor-5 Godot route; a
headless-only proof cannot pass its gate.

## 9. Review

Self-review the full branch diff against `main` for scope, correctness,
invariants, determinism, save/versioning, tests, docs, generated output, secrets,
and accidental artifacts.

Then invoke `$telengard-review` on the final candidate diff. Pass only the
selected ticket's risk tags, changed paths, base/head refs, lane objective,
required lane-specific docs, and finding schema. Resolve every actionable
finding and rerun affected checks/review lanes after material fixes.

## 10. Update durable knowledge

Only after implementation evidence exists, update the selected ticket and human
ledger. Update BUILD_STATUS/ExecPlan/ADR/README/DEVELOPMENT/INVARIANTS/generated
status only when their documented role requires it.

For TEL-129–TEL-132, update the Five-Floor MVP ExecPlan/gate as applicable. Do
not mark TEL-127 or TEL-128 complete merely because the MVP passes. Status must
match reality; documentation alone never implements behavior.

## 11. Create and gate the PR

Prefer branch `agent/<ticket>-<short-slug>`. PR body includes intent, changed
scope, non-goals/deferred decisions, save/version impact, exact validation, and
independent review disposition.

Before merge require:

- local final verification passed when required;
- GitHub `Full verification` green for the reviewed head;
- no required check pending/failing;
- PR mergeable;
- no unresolved actionable review finding;
- docs/generated state consistent; and
- no invented unresolved product/tuning policy.

If any gate is uncertain, leave the PR open and report `ready-for-human`. Never
force/direct-merge around repository gates.

## 12. Handoff and stop

After merge inspect new `main` only enough to identify likely next candidates and
repository health. Do not implement another slice.

While the Five-Floor MVP is active, `next_candidates` should normally advance to
the next unfinished TEL-129–TEL-132 ticket. Only after TEL-132 passes should
TEL-126/TEL-127/TEL-128 return to the normal candidate set.

End with exactly one block:

```text
RUN_RESULT
status: merged | ready-for-human | blocked | no-work
selected: TEL-### | none
pr: <number-or-none>
merge_sha: <sha-or-none>
verification: <concise evidence>
next_candidates: <comma-separated TEL ids or none>
blockers: <concise blockers or none>
```

## Skill-local telemetry

Use `scripts/skill_telemetry.py` as best-effort, non-gating instrumentation.
Start one session before candidate selection, record normalized workflow events
(candidate set/rejection/selection, context or Godot blockers, review results,
merge-gate outcomes, and any second-slice violation), and end with the same
terminal disposition as `RUN_RESULT`. Never log prompts/private reasoning,
source/diff contents, command output, credentials, environment variables, or API
payloads. Use the helper's `run`/`probe` wrappers only for consequential checks;
telemetry failure never relaxes verification, review, or merge gates.
