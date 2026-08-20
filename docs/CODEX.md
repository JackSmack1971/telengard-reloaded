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

- `$telengard-ticket` — implement or continue a TEL ticket using spec-first scoping, dependency inspection, deterministic tests, and status updates.
- `$telengard-review` — perform a high-signal Telengard-specific review, optionally using read-only parallel subagents for architecture, determinism/save, and test gaps.

## Design references

The control plane follows current official Codex guidance: keep `AGENTS.md` concise and scoped; use repository docs as the deeper system of record; use project `.codex/config.toml` for repo-specific behavior; use skills for repeatable workflows with progressive disclosure; use hooks for deterministic lifecycle enforcement; and use ExecPlans for long-running implementation work.
