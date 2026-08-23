---
name: telengard-next-slice
description: Select and execute the next safe, logical Telengard Reloaded implementation slice end to end. Use when asked to continue the project, determine what to build next, implement the next TEL ticket, advance Core Alpha, or run the repository's autonomous Codex development loop through planning, implementation, testing, review, documentation, PR, and gated merge.
---

# Telengard Next Slice

Execute exactly one repository slice as a fresh transaction. Follow the root `AGENTS.md` completely.

## Contents

- Establish compact current state and candidate set
- Load selected-ticket context and plan one transaction
- Implement, test, observe, review, document, and gate one slice
- Emit the final handoff and stop

## Objective

Advance the nearest project milestone by selecting the best **eligible** next slice from repository evidence, implementing only that slice, validating it, independently reviewing it, updating durable knowledge, and creating/merging a PR only when every repository merge gate is satisfied.

Never continue into a second implementation slice in the same run.

The current convergence is representative floors 1–5 content plus a
production-shaped Godot placeholder/graybox client, followed by TEL-127
Playable Godot Vertical Slice acceptance and TEL-128 Art Production Ready.
Do not create systematic final-asset batches before TEL-128 passes.

When delegation is needed, follow `docs/CODEX_MODEL_POLICY.md`. Use the
lowest role-appropriate effort, keep `high` for demonstrated ambiguity or
high-impact boundaries, and do not inherit a parent's `high` setting by
default.

## 1. Establish current state

From repository root, keep the pre-selection context deliberately small:

1. Read `AGENTS.md` and this skill.
2. Read generated `docs/tasks/index.json` and query its ticket status, track,
   dependencies, risk tags, decision state, context, review, and verification
   fields.
3. Inspect current branch/worktree, remote `main`, recent commits, open PRs,
   and CI state.
4. Detect overlapping/stale work before choosing a ticket.

