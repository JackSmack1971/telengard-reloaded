# Telengard ExecPlans

An ExecPlan is a living implementation document for work too large or risky to hold reliably in chat context. Use one for significant features/refactors, multi-ticket work, save/version migrations, broad architecture changes, or work expected to span many files/iterations. Small local fixes do not need an ExecPlan.

Store active plans in `docs/exec-plans/active/` and move completed plans to `docs/exec-plans/completed/` when the implementation and verification are complete.

## Umbrella plans and one-slice transactions

A multi-ticket/umbrella ExecPlan may coordinate a milestone across many TEL tickets, dependency tracks, or acceptance gates. It does **not** override the repository's one-slice-per-run rule. `$telengard-next-slice` still selects, implements, verifies, reviews, and merges at most one TEL slice per run.

When a selected ticket belongs to an active umbrella plan, update only the durable progress/decision/discovery state affected by that ticket. The canonical task ledger remains `docs/tasks/README.md`; an ExecPlan is coordination state, not an alternate status ledger.

The current example is `docs/exec-plans/active/GODOT-PLAYABLE-VERTICAL-SLICE.md`, which coordinates the TEL-110–TEL-116 content track with TEL-120–TEL-127 Godot-client work and the playable/art-readiness gates.

## Required properties

An ExecPlan must be self-contained for a contributor who has only the current repository checkout. It must be updated as discoveries are made. Record facts and decisions, not hidden chain-of-thought.

Every plan contains these sections:

### Purpose / user-visible outcome
What changes when this plan is complete and how a human can observe it.

### Scope and non-goals
Exact TEL ticket(s), modules, and behavior in scope; explicit nearby work that is not in scope.

### Sources of truth
Relevant spec sections, TEL tickets, invariants, ADRs, existing code/tests, blueprints/gates when applicable, and any decisions that constrain the work.

### Current state
What exists before the change. Cite concrete files/types/tests so the plan does not rely on memory.

### Invariant impact
For each applicable contract, state how it is preserved: simulation authority, command/event ordering, determinism/RNG, hidden information, content separation, carried/secured wealth, renderer independence. Presentation plans should also state resource ownership and required observation boundaries.

### Save/version impact
State `none` when truly none. Otherwise describe DTO, migration, save/simulation/generator/content version implications and backward-compatibility tests.

### Implementation plan
Ordered milestones expressed as observable repository changes. Each milestone should be independently understandable and testable. For multi-track plans, record explicit convergence dependencies rather than implying numerical ticket order.

### Validation
Exact focused checks and the final gate. For code changes the final headless gate is normally:

`./eng/verify.ps1 -Mode Full`

Also list deterministic harness scenarios, Godot/manual presentation checks, or other acceptance gates required by the task. Headless verification does not replace explicitly required user-visible observation.

### Progress
Use timestamped checkboxes or short entries. Update this section as work completes. Do not duplicate the entire canonical task ledger.

### Surprises / discoveries
Record unexpected repository facts, stale docs, constraints, or test/observation behavior that changed the plan.

### Decision log
Record consequential implementation decisions and why they were selected. Do not record private reasoning; capture the decision and evidence needed by future contributors.

### Results / remaining work
At completion, summarize what landed, verification evidence, required presentation/acceptance evidence, and intentionally deferred follow-up. Never mark work complete when required checks or observations did not run.
