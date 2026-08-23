---
name: telengard-next-slice
description: Select and execute the next safe, logical Telengard Reloaded implementation slice end to end. Use when asked to continue the project, determine what to build next, implement the next TEL ticket, advance the nearest milestone, or run the repository's autonomous Codex development loop through planning, implementation, testing, review, documentation, PR, and gated merge.
---

# Telengard Next Slice

Execute exactly one repository slice as a fresh transaction. Follow the root `AGENTS.md` completely.

## Objective

Advance the nearest project milestone by selecting the best **eligible** next slice from repository evidence, implementing only that slice, validating it, independently reviewing it, updating durable knowledge, and creating/merging a PR only when every repository merge gate is satisfied.

Never continue into a second implementation slice in the same run.

The current product-development convergence milestone is the **Playable Godot Vertical Slice**: complete the representative floors 1-5 content while building the production-shaped Godot client with placeholders, then pass the playable-client and Art Production Ready gates. Do not treat TEL-091 as a complete Godot client.

## 1. Establish current state

From repository root:

1. Read `AGENTS.md`.
2. Read `docs/tasks/README.md`, `docs/INVARIANTS.md`, `docs/DEVELOPMENT.md`, and `docs/AGENT_INDEX.md`.
3. Inspect current branch/worktree, remote `main`, recent commits, open PRs, and CI state.
4. Detect overlapping/stale work before choosing a ticket.
5. Read status/milestone material needed to understand the nearest milestone.
6. If any serious candidate is TEL-110 through TEL-127, read `docs/presentation/GODOT_CLIENT_BLUEPRINT.md` and `docs/exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md` before ranking candidates.

If unrelated user changes are present, do not overwrite, reset, stash, or absorb them without explicit authorization. Prefer an isolated worktree/branch when available.

## 2. Build the candidate set

Extract plausible `Not started` work from `docs/tasks/README.md` plus any explicit active remediation work. For each serious candidate, inspect its ticket and dependencies.

For the current playable-slice milestone, build the candidate set across **both** coordinated tracks:

- TEL-110–TEL-116 — representative authored content;
- TEL-120–TEL-127 — playable Godot client.

Do not rigidly select the lowest TEL number. TEL-120–TEL-123 are intentionally able to progress while TEL-110–TEL-116 are still being authored when their explicit dependencies are satisfied. TEL-124 onward has content dependencies that enforce convergence.

Reject a candidate when:

- dependencies are incomplete;
- an active PR already owns it;
- acceptance criteria require an undefined product/tuning decision;
- the slice is too broad to review coherently;
- another prerequisite is visibly more fundamental;
- current repository health prevents trustworthy validation;
- required Godot/manual observation cannot be performed and the ticket provides no repository-approved substitute;
- it is a production-art/final-asset batch while `docs/gates/ART-PRODUCTION-READY.md` lacks passing evidence.

When a Godot-visible ticket is blocked only because the current environment lacks required Godot tooling, prefer another eligible content/headless/client-contract slice if one exists. If none exists, stop with a concrete environment blocker; never weaken the ticket's observation criteria.

Rank remaining candidates by:

1. direct dependency leverage toward the Playable Godot Vertical Slice convergence;
2. continuity with the most recently completed coherent feature/client chain;
3. milestone blocker value;
4. ability to unlock work on the other coordinated track;
5. ability to prove behavior with focused tests/observation;
6. small/reviewable scope;
7. architectural and save/version risk.

Choose one. Record only a concise evidence-based selection rationale, not private reasoning.

## 3. Read the selected slice deeply

Read:

- the selected ticket;
- directly referenced specification sections when needed;
- relevant architecture/ADR material;
- existing implementation and tests for adjacent behavior;
- recent commits/PRs in the same subsystem when useful.

For TEL-110–TEL-127, also read the blueprint documents referenced by the ticket. For TEL-120–TEL-127, read/update the active umbrella ExecPlan as durable coordination state.

Determine whether `docs/PLANS.md` requires a ticket-specific ExecPlan in addition to the umbrella plan. Create/resume one before implementation if required.

## 4. Plan the transaction

Create a short implementation plan with:

- selected TEL id and intended outcome;
- scope/non-goals;
- current gap or bug reproduction;
- modules/types/events likely affected;
- determinism and hidden-information considerations;
- save/version impact, including `none` when truly none;
- content/presentation boundary impact when relevant;
- Godot/manual observation required by acceptance when relevant;
- focused tests;
- canonical final verification;
- documentation/status/ExecPlan updates.

For presentation work, explicitly state whether the slice is:

- client foundation;
- placeholder/graybox presentation;
- playable integration;
- production art.

Production art is invalid before Art Production Ready passes.

