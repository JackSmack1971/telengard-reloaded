---
name: telengard-review
description: Independently review a Telengard Reloaded implementation diff before merge. Use after a slice is implemented, when reviewing a PR/branch/commit, or whenever the next-slice workflow reaches its independent-review phase. Coordinate separate reviewer agents for correctness, architecture/determinism/save contracts, tests/regression risk, documentation/status provenance, and presentation boundaries when relevant.
---

# Telengard Independent Review

Review the actual final diff against its base branch. Do not rely on the implementer's summary as evidence.

Read root `AGENTS.md`, the selected TEL ticket, relevant invariants, and only the architecture/spec/code needed for each lane. For TEL-110–TEL-128 or Godot/client/readiness work, also read the blueprint/gate documents referenced by the ticket and the active `GODOT-PLAYABLE-VERTICAL-SLICE` ExecPlan.

## Orchestration

Use independent subagents when available. Give each reviewer the base/head refs or complete diff target plus the selected ticket. Reviewers should not share conclusions before producing their own findings.

Run these lanes in parallel when practical:

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
- accidental hard-coding of unresolved configuration/tuning/design decisions.

### Lane C — Tests and regression evidence

Inspect for:

- missing acceptance-level behavior tests;
- tests that only mirror implementation details;
- missing negative/boundary/deterministic/event-order/save tests;
- weak assertions that could pass despite a broken feature;
- untested public API or migration behavior;
- verification commands inconsistent with repository policy;
- headless tests being presented as proof of a Godot-visible behavior that the ticket requires to be observed manually.

### Lane D — Documentation and provenance

Inspect:

- selected TEL ticket status vs actual implementation;
- `docs/tasks/README.md` status consistency;
- `docs/BUILD_STATUS.md` append-only semantics;
- ExecPlan progress/completion when one exists;
- generated audit/status views and generator usage;
- README/DEVELOPMENT/INVARIANTS/ADR updates only where durable contracts changed;
- stale statements, false verification claims, or undocumented deferred work;
- for TEL-120–TEL-128, synchronization with the active Godot umbrella ExecPlan and relevant gate state;
- TEL-127 only claiming Playable Godot Slice acceptance and TEL-128 separately owning Art Production Ready;
- no production-art TEL work being marked eligible before TEL-128 / `ART-PRODUCTION-READY` has passing evidence.

## Presentation lane

Add this lane whenever Terminal/Godot/UI/presentation behavior or renderer-facing projections/resources change. For Godot/client/readiness work read:

- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`;
- the selected TEL ticket's required blueprint context;
- `docs/gates/GODOT-PLAYABLE-SLICE.md` for TEL-127/playable claims;
- `docs/gates/ART-PRODUCTION-READY.md` for TEL-128/readiness claims.

Inspect for:

- direct Godot/UI/scene/animation mutation of authoritative `GameState`;
- UI callbacks or animation signals resolving combat, movement, feature, wealth,
  item, knowledge, or death outcomes rather than submitting commands;
- renderer FPS affecting authoritative simulation behavior;
- Godot scenes reading hidden Core/content internals because the presentation
  projection is incomplete;
- presentation projections leaking raw danger, unobserved geography, hidden
  monster stats, hidden feature outcomes, or other unearned knowledge;
- duplicated gameplay/content rules in Godot scripts/scenes;
- Godot resource paths, scene UIDs, texture/audio paths, or transient scene
  objects leaking into authoritative save DTOs/state;
- content/presentation identity mapped by scattered hard-coded scene conditions
  instead of the presentation asset registry once TEL-123 owns that boundary;
- silent fallback for missing required production mappings;
- required keyboard/controller/manual Godot observation missing from evidence;
- final production assets being introduced before TEL-128 passes Art Production
  Ready, except explicit concept/style/placeholder work permitted by the
  blueprint;
- TEL-128 inventing unresolved art direction, binary/LFS policy, or other
  product/repository policy simply to make the gate pass;
- final assets masking an unresolved scale, camera, UX, or presentation-contract
  problem that should remain a placeholder until fixed.

A visible Godot change with required manual acceptance cannot pass this lane on
headless tests alone.

## Security lane

Add a security lane when the diff changes trust boundaries, external inputs, credentials, workflow permissions, serialization attack surface, file/resource loading, or other security-sensitive behavior.

For future asset/resource registry work, include path validation, malformed resource/manifest handling, and untrusted external asset metadata as appropriate to the ticket.

## Finding standard

Return only actionable findings that are supported by repository evidence. Each finding must include:

- severity: `P0`, `P1`, `P2`, or `P3`;
- concrete file path and line/range when possible;
- the failure/risk;
- why it violates a ticket, invariant, contract, blueprint/gate, or expected behavior;
- the smallest reasonable correction or missing test/observation.

Do not invent findings to fill a quota. If a lane finds no actionable issues, report that lane as clean.

Prioritize:

- P0: destructive/catastrophic correctness or security issue; never merge.
- P1: ticket failure, invariant violation, determinism/save corruption, presentation-authority violation, hidden-information leak, or serious regression; never merge.
- P2: meaningful correctness/test/docs/acceptance defect that should be fixed before merge.
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
lanes: correctness=<pass/fail>, architecture=<pass/fail>, tests=<pass/fail>, docs=<pass/fail>, presentation=<pass/fail|n/a>, security=<pass/fail|n/a>
```

Any unresolved P0/P1/P2 means `changes-required`. P3 findings may remain only when explicitly judged non-blocking and out of proportion to the selected slice.
