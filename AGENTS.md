# Telengard Reloaded — Codex repository instructions

This file is the durable repository contract for coding agents. Apply it to all work in this repository unless a more-specific `AGENTS.override.md` or nested `AGENTS.md` applies.

## Mission

Advance Telengard Reloaded toward Core Alpha by completing **one minimal coherent slice per implementation run** while preserving the project's architecture, determinism, save compatibility, documentation provenance, and verification gates.

Autonomy does not authorize invention. When the specification or ticket leaves gameplay policy, balancing, formulas, or tuning unresolved, preserve that configurability and stop rather than silently choosing a permanent rule.

## Sources of truth

For product and engineering decisions, use this precedence:

1. Explicit user instruction for the current run.
2. This `AGENTS.md` and any applicable nested agent instructions.
3. `docs/tasks/README.md` for current TEL-ticket status and ordering.
4. The selected `docs/tasks/TEL-*.md` ticket for exact scope, dependencies, non-goals, tests, and acceptance criteria.
5. `docs/modern-telengard-spec.md` for product intent and requirements.
6. `docs/INVARIANTS.md` for contracts that must remain true.
7. `docs/ARCHITECTURE.md`, ADRs, and `docs/DEVELOPMENT.md` for architectural and implementation conventions.
8. Existing code and tests as evidence of current behavior.
9. `docs/BUILD_STATUS.md` as append-only verification history, never as a replacement for the task ledger.

If these sources materially conflict, do not guess. Record the conflict and stop the implementation run unless the conflict can be resolved from stronger repository evidence.

## The one-slice transaction

Every autonomous implementation run is a transaction with these phases. Do not start a second TEL slice in the same run after merge.

### 0. PREFLIGHT

- Start from a clean worktree based on current `main`.
- Fetch remote state and confirm the local base is not stale.
- Inspect open pull requests and branches for work that overlaps candidate tickets.
- Detect stale or duplicate PRs, failing baseline CI, merge conflicts, generated-doc drift, or repository-control problems before selecting work.
- Never modify or discard unrelated user changes.

### 1. READ

Read at minimum:

- this `AGENTS.md`;
- `docs/tasks/README.md`;
- `docs/INVARIANTS.md`;
- `docs/DEVELOPMENT.md`;
- the candidate ticket(s);
- relevant architecture/spec/ADR material discovered from those files.

For significant work, also read `docs/PLANS.md` and create or resume an ExecPlan when its criteria apply.

### 2. SELECT

Select exactly one next slice. Do not merely choose the first unchecked ticket.

A candidate is eligible only when:

- all explicit dependencies are implemented and verified;
- no active PR already owns the same behavior;
- its acceptance criteria are sufficiently defined to implement without inventing product policy;
- the change can be kept coherent and reviewable;
- it advances the nearest repository milestone, currently Core Alpha, or removes a blocker to that milestone.

Prefer candidates using this order of evidence:

1. direct continuation of the most recently completed coherent feature chain;
2. blocker removal for Core Alpha acceptance;
3. dependency-critical work that unlocks multiple subsequent tickets;
4. smallest independently verifiable vertical behavior;
5. repository engineering work only when it blocks safe product progress.

When multiple candidates remain plausible, compare dependency readiness, milestone value, scope size, architectural risk, verification clarity, and continuity with recent work. Record the selected ticket and a short evidence-based rationale in the plan/PR; do not record hidden chain-of-thought.

### 3. PLAN

Before editing code:

- identify the exact user-visible/domain outcome;
- list in-scope and non-goal behavior;
- identify affected modules, public APIs, events, deterministic inputs, content boundaries, and save/version impact;
- identify the focused tests that should fail or be absent before the change;
- state the full verification path;
- create/update an ExecPlan when `docs/PLANS.md` requires one.

Keep plans factual and self-contained. Update them when repository discoveries invalidate assumptions.

### 4. REPRODUCE OR PROVE THE GAP

For a bug/regression, reproduce the existing behavior before fixing it whenever practical.

For a net-new feature, prove the gap with the smallest acceptance-level test or executable observation that demonstrates the required behavior is currently absent. Do not manufacture a fake failing test when the ticket is purely documentation/tooling work.

### 5. IMPLEMENT

Implement the smallest coherent change that satisfies the selected ticket.

Repository contracts:

- authoritative gameplay state remains in the renderer-independent simulation;
- state-changing commands validate before mutation;
- committed domain changes emit domain events at the correct boundary;
- deterministic behavior uses stable, named/scoped inputs rather than ambient randomness;
- hidden information stays hidden until legitimately observed;
- content definitions stay separate from runtime state and renderer logic;
- carried/unsecured and secured wealth remain distinct;
- saves use explicit DTOs and migrations when persisted shape changes;
- undefined formulas/tuning remain configurable and are labeled `CONFIGURATION/TUNING DECISION REQUIRED` where appropriate;
- avoid unrelated refactors and opportunistic cleanup.

