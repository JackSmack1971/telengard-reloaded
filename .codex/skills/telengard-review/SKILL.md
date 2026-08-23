---
name: telengard-review
description: Independently review a Telengard Reloaded implementation diff before merge. Use after a slice is implemented, when reviewing a PR/branch/commit, or whenever the next-slice workflow reaches its independent-review phase. Route only the review lanes justified by ticket and diff risk, with correctness as the default target.
---

# Telengard Independent Review

Review the actual final diff against its base branch. Do not rely on the implementer's summary as evidence.

## Contents

- Route independent lanes from ticket/diff risk
- Review correctness, architecture, tests, and provenance as selected
- Escalate only for distinct falsification targets
- Synthesize actionable findings into `REVIEW_RESULT`

Read root `AGENTS.md`, the selected TEL ticket, its context manifest, relevant
index risk tags, and only the invariants/spec/code needed for the selected
review lanes.

The parent always performs a complete self-review. Independent reviewers are
reserved for distinct falsification targets; do not create a reviewer merely to
repeat a general code review. Runtime model/profile selection remains external
configuration and is not hard-coded in this skill. Apply the role defaults and
escalation rules in `docs/CODEX_MODEL_POLICY.md`.

## Risk-adaptive lane routing

Classify the actual diff together with the selected ticket metadata before
starting reviewers. Use the smallest lane set that can independently challenge
the changed behavior:

| Evidence in ticket/diff | Required independent lanes |
| --- | --- |
| docs-only typo or routing correction | documentation/provenance |
| normal content-definition addition | correctness, tests |
| ordinary Core feature | correctness, tests |
| architecture or public API boundary | correctness, architecture |
| RNG or determinism path | correctness, architecture/determinism, tests |
| save DTO or migration | correctness, save/architecture, tests; elevate the strongest lane |
| Godot/UI/projection behavior | correctness, presentation; tests when applicable |
| workflow, security, or trust boundary | correctness, security |
| gate-claim ticket (for example TEL-127/128) | gate-specific, correctness |

Add a lane only when it has a distinct falsification target. If multiple rows
apply, take their union and deduplicate equivalent targets. If the diff is
ambiguous, use the smallest safe escalation and state the reason.

Use independent subagents when available. Give each reviewer the base/head refs
or complete diff target, selected ticket, and its lane target. Reviewers should
not share conclusions before producing their own findings.

## Review lanes

### Lane A — Correctness and edge cases

Inspect for:

- behavior that does not satisfy the ticket's acceptance criteria;
- invalid state transitions or mutation before validation;
- off-by-one/range/null/empty/error-path problems;
- incorrect event payload/order or missing committed events;
- unintended behavior outside the selected slice;
- regressions in adjacent subsystem behavior.

### Lane B — Architecture, invariants, determinism, saves

Inspect against `docs/INVARIANTS.md`, `docs/ARCHITECTURE.md`, ADRs, and ticket constraints:

- simulation vs renderer authority;
- deterministic replay and RNG stream derivation;
- hidden-information/knowledge leakage;
- content-definition separation;
- carried vs secured progress;
- save DTO, migration, schema/version, backward compatibility, and replay impact;
- accidental hard-coding of unresolved configuration/tuning decisions.

### Lane C — Tests and regression evidence

Inspect for:

- missing acceptance-level behavior tests;
- tests that only mirror implementation details;
- missing negative/boundary/deterministic/event-order/save tests;
- weak assertions that could pass despite a broken feature;
- untested public API or migration behavior;
- verification commands inconsistent with repository policy.

### Lane D — Documentation and provenance

Inspect:

- selected TEL ticket status vs actual implementation;
- `docs/tasks/README.md` status consistency;
- `docs/BUILD_STATUS.md` append-only semantics;
- ExecPlan progress/completion when one exists;
- generated audit/status views and generator usage;
- README/DEVELOPMENT/INVARIANTS/ADR updates only where durable contracts changed;
- stale statements, false verification claims, or undocumented deferred work.

## Additional lanes

Add a presentation-observation lane when Terminal/Godot/UI behavior changed. Add a security lane when the diff changes trust boundaries, external inputs, credentials, workflow permissions, serialization attack surface, or other security-sensitive behavior.

## Finding standard

Return only actionable findings that are supported by repository evidence. Each finding must include:

- severity: `P0`, `P1`, `P2`, or `P3`;
- concrete file path and line/range when possible;
- the failure/risk;
- why it violates a ticket, invariant, contract, or expected behavior;
- the smallest reasonable correction or missing test.

Do not invent findings to fill a quota. If a lane finds no actionable issues, report that lane as clean.

Prioritize:

- P0: destructive/catastrophic correctness or security issue; never merge.
- P1: ticket failure, invariant violation, determinism/save corruption, or serious regression; never merge.
- P2: meaningful correctness/test/docs defect that should be fixed before merge.
- P3: low-risk maintainability or clarity issue; fix when proportional and in scope.

## Synthesis

Deduplicate only truly identical findings; preserve distinct evidence from different lanes when it reveals separate risks.

Conclude with:

```text
REVIEW_RESULT
verdict: pass | changes-required
p0: <count>
p1: <count>
p2: <count>
p3: <count>
lanes: <selected lane>=<pass/fail>, ...
```

Any unresolved P0/P1/P2 means `changes-required`. P3 findings may remain only when explicitly judged non-blocking and out of proportion to the selected slice.