If evidence reveals that the ticket is not actually implementable without a new product decision, stop with `RUN_RESULT status: blocked`.

## 5. Prove the gap

For bugs, reproduce the current failure/behavior where practical.

For net-new domain/client work, add or identify the smallest acceptance-level test or executable observation that demonstrates the missing behavior. Run focused tests before implementation when that produces meaningful evidence.

Do not force a red-test ritual for docs-only or tooling-only changes where it adds no information.

For Godot-visible work, distinguish headless contract evidence from actual presentation observation. A headless projection test does not by itself prove a playable UI/scene behavior.

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

For Godot/client work additionally enforce:

- Godot input/UI/animation may request commands but never resolve authoritative gameplay;
- missing drawing information must be added through renderer-safe presentation projections, not direct hidden `GameState` access;
- Godot resource paths/UIDs do not become authoritative save state;
- stable content/presentation IDs resolve through the presentation asset registry rather than scattered scene conditionals;
- placeholder-first implementation is preferred until the Art Production Ready gate passes;
- no opportunistic final asset production while implementing client infrastructure.

Do not bundle adjacent TEL tickets because they are convenient.

## 7. Test in narrowing-to-broadening order

Run:

1. directly affected tests;
2. subsystem/project tests as warranted;
3. formatter/build checks needed for rapid feedback;
4. the ticket's specified validation;
5. required Godot/manual presentation observation for visible client changes;
6. final `./eng/verify.ps1 -Mode Full` for code changes.

If generated audit/status projections are affected, run the repository generator/check rather than hand-editing generated views.

Capture exact pass/fail evidence. Do not substitute headless tests for an explicitly required Godot acceptance run.

## 8. Observe relevant surfaces

If the change affects Terminal/Godot/UI/CLI/integration behavior, exercise the observable surface and record concise evidence.

For Godot tickets:

- record the Godot/runtime version when manual observation is required;
- exercise the ticket's keyboard path;
- exercise controller behavior when required by the ticket/gate;
- confirm visual/UI callbacks did not become gameplay authority;
- use fixed-seed scenarios when they improve reproducibility.

Skip presentation observation for pure headless domain work unless the ticket requires it.

## 9. Self-review the full diff

Review the complete branch diff against `main`. Check scope, correctness, invariants, determinism, save/versioning, API changes, tests, docs, generated output, secrets, and accidental artifacts.

For presentation work also check:

- hidden-information leaks;
- direct Godot-to-GameState mutation;
- duplicated content/game rules in scenes;
- resource paths embedded in authoritative data;
- placeholder bypasses that would need architectural replacement later;
- production-art work introduced before the readiness gate.

Fix issues before independent review.

## 10. Invoke independent review

Invoke `$telengard-review` on the final candidate diff. Reviewers must inspect the repository/diff independently.

Resolve every actionable finding. If fixes materially change behavior, re-run focused tests, full verification, observation, and affected review lanes.

Use the presentation review lane for Godot/client work and explicitly review simulation authority, projection redaction, input-command boundaries, and asset/resource ownership.

## 11. Update durable repository knowledge

Update the selected ticket and task ledger only after implementation evidence exists. Update BUILD_STATUS/ExecPlan/ADRs/README/DEVELOPMENT/INVARIANTS/generated status only when their documented role requires it.

For TEL-120–TEL-127, update `docs/exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md` progress/decisions when the slice changes plan state.

For TEL-127, record the two gate results independently:

- `docs/gates/GODOT-PLAYABLE-SLICE.md`;
- `docs/gates/ART-PRODUCTION-READY.md`.

Do not create production-art TEL batches unless Art Production Ready has passing evidence.

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
- <Godot/manual observation when required>

## Review
- <independent review summary and disposition>
```

Prefer a concise ticket-oriented title such as `TEL-120: build Godot application host`.

## 13. Gate the merge

Do not treat a successful local run as sufficient to merge.

Confirm the PR head still matches the reviewed/verified commit and require:

- local final verification passed;
- required presentation observation passed;
- GitHub `Full verification` is green for the PR head;
- no required check is pending/failing;
- PR is mergeable;
- independent review has no unresolved actionable findings;
- docs/generated state are consistent;
- no unresolved product/tuning decision was invented;
- no production-art gate was bypassed.

If all gates are true, squash merge and delete the branch when supported.

If repository settings, CI visibility, permissions, Godot observation, or protection make the merge gate uncertain, leave the PR open and report `ready-for-human`. Never bypass the gate with force push/direct-to-main behavior.

## 14. Handoff and stop

After merge, inspect the new `main` only enough to identify likely next candidates and repository health. Do not implement another ticket.

When TEL-110–TEL-127 work remains, candidate handoff should mention both tracks when relevant rather than assuming strict numeric order.

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
