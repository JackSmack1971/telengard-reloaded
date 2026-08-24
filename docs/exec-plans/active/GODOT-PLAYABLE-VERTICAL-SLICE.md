# Playable Godot Vertical Slice — umbrella ExecPlan

## Purpose / user-visible outcome

Coordinate the multi-ticket transition from the TEL-091 Modern renderer
prototype to a complete playable Godot floors 1-5 vertical slice while the
representative content pack is authored in parallel, then carry the project to
an explicit Art Production Ready handoff.

When this plan is complete, a player can launch Godot and complete the
representative Telengard loop using normal keyboard/controller interaction,
real first-slice content, explicit save/resume, and renderer-safe presentation
boundaries. The repository will also have passed the separate readiness gate
required before systematic final production-art/audio batches are authored.

## Scope and non-goals

In scope:

- Godot client/readiness tickets TEL-120 through TEL-128;
- coordination with TEL-110 through TEL-116 first-slice content dependencies;
- host/bootstrap, input/clock, scene/session, presentation/asset contracts,
  graybox rendering, UX, persistence, and full playable integration;
- acceptance evidence for `docs/gates/GODOT-PLAYABLE-SLICE.md` owned by TEL-127;
- readiness evidence for `docs/gates/ART-PRODUCTION-READY.md` owned by TEL-128.

Non-goals:

- final production art batches before the readiness gate;
- all 50 floors or broad content expansion;
- redesigning authoritative simulation systems without a demonstrated missing
  client-facing boundary;
- moving gameplay logic into Godot;
- combining multiple TEL implementation slices into one autonomous run/PR.

## Sources of truth

