# Codex autonomous next-slice workflow

This repository uses a one-slice-per-run Codex workflow to advance Telengard Reloaded without allowing a long-lived agent thread to accumulate stale assumptions or silently invent product policy.

The durable behavior contract lives in [`/AGENTS.md`](../AGENTS.md). The reusable orchestrator is [`/.codex/skills/telengard-next-slice/SKILL.md`](../.codex/skills/telengard-next-slice/SKILL.md), and independent review is [`/.codex/skills/telengard-review/SKILL.md`](../.codex/skills/telengard-review/SKILL.md).

## Why the loop is transactional

The goal is not to ask one agent to "finish the game." Each run performs one auditable transaction:

```text
PREFLIGHT
  ↓
READ + DISCOVER
  ↓
SELECT ONE ELIGIBLE SLICE
  ↓
PLAN
  ↓
REPRODUCE / PROVE GAP
  ↓
IMPLEMENT
  ↓
TARGETED TESTS
  ↓
FULL VERIFY
  ↓
OBSERVE RELEVANT SURFACES
  ↓
SELF-REVIEW
  ↓
INDEPENDENT MULTI-LANE REVIEW
  ↓
FIX + REVERIFY
  ↓
DOCUMENT
  ↓
PR
  ↓
CI MERGE GATE
  ↓
SQUASH MERGE OR STOP FOR HUMAN
  ↓
HANDOFF + EXIT
```

A later run starts again from fresh `main` and re-evaluates the repository. This prevents the selection decision for ticket N+1 from being based on pre-merge assumptions from ticket N.

## Slice selection is not "first unchecked ticket"

`docs/tasks/README.md` is the current task-status authority, but the workflow evaluates each candidate before selection.

A ticket must have satisfied dependencies, no overlapping active PR, sufficiently defined acceptance criteria, reviewable scope, and a clear relationship to the nearest milestone. Eligible tickets are then favored by feature-chain continuity, Core Alpha blocker value, dependency leverage, focused-test clarity, small coherent scope, and lower architectural risk.

This lets Codex choose intelligently while keeping the decision auditable.

## Current likely first proof run

At the time this workflow was introduced, TEL-100 was the most recently completed Core Alpha feature-chain ticket. TEL-101 is the natural continuation because it implements the first concrete character-creation mode through the TEL-100 boundary and already defines deterministic configuration, dependencies, non-goals, tests, and acceptance criteria.

TEL-101 must not invent a permanent roll formula, reroll limit, or anti-reroll policy. Those are intentionally unresolved/configurable decisions in the ticket. The workflow should therefore implement configurable deterministic rolled creation, not "finish" character creation by making product decisions outside the task.

The selector still re-checks repository state on every run; this paragraph is context, not a hard-coded ticket override.

## Invoking the workflow manually

From a Codex session rooted at this repository, invoke the repository skill explicitly:

```text
$telengard-next-slice Advance Telengard Reloaded by exactly one safe logical implementation slice. Follow the repository merge gates and stop after the handoff.
```

The skill can also be selected implicitly for requests such as "implement the next logical Telengard slice" because its description declares that trigger.

## Repeating with a Codex Automation

Use a Codex Automation when you want periodic autonomous progress. Scope the automation to this repository and use an instruction equivalent to:

```text
$telengard-next-slice
Advance Telengard Reloaded by exactly one safe logical implementation slice.
Start from current main, do not overlap an active implementation PR, obey all hard-stop conditions, and stop after one merge or one ready-for-human/blocked result.
```

A conservative recurring schedule is preferable to an unbounded recursive prompt. The preflight overlap check makes repeated scheduled runs idempotent: if a prior implementation PR is still active, the new run should stop instead of opening competing work.

For faster project velocity, increase schedule frequency only after the workflow has demonstrated that CI, review, and merge gating behave reliably. The workflow itself must never weaken gates just to keep the schedule moving.

## Merge policy

This repository currently allows squash merge and disallows merge commits and rebase merge. The agent therefore uses squash merge when all gates pass.

The repository-level workflow `.github/workflows/verification.yml` runs:

1. generated audit-status checking;
2. `./eng/verify.ps1 -Mode Full`.

The autonomous workflow requires the GitHub `Full verification` result to be green for the exact PR head in addition to successful local verification.

### Required repository setting

Enable `Full verification` from the `Repository verification` workflow as a required status check for `main` branch protection. Agent instructions treat it as mandatory even if GitHub settings do not yet enforce it, but server-side enforcement prevents accidental/manual bypass and is the stronger control.

## Independent review design

Self-review is useful but not independent. The `telengard-review` skill separates review into distinct lanes:

- correctness and edge cases;
- architecture, invariants, determinism, and save/version impact;
- tests and regression evidence;
- documentation and status provenance.

Presentation and security lanes are added when relevant. Any unresolved P0/P1/P2 finding blocks merge.

## Documentation behavior

The workflow distinguishes current status from historical evidence:

- `docs/tasks/README.md` is the current TEL status ledger;
- individual `docs/tasks/TEL-*.md` files define slice scope and acceptance;
- `docs/BUILD_STATUS.md` is append-only verification history;
- ExecPlans live under `docs/exec-plans/` when `docs/PLANS.md` requires them;
- generated audit/status projections must be updated through repository tooling.

The agent updates documentation as part of the same slice, after implementation evidence exists. Documentation-only creation never counts as implementation completion.

## Run result contract

Every run ends with:

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

`merged` means the slice is complete and on `main`.

`ready-for-human` means implementation is reviewable but an external gate, permission, CI state, or human decision prevents safe autonomous merge.

`blocked` means the selected work cannot be implemented safely from current repository truth.

`no-work` means no eligible slice exists.

## Repository hygiene before autonomous runs

Before turning up automation frequency, clean up stale/duplicate PRs and enforce required checks. A stale PR for an already-landed ticket can make candidate ownership ambiguous; the workflow intentionally stops on overlapping work instead of guessing which branch is authoritative.

Keep secrets, build outputs, local SDKs, `.codex/.verify-stamp.json`, and other transient agent state untracked. Repository-local skills and `AGENTS.md` are intentionally versioned so every fresh clone receives the same autonomous engineering contract.
