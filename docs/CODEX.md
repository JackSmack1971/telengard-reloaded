# Codex development control plane

This repository carries project-scoped Codex guidance, skills, hooks, and executable verification so agent behavior does not depend on a developer's global `PATH` or remembered prompts.

## First use

1. Extract/copy the control-plane files into the repository root, preserving directories.
2. Start Codex from the repository (preferably the Git root) and mark the project trusted so project `.codex/` configuration can load.
3. In Codex CLI, run `/hooks`, review the repository hooks, and trust them. Codex intentionally requires explicit trust for changed non-managed hooks.
4. Run `./eng/doctor.ps1` once.
5. Start a new Codex session after changing `AGENTS.md` or `.codex/config.toml` so startup discovery is refreshed.

## Canonical commands

The command and SDK policy is canonical in the repository root `AGENTS.md`.
Use the wrapper and verification commands defined there; do not use bare
`dotnet`.

## What the hooks do

- `SessionStart`: reminds Codex of the local SDK and canonical verification commands, including after context compaction.
- `PreToolUse`: blocks bare `dotnet` and a small set of repository-wide destructive Git commands (`git clean`, `git reset --hard`, repo-wide restore/checkout) before they run.
- `Stop`: when source/build/test files changed, checks for a verification fingerprint from a successful Full verification. If stale/missing, it asks Codex to run the final gate once before concluding. It will not loop forever if verification cannot succeed.

Set environment variable `CODEX_SKIP_VERIFY_GUARD=1` for a session if you intentionally need to bypass only the stop verification nudge. This does not bypass test failures or other repository instructions.

## Skills

- `$telengard-next-slice` — select and execute exactly one eligible TEL slice through repository preflight, planning, implementation, testing/observation, independent review, documentation, PR, CI gate, and handoff. During the current convergence it must prioritize the Five-Floor MVP Demo critical path from `docs/tasks/index.json`.
- `$telengard-review` — perform a high-signal independent Telengard-specific review with correctness, architecture/determinism/save, tests, documentation/provenance, and presentation/security lanes when relevant.

## Current milestone context

The current product milestone is the **Five-Floor MVP Demo**.

Codex should use:

- `docs/MVP_DEMO.md` for the canonical immediate product scope and non-goals;
- `docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md` for living multi-ticket coordination;
- `docs/gates/FIVE-FLOOR-MVP-DEMO.md` for TEL-132 acceptance;
- `docs/tasks/index.json` for the generated scheduling surface; and
- `docs/tasks/README.md` for the human-readable status ledger.

The intended implementation order is TEL-129 → TEL-130 → TEL-131 → TEL-132.
TEL-126, TEL-127, and TEL-128 remain valid but are **post-MVP** work and must not
pre-empt this sequence unless they become a demonstrated prerequisite or the
user explicitly changes the milestone.

After TEL-132 passes, Codex returns to:

- `docs/exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md` for broader client completion;
- `docs/gates/GODOT-PLAYABLE-SLICE.md` for TEL-127 full playable-slice acceptance; and
- `docs/gates/ART-PRODUCTION-READY.md` for TEL-128 before systematic final art/audio asset batches.

The repository already contains substantial Godot work: TEL-120 host/bootstrap,
TEL-121 input/clock implementation, TEL-122 session/scene-flow implementation,
TEL-123 presentation contract/asset registry, TEL-124 graybox presentation, and
partial TEL-125 HUD/combat-intent work. The current gap is integration into one
real five-floor session, not another foundational renderer prototype.

## Design references

The control plane follows current official Codex guidance: keep `AGENTS.md` scoped and authoritative; use repository docs as the deeper system of record; use project `.codex/config.toml` for repo-specific behavior; use skills for repeatable workflows with progressive disclosure; use hooks for deterministic lifecycle enforcement; and use ExecPlans for long-running or multi-ticket implementation work.

The presentation methodology deliberately separates the fixed-seed five-floor MVP, broader placeholder/graybox client acceptance, and gated production asset work so Codex does not spend effort on persistence breadth or final art before the core player-facing dungeon loop is demonstrably playable.
