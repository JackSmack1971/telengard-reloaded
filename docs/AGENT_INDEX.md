# Agent context index

Use this page to choose the smallest context set for a task. The repository specification is intentionally larger than the instructions loaded on every Codex turn.

## Source-of-truth routing

| Need | Read first | Then verify against |
|---|---|---|
| Current product milestone | `MVP_DEMO.md` | `tasks/index.json` + active MVP ExecPlan/gate |
| Next-slice selection | `tasks/index.json` | selected ticket + active PR/CI state |
| Product/game behavior | `modern-telengard-spec.md` | relevant TEL ticket + tests/code |
| Cross-cutting contracts | `INVARIANTS.md` | architecture tests + implementation |
| Dependency/authority design | `ARCHITECTURE.md` | project references + code |
| Tech-stack decision | `adr/ADR-001-technology-stack.md` | `global.json`, `Directory.Build.props`, projects |
| A TEL ticket | `tasks/TEL-xxx.md` | spec sections, dependencies, code/tests, `BUILD_STATUS.md` |
| What is implemented now | code + tests | ticket verification evidence + `BUILD_STATUS.md` |
| MVP acceptance | `gates/FIVE-FLOOR-MVP-DEMO.md` | TEL-129–TEL-132 + real Godot observation |
| Broader playable-slice acceptance | `gates/GODOT-PLAYABLE-SLICE.md` | only after the MVP gate passes |
| Long-running work | `PLANS.md` | highest-priority active plan under `exec-plans/active/` |
| Codex/runtime workflow | `CODEX.md` | `AGENTS.md`, `eng/*.ps1`, `.codex/` |

## Important status rule

Task-ledger text can lag implementation. Treat working code/tests as present-state evidence and `modern-telengard-spec.md` as long-term product intent. Never remove implemented behavior solely because a ledger line still says “Not started”; fix the stale status record when appropriate.

The immediate sequencing authority is now explicit:

- `MVP_DEMO.md` defines the Five-Floor MVP product checkpoint;
- `tasks/README.md` is the TEL-ticket status ledger;
- `tasks/index.json` is the generated scheduling projection;
- `exec-plans/active/FIVE-FLOOR-MVP-DEMO.md` coordinates the current critical path;
- `gates/FIVE-FLOOR-MVP-DEMO.md` defines TEL-132 acceptance;
- `docs/audit-status.json` remains the canonical audit-remediation ledger; and
- `BUILD_STATUS.md` remains append-only verification history.

TEL-126, TEL-127, and TEL-128 are post-MVP work. A normal next-slice selection should prefer TEL-129 through TEL-132 in dependency order unless current repository evidence shows a blocker or the user explicitly changes priority.

## Current code map

- `src/Telengard.Core/Events/` — domain event bus.
- `src/Telengard.Core/Rng/` — deterministic RNG.
- `src/Telengard.Core/Simulation/` — game state and command dispatcher.
- `src/Telengard.Core/world/generation/` — floor generation/walking/transitions.
- `src/Telengard.Core/world/visibility/` — tile visibility.
- `src/Telengard.Save/` — explicit save DTOs, serializer, migrations.
- `src/Telengard.Terminal/` — terminal presentation entry point.
- `src/Telengard.Godot/` — hosted Godot client shell, input/session state, graybox renderer, and presentation registry.
- `tools/Telengard.GodotHost/` — external authoritative Core/content composition and transport boundary used by Godot.
- `tests/Telengard.Architecture.Tests/` — architecture + domain/integration behavior tests.
- `tools/Telengard.TestHarness/` — scripted deterministic simulation harness.
- `content/` — production data-driven first-slice content pack and future content areas.

## Context recipes

### Implement the next MVP ticket

Read: root `AGENTS.md` → `MVP_DEMO.md` → `tasks/index.json` → selected TEL-129–TEL-132 ticket context manifest → active MVP ExecPlan/gate → only then relevant blueprint/invariant/architecture/source/test context. Do not load TEL-126–TEL-128 as candidate work while the MVP chain is healthy.

### Implement another TEL ticket

Read: root `AGENTS.md` → `tasks/index.json` → selected task's context manifest → cited spec sections → relevant invariant/architecture sections → dependency implementations/tests → current `BUILD_STATUS.md` evidence. A non-MVP selection during the current milestone requires a documented blocker/prerequisite reason.

### Debug a failure

Read: failing test/source → relevant nested `AGENTS.md` → invariant/architecture docs only as needed. Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/doctor.ps1` for SDK/environment failures and `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./eng/dotnet.ps1` for all .NET commands.

### Change persistence

Read: `src/Telengard.Save/AGENTS.md` → save/version sections of spec → serializer/DTO/migrations → save tests → relevant Core state. During the current milestone, persistence work should be selected only when it is a demonstrated MVP blocker; normal TEL-126 breadth is post-MVP.

### Review a diff

Use `$telengard-review`; focus on authority boundaries, determinism, save/version semantics, hidden information, scope, tests, documentation/provenance, and presentation/security lanes when relevant.
