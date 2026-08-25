# Playable Godot Vertical Slice — umbrella ExecPlan

## Status

**Post-MVP continuation plan.**

The broader Playable Godot Vertical Slice remains valid, but it is not the
immediate scheduling target. The active highest-priority plan is
[`FIVE-FLOOR-MVP-DEMO.md`](docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md). Resume this plan as primary
convergence only after the Five-Floor MVP Demo gate passes or the user explicitly
changes priority.

## Purpose / user-visible outcome

Coordinate the transition from the hosted/grayboxed Godot client into a complete
production-shaped playable first slice with explicit save/resume, full required
interaction coverage, death/Legacy flow, keyboard/controller acceptance, and the
separate Art Production Ready handoff.

When this broader plan is complete, a player can launch Godot and complete the
representative Telengard loop using normal keyboard/controller interaction, real
first-slice content, explicit save/resume, and renderer-safe presentation
boundaries. The repository will also have passed the separate readiness gate
required before systematic final production-art/audio batches are authored.

## Relationship to the Five-Floor MVP Demo

The MVP is a deliberately smaller product checkpoint that comes first. It proves
legitimate floors 1–5 traversal, representative production runtime ecology, a
usable combat path, and a clear fixed-seed end-of-demo state.

This plan owns the broader client/readiness work after that checkpoint:

- remaining input/session manual acceptance;
- broad HUD/interaction acceptance;
- Godot save/suspend/resume lifecycle;
- full required keyboard/controller coverage;
- load/recovery/error flows;
- death/Legacy replacement and second-expedition acceptance;
- the full Playable Godot Vertical Slice gate; and
- Art Production Ready evidence.

The MVP milestone does not weaken any architecture, determinism, save,
hidden-information, or content-separation contract in this plan.

## Scope and non-goals

In scope after MVP completion:

- remaining TEL-121/TEL-122 manual acceptance evidence where still outstanding;
- remaining TEL-125 HUD/interaction acceptance breadth;
- TEL-126 save/suspend/resume and application lifecycle integration;
- TEL-127 full `GODOT-PLAYABLE-SLICE.md` acceptance;
- TEL-128 `ART-PRODUCTION-READY.md` acceptance; and
- coordination of production-art readiness only after client behavior is stable.

Non-goals:

- final production art batches before TEL-128;
- all 50 floors or broad content expansion;
- redesigning authoritative simulation systems without a demonstrated missing
  client-facing boundary;
- moving gameplay logic into Godot; and
- combining multiple TEL implementation slices into one autonomous run/PR.

## Sources of truth