### 6. TEST

Run the narrowest relevant tests first. Add tests proportional to the behavior, including invalid/boundary cases, deterministic replay, event ordering, save round trip/migration, and renderer independence when applicable.

Use repository wrappers from `docs/DEVELOPMENT.md`; do not bypass the pinned SDK policy.

### 7. VERIFY

Before review, run the repository's canonical checks required by the ticket. For code changes the final local gate is normally:

```powershell
./eng/verify.ps1 -Mode Full
```

Also run generated-status checks or other ticket-specific tooling when applicable. Never claim a check passed unless it actually ran successfully.

### 8. OBSERVE

When the slice affects presentation, CLI behavior, integration output, logs, or other externally observable behavior, exercise that surface and capture concise evidence. Do not require browser/Godot observation for renderer-independent work that has no presentation surface.

### 9. SELF-REVIEW

Inspect the complete diff against `main`, not only the files you remember changing. Check for:

- scope creep;
- incorrect assumptions;
- invariant violations;
- determinism leaks;
- save/version mistakes;
- public API drift;
- missing negative tests;
- stale generated/documentation output;
- accidental secrets, debug artifacts, or build output.

### 10. INDEPENDENT REVIEW

Run the repository `telengard-review` skill or equivalent independent reviewer subagents. Reviewers must inspect the actual diff and relevant sources of truth, not rely on the implementing agent's summary.

At minimum cover these independent lanes:

1. correctness and edge cases;
2. architecture, invariants, determinism, and save/version impact;
3. test quality and regression risk;
4. documentation, task-ledger, ExecPlan, and generated-status consistency.

For presentation/security-sensitive changes add specialized review lanes as relevant.

### 11. FIX AND REVERIFY

Resolve every actionable review finding or explicitly document why it is not applicable. Re-run focused tests for changed behavior and repeat the full required verification gate after fixes.

If fixes materially change the diff, repeat independent review on the final diff.

### 12. DOCUMENT

Update durable repository knowledge in the same slice when required:

- selected TEL ticket status and evidence;
- `docs/tasks/README.md` current status;
- `docs/BUILD_STATUS.md` append-only verification evidence when appropriate;
- active/completed ExecPlan state;
- ADRs only for durable architectural decisions;
- generated audit/status projections using the repository tooling rather than hand-editing generated files;
- README/DEVELOPMENT/INVARIANTS only when their durable contracts actually change.

Documentation must describe what exists after the change, what was verified, save/version impact, and intentionally deferred work. Never mark work implemented merely because a plan or task file was written.

### 13. PR AND MERGE GATE

Use a dedicated branch, normally `agent/<ticket>-<short-slug>`.

A PR must include:

- intent and selected ticket;
- concise implementation summary;
- explicit non-goals/deferred decisions;
- save/version impact;
- exact validation run and results;
- reviewer findings disposition when material.

Default to one TEL ticket per PR unless the ticket itself is explicitly an integration/multi-ticket proof.

Merge only when all of the following are true:

- local required verification passed on the final diff;
- the GitHub `Full verification` check for the PR head is green;
- no required workflow is failing or pending;
- there are no unresolved actionable review findings;
- the PR is mergeable and up to date enough to trust its CI result;
- documentation/status projections are consistent;
- no unresolved specification/tuning decision was silently invented.

Use squash merge because this repository allows squash merging and does not allow merge commits or rebase merge. If any merge gate is unavailable or ambiguous, leave the PR open and report `ready-for-human` rather than forcing the merge.

### 14. HANDOFF AND EXIT

After merge, synchronize/read the new `main`, identify likely next candidates only as a handoff, and **end the run**. Do not implement the next ticket in the same thread/run.

Finish with a compact machine-readable handoff:

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

A scheduler, Codex Automation, or human can start a fresh run of the `telengard-next-slice` skill from the updated repository state.

## Hard stop conditions

Stop without implementing or merging when any of these applies:

- required behavior depends on an unresolved gameplay formula, balance rule, policy, or tuning choice not intentionally configurable;
- a dependency is incomplete or contradictory;
- another active PR owns the same behavior;
- baseline `main` is materially broken in a way that makes validation untrustworthy;
- required tests/verification cannot run and there is no repository-approved substitute;
- a save migration/version decision cannot be made from existing contracts;
- the requested change would move authoritative gameplay behavior into presentation/tooling;
- credentials, secrets, destructive external actions, or unrelated user work would be endangered;
- the coherent slice expands beyond what can be reviewed as one transaction.

When stopped, report the smallest concrete decision or prerequisite needed to resume.

## Current repository-specific verification

The canonical full gate is `./eng/verify.ps1 -Mode Full`. The GitHub workflow `.github/workflows/verification.yml` runs repository audit-status checking and the same full gate on pull requests and pushes to `main`.

Treat the task ledger as current status authority and `BUILD_STATUS.md` as historical evidence.
