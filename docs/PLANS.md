# Telengard ExecPlans

An ExecPlan is a living implementation document for work too large or risky to hold reliably in chat context. Use one for significant features/refactors, multi-ticket work, save/version migrations, broad architecture changes, or work expected to span many files/iterations. Small local fixes do not need an ExecPlan.

Store active plans in `docs/exec-plans/active/` and move completed plans to `docs/exec-plans/completed/` when the implementation and verification are complete.

## Required properties

An ExecPlan must be self-contained for a contributor who has only the current repository checkout. It must be updated as discoveries are made. Record facts and decisions, not hidden chain-of-thought.

Every plan contains these sections:

### Purpose / user-visible outcome
What changes when this plan is complete and how a human can observe it.

### Scope and non-goals
Exact TEL ticket(s), modules, and behavior in scope; explicit nearby work that is not in scope.

### Sources of truth
Relevant spec sections, TEL tickets, invariants, ADRs, existing code/tests, and any decisions that constrain the work.

### Current state
What exists before the change. Cite concrete files/types/tests so the plan does not rely on memory.

### Invariant impact
For each applicable contract, state how it is preserved: simulation authority, command/event ordering, determinism/RNG, hidden information, content separation, carried/secured wealth, renderer independence.

### Save/version impact
State `none` when truly none. Otherwise describe DTO, migration, save/simulation/generator/content version implications and backward-compatibility tests.

### Implementation plan
Ordered milestones expressed as observable repository changes. Each milestone should be independently understandable and testable.

### Validation
Exact focused checks and the final gate. For code changes the final gate is normally:

`./eng/verify.ps1 -Mode Full`

Also list any deterministic harness scenarios or manual presentation checks required by the task.

### Progress
Use timestamped checkboxes or short entries. Update this section as work completes.

### Surprises / discoveries
Record unexpected repository facts, stale docs, constraints, or test behavior that changed the plan.

### Decision log
Record consequential implementation decisions and why they were selected. Do not record private reasoning; capture the decision and evidence needed by future contributors.

### Results / remaining work
At completion, summarize what landed, verification evidence, and intentionally deferred follow-up. Never mark work complete when required checks did not run.