- `docs/MVP_DEMO.md` for the preceding product checkpoint;
- `docs/gates/FIVE-FLOOR-MVP-DEMO.md` for its prerequisite acceptance;
- `docs/tasks/README.md` and generated `docs/tasks/index.json` for current status/scheduling;
- individual TEL-110 through TEL-128 ticket files;
- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`;
- `docs/presentation/UX_INTERACTION_BLUEPRINT.md`;
- `docs/presentation/ART_DIRECTION_BLUEPRINT.md`;
- `docs/presentation/ASSET_PIPELINE_BLUEPRINT.md`;
- `docs/gates/GODOT-PLAYABLE-SLICE.md`;
- `docs/gates/ART-PRODUCTION-READY.md`;
- `docs/modern-telengard-spec.md`;
- `docs/INVARIANTS.md`; and
- current implementation/tests.

## Current state — reconciled 2026-08-25

The previous version of this plan had status drift relative to current ticket and
implementation evidence. The reconciled state is:

- TEL-110–TEL-116 — representative first-slice content: implemented and verified.
- TEL-120 — application host/bootstrap: implemented and verified.
- TEL-121 — input/clock bridge: implementation/headless verification exists;
  broader interactive/manual observation remains pending, so ledger status is
  `In progress`.
- TEL-122 — session/scene flow: implementation and runtime probe exist; broader
  visual/manual observation remains pending, so ledger status is `In progress`.
- TEL-123 — production presentation/asset contract: implemented and verified.
- TEL-124 — content-aware dungeon graybox: implemented and verified; the former
  `In progress` line in this plan was stale.
- TEL-125 — HUD/interaction flows: in progress; HUD and combat-intent bridging
  exist, but full interaction acceptance is outstanding.
- TEL-126 — save/suspend/resume lifecycle: not started and intentionally
  post-MVP.
- TEL-127 — full playable-slice acceptance: not started and intentionally
  post-MVP.
- TEL-128 — Art Production Ready: not started and intentionally post-MVP.

The separate Five-Floor MVP plan owns the current prerequisite integration work;
this plan deliberately does not duplicate those TEL owners.

## Invariant impact

### Simulation authority

All Godot gameplay intent crosses simulation/application boundaries. Godot owns
transient presentation state only.

### Command/event ordering

State-changing intent is validated/resolved by the simulation. Presentation
responds to committed state/events and never fabricates authoritative outcomes.

### Determinism/RNG

The client introduces no authoritative ambient RNG. Simulation timing remains
independent of rendering FPS. Visual-only randomness must not affect replay.

### Hidden information

Production projections expose only legitimately observable information. Missing
visual data is solved through renderer-safe projection changes, not direct
access to hidden Core/content internals.

### Content separation

Authored first-slice data remains in the content boundary. Presentation asset
mappings remain presentation-side and do not become simulation rules.

### Wealth/knowledge/death

Client flows call existing simulation boundaries and preserve carried/secured,
knowledge, and death-mode rules.

### Renderer independence

The playable Godot client may become richer than Terminal presentation but must
not change authoritative behavior or save meaning.

## Save/version impact

This umbrella plan does not presume a save-version change. Each selected ticket
must state its own impact. Presentation-only/client-shell state should not enter
`GameState` saves unless it becomes authoritative domain state, in which case
the owning ticket must explicitly use DTO/migration/version discipline.

## Implementation plan after MVP

### Milestone A — finish interaction/manual acceptance gaps

Close remaining TEL-121/TEL-122/TEL-125 acceptance evidence required by the
broader client gate and intentionally not required by the MVP.

### Milestone B — TEL-126 persistence/application lifecycle

Expose existing save/suspension boundaries through Godot, verify fixed-seed
authoritative equivalence across save/resume, and rebuild presentation from
loaded authoritative state.

### Milestone C — TEL-127 Playable Godot Vertical Slice

Complete the full checklist in `docs/gates/GODOT-PLAYABLE-SLICE.md`, including
startup/new/load, required input coverage, representative loop breadth,
persistence, death/Legacy behavior, second expedition, and required manual
Godot observations.

### Milestone D — TEL-128 Art Production Ready

Only after TEL-127 passes, complete the separate art-direction, asset-pipeline,
UX stability, and repository-policy readiness gate. Systematic final art/audio
TEL batches remain prohibited until this gate passes.

## Validation

Every code ticket uses focused checks plus the canonical repository gate:

`./eng/verify.ps1 -Mode Full`

Presentation-visible tickets must also exercise their required Godot surface.
TEL-127 requires a real Godot acceptance run; headless tests alone cannot pass
the playable gate. TEL-128 remains separately blocked if required art-direction
or repository policy decisions are unresolved.

## Progress

### Completed foundation/client/content work

- [x] TEL-110–TEL-116 — first-slice production content.
- [x] TEL-120 — Godot application host/bootstrap.
- [x] TEL-123 — production presentation/asset contract.
- [x] TEL-124 — content-aware dungeon graybox.

### Partially accepted existing work

- [ ] TEL-121 — implementation/headless evidence exists; broader manual
  observation remains.
- [ ] TEL-122 — implementation/runtime probe exists; broader visual/manual
  observation remains.
- [ ] TEL-125 — HUD/combat-intent bridge exists; full interaction acceptance
  remains.

### Post-MVP continuation

- [ ] TEL-126 — persistence/application lifecycle.
- [ ] TEL-127 — full playable Godot vertical-slice acceptance.
- [ ] TEL-128 — Art Production Ready acceptance.

## Decision log

- **Five-floor MVP first:** integration proof precedes persistence/polish breadth.
- **Placeholder first:** neither the MVP nor TEL-127 requires final art.
- **Separate readiness ticket:** TEL-127 proves broad playability; TEL-128 alone
  authorizes production-art ticket creation.
- **Production-art hard gate:** systematic final asset batches wait for Art
  Production Ready evidence.
- **Presentation-side registry:** stable IDs map to Godot resources outside
  authoritative `GameState`/save state.
- **One ticket per run:** umbrella plans coordinate work but do not weaken the
  transactional autonomous workflow.

## Results / remaining work

This plan remains active as a post-MVP continuation record. Its implementation
queue becomes primary again after the separate MVP gate passes. At that point
finish TEL-126, TEL-127, and TEL-128, then move this plan to
`docs/exec-plans/completed/` only when both broader presentation gates have
passing evidence.
