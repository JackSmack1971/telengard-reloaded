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

## Current convergence

The current product sequence is **Playable Godot Vertical Slice** followed by the separate **Art Production Ready** handoff.

The repository has already proven renderer-independent Core Alpha composition headlessly and completed the TEL-090–TEL-093 presentation-separation proof. TEL-091 is a visual renderer prototype, not a complete client.

The current methodology coordinates two tracks:

```text
TEL-110..116 representative authored content
                +
TEL-120..127 playable Godot client
                |
                v
TEL-127 / GODOT-PLAYABLE-SLICE gate
                |
                v
TEL-128 / ART-PRODUCTION-READY gate
                |
                v
future production-art batches
```

Read [`presentation/GODOT_CLIENT_BLUEPRINT.md`](presentation/GODOT_CLIENT_BLUEPRINT.md) for the durable methodology and [`exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md`](exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md) for living multi-ticket coordination.

## Slice selection is not "first unchecked ticket"

`docs/tasks/README.md` is the current task-status authority, but the workflow evaluates each candidate before selection.

A ticket must have satisfied dependencies, no overlapping active PR, sufficiently defined acceptance criteria, reviewable scope, available required verification/observation, and a clear relationship to the nearest milestone.

For the current convergence the selector must examine both tracks. TEL-120–TEL-123 intentionally may proceed while TEL-110–TEL-116 are still being authored. TEL-124 onward declares content dependencies that force the two tracks to converge. TEL-127 proves playability; TEL-128 separately determines whether final production assets are eligible.

Eligible tickets are favored by dependency leverage toward the playable slice, continuity, acceptance-gate blocker value, ability to unlock the other track, focused verification clarity, and small coherent scope. Numerical TEL order is not itself a scheduling rule.

## Placeholder-first presentation policy

The repository distinguishes four presentation stages:

1. **Visual development** — style studies, UI wireframes, lighting tests, silhouettes, technical experiments. Allowed before the playable client is complete.
2. **Graybox/placeholder implementation** — production-shaped client architecture and resource paths using cheap placeholder visuals. This is the expected TEL-120–TEL-126 strategy.
3. **Playable-client acceptance** — TEL-127 proves the full first-slice loop in Godot with real content and placeholder/graybox presentation.
4. **Production readiness/assets** — TEL-128 must pass `docs/gates/ART-PRODUCTION-READY.md` before final tiles, sprites, animation, VFX, UI-art, icons, or production audio are systematically ticketed.

The workflow must not turn a client-infrastructure ticket into an opportunistic art-production batch.

## Godot observation is real acceptance evidence

A headless projection test proves a renderer contract; it does not prove a usable Godot client.

When a TEL ticket or gate requires Godot-visible acceptance, the workflow records the Godot/runtime version and exercises the relevant surface. Keyboard acceptance is required where specified; controller acceptance is required by the full playable-client gate and any ticket that explicitly owns it.

If the current execution environment cannot perform required Godot observation, the selector should prefer another eligible content/headless/client-contract slice. If no such slice exists, the run stops with a concrete environment blocker rather than weakening the acceptance criteria.

TEL-128 is additionally allowed to stop `blocked` when the Art Production Ready gate requires a visual-direction, binary/LFS, or other product/repository policy decision that is not already authoritative. Codex must not invent such a decision simply to pass the gate.

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
Start from current main, evaluate both current milestone tracks, do not overlap an active implementation PR, obey all hard-stop conditions, and stop after one merge or one ready-for-human/blocked result.
```

A conservative recurring schedule is preferable to an unbounded recursive prompt. The preflight overlap check makes repeated scheduled runs idempotent: if a prior implementation PR is still active, the new run should stop instead of opening competing work.

For faster project velocity, increase schedule frequency only after the workflow has demonstrated that CI, review, presentation observation, and merge gating behave reliably. The workflow itself must never weaken gates just to keep the schedule moving.

## Merge policy

This repository currently allows squash merge and disallows merge commits and rebase merge. The agent therefore uses squash merge when all gates pass.

The repository-level workflow `.github/workflows/verification.yml` runs:

1. generated audit-status checking;
2. `./eng/verify.ps1 -Mode Full`.

The autonomous workflow requires the GitHub `Full verification` result to be green for the exact PR head in addition to successful local verification. Presentation-visible tickets may also require manual Godot evidence that CI does not provide.

### Required repository setting

Enable `Full verification` from the `Repository verification` workflow as a required status check for `main` branch protection. Agent instructions treat it as mandatory even if GitHub settings do not yet enforce it, but server-side enforcement prevents accidental/manual bypass and is the stronger control.

## Independent review design

Self-review is useful but not independent. The `telengard-review` skill separates review into distinct lanes:

- correctness and edge cases;
- architecture, invariants, determinism, and save/version impact;
- tests and regression evidence;
- documentation and status provenance;
- presentation/resource ownership when relevant;
- security when relevant.

Godot/client reviews explicitly inspect simulation authority, input-command boundaries, projection redaction, resource ownership, required manual observation, and premature production-art scope. TEL-128 reviews also check that missing art/pipeline policy was not invented. Any unresolved P0/P1/P2 finding blocks merge.

## Documentation behavior

The workflow distinguishes current status from historical evidence:

- `docs/tasks/README.md` is the current TEL status ledger;
- individual `docs/tasks/TEL-*.md` files define slice scope and acceptance;
- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md` defines the current presentation development methodology;
- `docs/exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md` coordinates the multi-ticket Godot milestone but does not replace ticket status;
- `docs/gates/GODOT-PLAYABLE-SLICE.md` is owned by TEL-127;
- `docs/gates/ART-PRODUCTION-READY.md` is separately owned by TEL-128;
- `docs/BUILD_STATUS.md` is append-only verification history;
- other ExecPlans live under `docs/exec-plans/` when `docs/PLANS.md` requires them;
- generated audit/status projections must be updated through repository tooling.

The agent updates documentation as part of the same slice, after implementation evidence exists. Documentation-only creation never counts as gameplay/client implementation completion.

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

`ready-for-human` means implementation is reviewable but an external gate, permission, CI state, required presentation observation, or human decision prevents safe autonomous merge.

`blocked` means the selected work cannot be implemented safely from current repository truth.

`no-work` means no eligible slice exists.

## Repository hygiene before autonomous runs

Before turning up automation frequency, clean up stale/duplicate PRs and enforce required checks. A stale PR for an already-landed ticket can make candidate ownership ambiguous; the workflow intentionally stops on overlapping work instead of guessing which branch is authoritative.

Keep secrets, build outputs, local SDKs, `.codex/.verify-stamp.json`, Godot generated caches/import outputs not intentionally versioned, and other transient agent state untracked. Repository-local skills and `AGENTS.md` are intentionally versioned so every fresh clone receives the same autonomous engineering contract.
