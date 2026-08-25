---
name: telengard-next-slice
description: Select and execute the next safe, logical Telengard Reloaded implementation slice end to end. Use when asked to continue the project, determine what to build next, implement the next TEL ticket, advance the Five-Floor MVP Demo, or run the repository's autonomous Codex development loop through planning, implementation, testing, review, documentation, PR, and gated merge.
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

The current convergence is the **Five-Floor MVP Demo** defined in
`docs/MVP_DEMO.md`. The intended critical path is TEL-129 → TEL-130 → TEL-131 →
TEL-132. TEL-126, TEL-127, and TEL-128 are post-MVP work and must not pre-empt
that sequence unless current repository evidence proves a hard prerequisite,
the MVP chain is blocked, or the user explicitly changes the milestone. Do not
create systematic final-asset batches before TEL-128 passes.

When delegation is needed, follow `docs/CODEX_MODEL_POLICY.md`. Use the
lowest role-appropriate effort, keep `high` for demonstrated ambiguity or
high-impact boundaries, and do not inherit a parent's `high` setting by
default.

## 1. Establish current state

From repository root, keep the pre-selection context deliberately small:

1. Read `AGENTS.md` and this skill.
2. Read generated `docs/tasks/index.json` and query its milestone, ticket status,
   track, dependencies, risk tags, decision state, context, review, and
   verification fields.
3. If the index milestone is `five-floor-mvp-demo`, read `docs/MVP_DEMO.md`
   before ranking candidates.
4. Inspect current branch/worktree, remote `main`, recent commits, open PRs,
   and CI state.
5. Detect overlapping/stale work before choosing a ticket.
6. When any candidate has `verification.godot_manual: true`, run
   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/godot-doctor.ps1`.
   Treat its discovered executable path and reported version as the authoritative
   local availability evidence. This check includes WinGet-installed Godot
   locations; do not conclude that Godot is absent from a failed PATH lookup
   alone.

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

When the generated milestone is `five-floor-mvp-demo`, treat TEL-129–TEL-132 as
the preferred candidate family. A not-started ticket outside that family must
have a concise evidence-based reason to pre-empt the MVP chain, such as a hard
prerequisite, a repository-health blocker, or explicit user reprioritization.

Reject a candidate when:

- dependencies are incomplete;
- an active PR already owns it;
- acceptance criteria require an undefined product/tuning decision;
- the slice is too broad to review coherently;
- another prerequisite is visibly more fundamental;
- current repository health prevents trustworthy validation;
- required Godot/manual observation cannot be performed and no approved
  substitute exists;
- it is post-MVP breadth while an eligible Five-Floor MVP ticket is ready; or
- it is a final-asset batch before TEL-128 / `ART-PRODUCTION-READY` passes.

Rank remaining candidates by:

1. continuity with the active milestone's critical path;
2. Five-Floor MVP blocker value while that milestone is active;
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
without it use the generated index and existing ticket sections as a
compatibility fallback; absent context is never permission to skip relevant
evidence.

Determine whether `docs/PLANS.md` requires an ExecPlan. Create/resume one before implementation if required. For TEL-129–TEL-132, use the existing `docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md` rather than creating a competing umbrella plan.

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

For the Five-Floor MVP, fixed seed/demo setup and explicit temporary tuning are
allowed only where the MVP docs/ticket permit them. They must remain visible and
replaceable rather than becoming hidden permanent balance policy.

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
manual observation; record the discovered executable path, runtime version, and
input path when the ticket or gate requires it. If `godot-doctor.ps1` reports an
installed runtime, use it before reporting an environment blocker.

TEL-132 cannot pass from headless evidence alone: it must exercise the real
fixed-seed floor-1-through-floor-5 Godot route defined by the MVP gate.

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

For TEL-129–TEL-132, update the Five-Floor MVP ExecPlan/gate as required by the selected ticket. Do not mark the broader TEL-127 gate or TEL-128 readiness complete merely because the MVP passes.

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

Prefer a concise ticket-oriented title such as `TEL-129: compose five-floor Godot session`.

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

While the Five-Floor MVP milestone is active, `next_candidates` should normally
advance to the next unfinished TEL-129–TEL-132 ticket. Only after TEL-132 passes
should TEL-126/TEL-127/TEL-128 return to the normal candidate set.

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

## Skill-local telemetry

Use `scripts/skill_telemetry.py` as best-effort, non-gating instrumentation. Resolve `<skill-root>` once as the directory containing this `SKILL.md`, then invoke an available Python 3 executable with `<skill-root>/scripts/skill_telemetry.py`; do not rediscover the helper path for each event. Runtime records stay under this skill's `telemetry/` directory. If Python or telemetry writes are unavailable, continue the slice workflow and mention the instrumentation failure in the final handoff; telemetry never relaxes selection, verification, review, or merge gates.

Start example: `python <skill-root>/scripts/skill_telemetry.py start --ticket none`. Semantic events use `event <name> --field key=value`; end with `end --outcome <RUN_RESULT-status> --ticket TEL-###`.

Start one session at the beginning of the transaction, before candidate selection; use ticket `none` until a slice is selected. Record normalized workflow facts rather than private selection reasoning:

- `candidate_set` with the compact candidate count;
- `candidate_rejected` with TEL id and one normalized reason: `dependency_incomplete`, `active_pr`, `undefined_product_decision`, `scope_too_broad`, `prerequisite_more_fundamental`, `repository_health`, `manual_observation_unavailable`, `post_mvp_preemption`, or `pre_tel128_final_asset`;
- `candidate_selected` for the initial choice and `candidate_selection_changed` if deeper evidence forces a different selection;
- `context_required_missing`, `unexpected_context_needed`, `context_conflict`, `legacy_ticket_fallback`, and `unmapped_risk` when those branches occur;
- `godot_runtime_resolved`, `godot_runtime_unavailable`, and `godot_manual_blocked` for Godot/manual-observation decisions;
- `review_requested`, `review_result`, and `review_rerun` at the independent-review boundary; record only verdict/severity counts, not finding prose;
- `merge_gate_checked`, `merge_gate_blocked` with a normalized `reason`, `pr_created`, `merge_attempted`, `merge_succeeded`, and `ready_for_human` for external gate outcomes;
- `second_slice_attempted` if the one-slice transaction boundary is ever violated, and `missing_terminal_result` if the run cannot emit the required handoff.

Use `probe` for declared/expected required paths where first-attempt resolution matters. Use `run` only for consequential commands: task-index validation, Godot doctor, focused/subsystem tests, formatter/build gates, generators, ticket verification, and canonical final verification. Set the canonical full gate to `--kind full-verification --label verify-full`. Routine reads, searches, `git status`, metadata inspection, and diff review run normally. Reuse stable labels for retries; inspect the concrete failure and make a targeted correction before rerunning. Prefer repository PowerShell `-File` entry points and do not place complex inline PowerShell behind the Python wrapper.

External GitHub/tool actions are logged semantically rather than forced through the command wrapper. Do not log prompts/private reasoning, diff or source contents, command stdout/stderr, environment variables, credentials, PR bodies, API payloads, or access tokens.

End the telemetry session with the same terminal disposition as `RUN_RESULT`: `merged`, `ready-for-human`, `blocked`, or `no-work` (use `failed`/`inconclusive` only when that accurately describes an abnormal run). Generate an improvement report when requested with `scripts/skill_telemetry.py report --write`; compare fingerprint cohorts rather than overwriting historical evidence.