Do not load the full human task ledger, invariants, development guide,
architecture, blueprint, or ExecPlan merely to construct the initial candidate
set. If the generated index is missing or stale, run
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/task-index.ps1 -Mode Check`;
regenerate only when the source ledger is intentionally being updated.

If unrelated user changes are present, do not overwrite, reset, stash, or absorb them without explicit authorization. Prefer an isolated worktree/branch when available.

## 2. Build the candidate set

Extract plausible `not_started` work from `docs/tasks/index.json` plus any
explicit active remediation work. Rank from the compact metadata before opening
ticket prose. Narrow to at most two serious candidates, then inspect their
ticket files, declared dependencies, and active PR ownership.

Reject a candidate when:

- dependencies are incomplete;
- an active PR already owns it;
- acceptance criteria require an undefined product/tuning decision;
- the slice is too broad to review coherently;
- another prerequisite is visibly more fundamental;
- current repository health prevents trustworthy validation.
- required Godot/manual observation cannot be performed and no approved
  substitute exists;
- it is a final-asset batch before TEL-128 / `ART-PRODUCTION-READY` passes.

Rank remaining candidates by:

1. continuity with the most recently completed coherent feature chain;
2. Core Alpha blocker value;
3. dependency leverage;
4. ability to prove the behavior with focused tests;
5. small/reviewable scope;
6. architectural and save/version risk.

Choose one. Record only a concise evidence-based selection rationale, not private reasoning.

## 3. Read the selected slice deeply

Read:

- the selected ticket;
- its machine-readable YAML context manifest, when present;
- every `context.required` path declared by the ticket;
- only applicable `context.conditional` paths for the selected risk domains,
  using `docs/tasks/index.json`'s `conditional_context_by_risk` mapping;
- directly referenced specification sections when needed;
- relevant architecture/ADR material;
- existing implementation and tests for adjacent behavior;
- recent commits/PRs in the same subsystem when useful.

For tickets that declare a context manifest, treat missing or contradictory
required context as a hard stop. Apply the index risk-to-context mapping before
conditional loading; an unmapped risk tag requires just-in-time inspection.
Legacy tickets without a manifest use the generated index and existing ticket
sections as a compatibility fallback. Discover additional evidence just-in-time
when the selected ticket or code requires it; do not restore the old
read-everything default.

New or materially revised tickets should put this manifest near the top of the
ticket (after its title/status when those remain human-facing):

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

The manifest is required for new or materially revised tickets. Legacy tickets
without it use the generated index and existing sections as a compatibility
fallback; absent context is never permission to skip relevant evidence.

Determine whether `docs/PLANS.md` requires an ExecPlan. Create/resume one before implementation if required.

## 4. Plan the transaction

Create a short implementation plan with:

- selected TEL id and intended outcome;
- scope/non-goals;
- current gap or bug reproduction;
- modules/types/events likely affected;
- determinism and hidden-information considerations;
- save/version impact, including `none` when truly none;
- focused tests;
- canonical final verification;
- documentation/status updates.

If evidence reveals that the ticket is not actually implementable without a new product decision, stop with `RUN_RESULT status: blocked`.

## 5. Prove the gap

For bugs, reproduce the current failure/behavior where practical.

For net-new domain work, add or identify the smallest acceptance-level test that demonstrates the missing behavior. Run focused tests before implementation when that produces meaningful evidence.

Do not force a red-test ritual for docs-only or tooling-only changes where it adds no information.

## 6. Implement minimally

Implement only the selected slice and required supporting changes.

Preserve all root `AGENTS.md` contracts. In particular:

- renderer-independent simulation authority;
- validation before mutation;
- event ordering after committed state changes;
- deterministic scoped RNG;
- explicit save DTO/migration/version discipline;
- content/runtime/presentation separation;
- hidden-information boundaries;
- carried versus secured progress;
- configurable unresolved tuning rather than invented permanent policy.

Do not bundle adjacent TEL tickets because they are convenient.

## 7. Test in narrowing-to-broadening order

Run:

1. directly affected tests;
2. subsystem/project tests as warranted;
3. formatter/build checks needed for rapid feedback;
4. the ticket's specified validation;
5. required Godot/manual observation for visible client changes;
6. final `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/verify.ps1 -Mode Full` for code changes.

If generated audit/status projections are affected, run the repository generator/check rather than hand-editing generated views.

Capture exact pass/fail evidence.

## 8. Observe relevant surfaces

If the change affects Terminal/Godot/UI/CLI/integration behavior, exercise the observable surface and record concise evidence. Skip presentation observation for pure headless domain work unless the ticket requires it.

For Godot-visible work, headless projection tests do not replace required
manual observation; record the relevant runtime/version and input path when the
ticket or gate requires it.

## 9. Self-review the full diff

Review the complete branch diff against `main`. Check scope, correctness, invariants, determinism, save/versioning, API changes, tests, docs, generated output, secrets, and accidental artifacts.

Fix issues before independent review.

## 10. Invoke independent review

Invoke `$telengard-review` on the final candidate diff. Reviewers must inspect the repository/diff independently.

Pass the selected ticket's risk tags and the actual changed paths so the review
skill can route only lanes with distinct falsification targets. For each lane,
construct the compact reviewer contract required by `$telengard-review`:
`base ref`, `head ref`, selected TEL id, lane objective, lane-specific required
docs, and the finding schema. Do not forward the parent planning transcript,
implementation narrative, unrelated blueprint/invariant material, prior test
output, or other reviewers' conclusions unless that lane explicitly requires
the material. The parent still self-reviews the complete diff. Do not
instantiate every generic lane by default.

Resolve every actionable finding. If fixes materially change behavior, re-run focused tests, full verification, and the affected review lanes.

## 11. Update durable repository knowledge

Update the selected ticket and task ledger only after implementation evidence exists. Update BUILD_STATUS/ExecPlan/ADRs/README/DEVELOPMENT/INVARIANTS/generated status only when their documented role requires it.

Status must match reality. Never mark a ticket implemented because only its plan or docs exist.

## 12. Create the PR

Use branch `agent/<ticket>-<short-slug>` when practical.

PR body must contain:

```markdown
## Intent
<TEL ticket and outcome>

## What changed
- ...

## Non-goals / deferred decisions
- ...

## Save/version impact
<none or explicit details>

## Validation
- `<exact command>` — <result>

## Review
- <independent review summary and disposition>
```

Prefer a concise ticket-oriented title such as `TEL-101: implement rolled character creation`.

## 13. Gate the merge

Do not treat a successful local run as sufficient to merge.

Confirm the PR head still matches the reviewed/verified commit and require:

- local final verification passed;
- GitHub `Full verification` is green for the PR head;
- no required check is pending/failing;
- PR is mergeable;
- independent review has no unresolved actionable findings;
- docs/generated state are consistent;
- no unresolved product/tuning decision was invented.

If all gates are true, squash merge and delete the branch when supported.

If repository settings, CI visibility, permissions, or protection make the merge gate uncertain, leave the PR open and report `ready-for-human`. Never bypass the gate with force push/direct-to-main behavior.

## 14. Handoff and stop

After merge, inspect the new `main` only enough to identify likely next candidates and repository health. Do not implement another ticket.

End with exactly one handoff block:

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

For an Automation, this handoff is the terminal result of the scheduled run. A later run starts again from fresh repository state.
