# Agent context index

Use this page to choose the smallest context set for a task. The repository specification is intentionally larger than the instructions loaded on every Codex turn.

## Source-of-truth routing

| Need | Read first | Then verify against |
|---|---|---|
| Product/game behavior | `modern-telengard-spec.md` | relevant TEL ticket + tests/code |
| Cross-cutting contracts | `INVARIANTS.md` | architecture tests + implementation |
| Dependency/authority design | `ARCHITECTURE.md` | project references + code |
| Tech-stack decision | `adr/ADR-001-technology-stack.md` | `global.json`, `Directory.Build.props`, projects |
| A TEL ticket | `tasks/TEL-xxx.md` | spec sections, dependencies, code/tests, `BUILD_STATUS.md` |
| What is implemented now | code + tests | `tasks/README.md`, then `BUILD_STATUS.md` history |
| Phase/gate acceptance | `gates/*.md` | current test/build/manual evidence |
| Long-running work | `PLANS.md` | active plan under `exec-plans/active/` |
| Playable Godot/client work | `presentation/GODOT_CLIENT_BLUEPRINT.md` | TEL-120–TEL-127, active Godot ExecPlan, Godot code/tests |
| UX/input work | `presentation/UX_INTERACTION_BLUEPRINT.md` | selected TEL ticket + command/application boundaries |
| Art/readiness work | `presentation/ART_DIRECTION_BLUEPRINT.md` | `gates/ART-PRODUCTION-READY.md`, first-slice content IDs |
| Asset/resource pipeline | `presentation/ASSET_PIPELINE_BLUEPRINT.md` | Godot registry/import implementation + selected TEL ticket |
| Codex/runtime workflow | `CODEX_WORKFLOW.md` | `AGENTS.md`, `eng/*.ps1`, `.codex/` |

## Important status rule

`docs/tasks/README.md` is the current TEL-ticket status authority. Working code/tests are the present-state implementation evidence and `modern-telengard-spec.md` remains product intent. `BUILD_STATUS.md` is append-only verification history. Never remove implemented behavior solely because historical prose lags; reconcile stale status records when appropriate.

For TEL-110–TEL-127, the task ledger and explicit ticket dependencies govern eligibility. The active Godot umbrella ExecPlan coordinates the multi-ticket milestone but is not a status ledger.

## Current milestone routing

The current convergence milestone is the **Playable Godot Vertical Slice**.

Two tracks may progress in parallel:

- TEL-110–TEL-116 — representative floors 1-5 authored content;
- TEL-120–TEL-127 — playable Godot client.

Read `presentation/GODOT_CLIENT_BLUEPRINT.md` before ranking serious candidates across these tracks. Do not infer that TEL-091 is a complete client. Do not start systematic final production-art batches before `gates/ART-PRODUCTION-READY.md` passes.

## Current code map

- `src/Telengard.Core/Events/` — domain event bus.
- `src/Telengard.Core/Rng/` — deterministic RNG.
- `src/Telengard.Core/Simulation/` — game state and command dispatcher.
- `src/Telengard.Core/world/generation/` — floor generation/walking/transitions.
- `src/Telengard.Core/world/visibility/` — tile visibility.
- `src/Telengard.Core/presentation/` — renderer-safe presentation projections and Modern frame prototype contract.
- `src/Telengard.Save/` — explicit save DTOs, serializer, migrations.
- `src/Telengard.Terminal/` — terminal presentation boundary.
- `src/Telengard.Godot/` — Godot presentation boundary/prototype; playable-client work is TEL-120–TEL-127.
- `tests/Telengard.Architecture.Tests/` — architecture + domain behavior tests.
- `tools/Telengard.TestHarness/` — scripted deterministic simulation harness.
- `content/` — external data-defined content pack; authored first-slice work is TEL-110–TEL-116.

## Context recipes

### Implement the next logical slice

Use `$telengard-next-slice`. Read root `AGENTS.md` → this index → `tasks/README.md` → serious candidate tickets → required blueprint/ExecPlan context → cited spec/invariants/architecture → adjacent code/tests.

For TEL-110–TEL-127, evaluate both current milestone tracks rather than selecting strictly by TEL number.

### Implement a specific TEL ticket

Read root `AGENTS.md` → this index → task file → its `Required Blueprint Context` when present → cited spec sections → relevant invariant/architecture sections → dependency implementations/tests → current status evidence. Still use the one-slice transaction and normal review/merge gates.

### Debug a failure

Read failing test/source → relevant nested `AGENTS.md` → invariant/architecture docs only as needed. Run `./eng/doctor.ps1` for SDK/environment failures and `./eng/dotnet.ps1` for all .NET commands.

### Change persistence

Read `src/Telengard.Save/AGENTS.md` when present → save/version sections of spec → serializer/DTO/migrations → save tests → relevant Core state. For Godot persistence UI also read TEL-126 and the Godot/UX blueprints.

### Change Godot presentation

Read `presentation/GODOT_CLIENT_BLUEPRINT.md` → selected TEL-120–TEL-127 ticket → UX/art/asset blueprint(s) referenced by that ticket → `INVARIANTS.md` → Core presentation projection → Godot boundary. Preserve simulation authority and require ticket-specified manual observation.

### Review a diff

Use `$telengard-review`; focus on authority boundaries, determinism, save/version semantics, hidden information, scope, and missing verification. For Godot/client diffs add simulation-authority, projection-redaction, input-command, resource-ownership, and art-gate checks.
