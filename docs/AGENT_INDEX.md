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
| What is implemented now | code + tests | `BUILD_STATUS.md` |
| Phase acceptance | `gates/PHASE-*.md` | current test/build evidence |
| Long-running work | `PLANS.md` | active plan under `exec-plans/active/` |
| Codex/runtime workflow | `CODEX.md` | `AGENTS.md`, `eng/*.ps1`, `.codex/` |

## Important status rule
Task-ledger text can lag implementation. Treat working code/tests as the present-state evidence and `modern-telengard-spec.md` as product intent. Never remove implemented behavior solely because a ledger line still says “Not started”; fix the stale status record when appropriate.

## Current code map

- `src/Telengard.Core/Events/` — domain event bus.
- `src/Telengard.Core/Rng/` — deterministic RNG.
- `src/Telengard.Core/Simulation/` — game state and command dispatcher.
- `src/Telengard.Core/world/generation/` — floor generation/walking/transitions.
- `src/Telengard.Core/world/visibility/` — tile visibility.
- `src/Telengard.Save/` — explicit save DTOs, serializer, migrations.
- `src/Telengard.Terminal/` — terminal presentation entry point.
- `src/Telengard.Godot/` — separate Godot presentation placeholder/project.
- `tests/Telengard.Architecture.Tests/` — architecture + domain behavior tests.
- `tools/Telengard.TestHarness/` — scripted deterministic simulation harness.
- `content/` — reserved data-defined content areas.

## Context recipes

### Implement a TEL ticket
Read: root `AGENTS.md` → this index → task file → cited spec sections → relevant invariant/architecture sections → dependency implementations/tests → current `BUILD_STATUS.md`. Use `$telengard-ticket`.

### Debug a failure
Read: failing test/source → relevant nested `AGENTS.md` → invariant/architecture docs only as needed. Run `./eng/doctor.ps1` for SDK/environment failures and `./eng/dotnet.ps1` for all .NET commands.

### Change persistence
Read: `src/Telengard.Save/AGENTS.md` → save/version sections of spec → serializer/DTO/migrations → save tests → relevant Core state.

### Review a diff
Use `$telengard-review`; focus on authority boundaries, determinism, save/version semantics, hidden information, scope, and missing verification.