- `AGENTS.md`
- `.codex/skills/telengard-next-slice/SKILL.md`
- `docs/tasks/README.md`
- individual TEL-110–TEL-128 ticket files
- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`
- `docs/presentation/UX_INTERACTION_BLUEPRINT.md`
- `docs/presentation/ART_DIRECTION_BLUEPRINT.md`
- `docs/presentation/ASSET_PIPELINE_BLUEPRINT.md`
- `docs/gates/GODOT-PLAYABLE-SLICE.md`
- `docs/gates/ART-PRODUCTION-READY.md`
- `docs/modern-telengard-spec.md` §3, §12, §34, §36–§39, §43–§48, §51–§52
- `docs/INVARIANTS.md`
- current Core presentation types and `src/Telengard.Godot/`

## Current state

At plan creation:

- TEL-090–TEL-093 are implemented and verified; presentation separation and
  renderer-independent save compatibility are proven.
- TEL-107 provides a deterministic headless Core Alpha integration proof.
- TEL-109 provides the external versioned content-pack loader.
- TEL-110–TEL-116 remain the authored floors 1-5 content work.
- `src/Telengard.Godot/ModernRenderer.tscn` remains a visual-only renderer
  prototype that expects an externally supplied render frame.
- no complete Godot application host, command-input loop, production
  presentation contract, asset registry, UX/session flow, or client
  persistence flow exists.

## Invariant impact

### Simulation authority

All Godot gameplay intent crosses the simulation command/application boundary.
Godot may own transient presentation state only.

### Command/event ordering

State-changing intent is validated/resolved by the simulation. Presentation
responds to committed state/events and never fabricates authoritative outcomes.

### Determinism/RNG

The client introduces no authoritative ambient RNG. Simulation timing remains
independent of rendering FPS. Visual-only randomness, if later introduced, must
not affect authoritative replay.

### Hidden information

Production projections expose only legitimately observable information. Missing
visual data is solved by renderer-safe projection changes, not direct access to
hidden Core/content internals.

### Content separation

TEL-110–TEL-116 authored data remains in the content boundary. Presentation
asset mappings remain presentation-side and do not become simulation rules.

### Wealth/knowledge/death

Client flows call existing simulation boundaries and preserve carried/secured,
knowledge, and death-mode rules.

### Renderer independence

The playable Godot client may become richer than Terminal presentation but must
not change authoritative behavior or save meaning.

## Save/version impact

The umbrella plan does not presume a save-version change. Each selected ticket
must state its own impact. Presentation-only/client-shell state should not enter
GameState saves unless it becomes authoritative domain state, in which case the
owning ticket must explicitly use DTO/migration/version discipline.

## Implementation plan

### Milestone A — Parallel foundations

Complete content-independent client foundation while TEL-110–TEL-116 may
continue in parallel:

1. TEL-120 — playable Godot application host/bootstrap.
2. TEL-121 — input-to-command and simulation-clock bridge.
3. TEL-122 — client session/scene flow.
4. TEL-123 — production presentation contract and presentation asset registry.

Each remains a separate one-slice transaction.

### Milestone B — Content-aware graybox client

After explicit content dependencies are satisfied:

5. TEL-124 — dungeon/biome/monster/feature graybox presentation.
6. TEL-125 — HUD, combat, inventory, spell, journal and interaction flows.

### Milestone C — Playable integration

7. TEL-126 — save/suspend/resume and application lifecycle integration.
8. TEL-127 — full fixed-seed Playable Godot Vertical Slice gate acceptance.

A passed TEL-127 means the client is genuinely playable with production-shaped
placeholder/graybox presentation. It does not by itself authorize final asset
batches.

### Milestone D — Art-production handoff

9. TEL-128 — complete the separate Art Production Ready gate, including stable
   art-direction constraints, asset registry/pipeline/inventory evidence, UX
   stability, and binary policy.

Only after TEL-128 passes:

- author focused production-asset TEL tickets;
- keep them scoped to the representative first-slice inventory;
- preserve presentation registry and simulation boundaries.

Production-art tickets are intentionally not pre-created by this plan because
their inventory and batching should be derived from the passed readiness gate.

## Validation

Every code ticket uses focused checks plus the canonical repository gate:

`./eng/verify.ps1 -Mode Full`

Presentation-visible tickets must also exercise the required Godot surface.
TEL-127 requires a real Godot acceptance run; headless tests alone cannot pass
the playable gate. TEL-128 is separately blocked if required art-direction or
repository policy decisions remain unresolved.

The final plan validation includes:

- `docs/gates/GODOT-PLAYABLE-SLICE.md` completed with evidence via TEL-127;
- `docs/gates/ART-PRODUCTION-READY.md` completed with evidence via TEL-128;
- first-slice content from TEL-110–TEL-116 loaded through the production content
  pack rather than test-only fixtures;
- fixed-seed authoritative equivalence through save/resume;
- keyboard and controller client acceptance;
- validated first-slice production asset inventory and pipeline/readiness
  evidence.

## Progress

- [x] 2026-08-23 — repository-evidence audit identified TEL-091 as a visual
  prototype rather than a playable client and established the two-track
  development methodology.
- [x] 2026-08-23 — durable Godot/UX/art/asset blueprints and acceptance gates
  authored; TEL-120–TEL-128 client/readiness slices added to the task system.
- [x] 2026-08-23 — TEL-114 loot table data, references, validation, and
  deterministic carried-wealth acceptance evidence completed; save impact none.
- [x] 2026-08-23 — TEL-116 fountain, altar, pit, and teleporter definitions,
  complete-pack validation, deterministic loading, and resolver acceptance
  evidence completed; save impact none.
- [x] TEL-120 — application host/bootstrap (Godot 4.7.2 smoke-verified; authoritative bootstrap remains in the external .NET host).
- [x] TEL-121 — input/clock bridge (headless verification passed; interactive Godot observation pending).
- [ ] TEL-122 — session/scene flow (placeholder state shell headlessly verified; interactive observation pending).
- [ ] TEL-123 — production presentation/asset contract.
- [ ] TEL-124 — content-aware dungeon graybox.
- [ ] TEL-125 — HUD and interaction flows.
- [ ] TEL-126 — persistence/application lifecycle.
- [ ] TEL-127 — playable Godot vertical-slice acceptance.
- [ ] TEL-128 — Art Production Ready acceptance.

TEL-110–TEL-116 progress is tracked by the canonical task ledger and should not
be duplicated as authoritative status here.

## Surprises / discoveries

- The original Phase 9 presentation work intentionally proved separation before
  expensive presentation, but did not define the subsequent production-client
  phase.
- The Godot README explicitly leaves host adaptation and command submission to
  an external host, confirming the missing application layer.
- The current Modern projection is sufficient for prototype markers but should
  be audited before production scenes seek additional observable geometry/theme
  information.
- Feature definitions already have a presentation key while monster/item/spell
  definitions do not share one standardized presentation-key contract; the
  presentation-side registry avoids prematurely coupling engine resources to
  authoritative content schemas.
- Playable-client acceptance and production-art readiness are intentionally
  separate so unresolved visual direction cannot make TEL-127 ambiguously
  complete.

## Decision log

- **Two-track execution:** authored content and client foundation may progress in
  parallel; dependency graph controls convergence.
- **Placeholder first:** the playable client gate explicitly does not require
  final art.
- **Separate readiness ticket:** TEL-127 proves playability; TEL-128 alone
  authorizes production-art ticket creation.
- **Production-art hard gate:** systematic final asset batches wait for
  Art Production Ready evidence.
- **Presentation-side registry:** stable IDs map to Godot resources outside
  authoritative GameState/save state.
- **One ticket per run:** this umbrella plan coordinates work but does not weaken
  the repository's transactional autonomous workflow.

## Results / remaining work

The plan remains active until TEL-120–TEL-128 are complete and both presentation
gates have recorded passing evidence. At that point move this plan to
`docs/exec-plans/completed/` and author the first production-art batch tickets
from the validated first-slice asset inventory.
