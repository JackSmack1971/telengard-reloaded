# Telengard agent contract

This is the repository constitution for coding agents. Apply it together with
any more-specific `AGENTS.override.md` or nested `AGENTS.md`.

## Mission

Advance Telengard Reloaded toward its verified product milestones by completing
exactly one minimal, coherent TEL slice per implementation run. Preserve
architecture, determinism, save compatibility, documentation provenance, and
verification gates. Autonomy never authorizes inventing gameplay policy,
formulas, balance, tuning, visual direction, or production-asset policy left
unresolved by the product sources.

## Authority

Resolve product and engineering decisions in this order:

1. explicit user instruction for the current run;
2. this contract and applicable nested agent instructions;
3. `docs/tasks/index.json` for compact scheduling data (projected from the
   ledger plus `docs/tasks/index-overrides.json`), then the selected ticket;
4. `docs/modern-telengard-spec.md` for product intent;
5. `docs/INVARIANTS.md` for cross-cutting contracts;
6. `docs/ARCHITECTURE.md`, ADRs, and `docs/DEVELOPMENT.md`;
7. existing code and tests as current-state evidence.

`docs/tasks/README.md` is the human-readable TEL status ledger. `docs/BUILD_STATUS.md`
is append-only historical evidence and never overrides the ledger or code.
When authoritative sources materially conflict, record the conflict and stop
unless stronger repository evidence resolves it.

## Hard invariants

- authoritative gameplay state and rules remain in the renderer-independent
  simulation;
- state-changing commands validate before mutation and committed changes emit
  events at the correct boundary;
- deterministic behavior uses stable named/scoped inputs, never ambient random
  state;
- hidden information remains hidden until legitimately observed;
- content definitions stay separate from runtime state and presentation;
- carried/unsecured and secured wealth remain distinct;
- persisted state uses explicit DTOs and migrations when its shape changes;
- undefined formulas and tuning remain configurable and are labeled
  `CONFIGURATION/TUNING DECISION REQUIRED`.

## Autonomy and definition of done

Safe reads, repository-local tests, and in-scope edits are autonomous. Preserve
unrelated user changes. Do not bypass the pinned SDK, required verification,
manual acceptance, or external merge gates. Do not add speculative abstractions
or unrelated cleanup.

A slice is done only when its acceptance criteria and non-goals are satisfied,
focused tests and required observations pass, the canonical full gate passes,
the actual diff receives risk-appropriate independent review, and durable
status/documentation records truthfully describe the result.

## Hard stops

Stop without implementing or merging when:

- required behavior depends on an unresolved product, formula, balance, policy,
  or tuning decision that is not intentionally configurable;
- a dependency is incomplete or contradictory, or another active PR owns the
  same behavior;
- baseline health or required verification is not trustworthy or unavailable;
- a save/version decision cannot be made from existing contracts;
- the change would move authority into presentation/tooling or expose hidden
  information;
- credentials, destructive external actions, or unrelated user work are at
  risk; or
- the slice cannot remain coherent and reviewable as one transaction.

## Context routing

For next-slice selection, read this file, the next-slice skill, and
`docs/tasks/index.json` first. Load `docs/AGENT_INDEX.md`, the selected ticket,
its declared context manifest, specification sections, invariants, architecture,
development guidance, source, tests, and ExecPlans progressively after
candidates are narrowed. Use `docs/PLANS.md` when the selected work is
cross-cutting, persistent, multi-milestone, or otherwise risky.

## Intent Layer

This root file contains stable repository-wide doctrine only. Read the nearest
nested `AGENTS.md` for subsystem rules; nested files must add local contracts,
not repeat this workflow. Current milestone, ticket, diff, test, and CI state
belong in task/plan records and are loaded after this stable prefix.

Keep the context order stable: root constitution, applicable skill workflow,
tool contracts, then dynamic task and verification material.

## Workflows and verification

- `$telengard-next-slice` owns selection, one-slice execution, review routing,
  documentation, PR gating, and the final handoff contract.
- `$telengard-review` owns independent review of the actual diff.
- `docs/CODEX_MODEL_POLICY.md` defines effort escalation and reviewer role
  defaults; `high` is never a global subagent default.
- The canonical local gate is `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/verify.ps1 -Mode Full`.
- `.github/workflows/verification.yml` checks generated audit status and runs
  the same full gate for PRs and pushes to `main`.

Never begin a second TEL slice after merge in the same run. If any merge gate
is unavailable or ambiguous, leave the PR open and report `ready-for-human`.
