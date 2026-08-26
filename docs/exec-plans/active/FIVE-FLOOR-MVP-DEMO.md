# Five-Floor MVP Demo — umbrella ExecPlan

## Status

**Active and highest-priority product plan.**

This plan takes precedence over broader presentation/readiness work until
TEL-132 passes or the user explicitly changes the milestone.

## Purpose / user-visible outcome

Turn the existing simulation, authored floors 1–5 content, Godot host, session
shell, graybox renderer, and HUD work into one uninterrupted playable demo.

When this plan is complete, a player can launch Godot, enter the Upper Ruins,
play through floors 1–5 with normal input, encounter representative authored
content, and reach a clear end-of-demo state without developer/debug commands.

## Why this plan exists

Repository evidence shows that the difficult domain systems already exist, but
the production-shaped Godot host does not yet compose them into a five-floor
session. The immediate risk is integration drift: completed Core/content/client
foundations can look closer to a playable product than the actual hosted path is.

The MVP plan closes that composition gap before additional persistence, UX
breadth, controller parity, or art-production readiness work.

## Scope

In scope:

- deterministic floor-layout composition for floors 1–5;
- legitimate stair transitions and the floor-1 exit boundary needed by the
  hosted session;
- content-driven encounters during normal movement;
- production first-slice feature instantiation/activation;
- production first-slice treasure/loot acquisition into unsecured progress;
- an explicit demo-ready player and gameplay configuration through existing Core
  boundaries;
- a coherent encounter-to-player-action combat path;
- a clear floor-5 demo completion state; and
- a fixed-seed real-Godot acceptance run.

## Non-goals

Out of scope until the MVP gate passes:

- Godot save/suspend/resume breadth;
- the complete broader Playable Godot Vertical Slice gate;
- Art Production Ready work or systematic final art/audio production;
- full client character-creation breadth;
- comprehensive inventory/equipment UX;
- combat item use;
- complete death/Legacy replacement UX;
- final balance or enemy AI; and
- all 50 floors.

The separate `GODOT-PLAYABLE-VERTICAL-SLICE.md` ExecPlan owns that later
continuation and remains intentionally distinct from this plan.

## Sources of truth

- `docs/MVP_DEMO.md`
- `docs/gates/FIVE-FLOOR-MVP-DEMO.md`
- `docs/tasks/README.md`
- `docs/tasks/TEL-129.md`
- `docs/tasks/TEL-130.md`
- `docs/tasks/TEL-131.md`
- `docs/tasks/TEL-132.md`
- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`
- `docs/presentation/UX_INTERACTION_BLUEPRINT.md`
- `docs/INVARIANTS.md`
- `docs/ARCHITECTURE.md`
- `docs/modern-telengard-spec.md`
- current Core/content/Godot implementation and tests

## Current state at plan creation — 2026-08-25

Implemented foundations already available to this plan include:

- deterministic dungeon generation, walking, visibility, and floor-transition
  rules in Core;
- encounters/combat, features, treasure, expedition state, progression,
  knowledge, death policies, and explicit save contracts;
- the external first-slice content pack covering floors 1–5 with representative
  monsters, encounter tables, items, loot, spells, and features;
- an external authoritative Godot host/bootstrap;
- implemented input/clock and session/scene-flow bridges with broader manual
  acceptance still pending elsewhere;
- renderer-safe production presentation identity and asset-registry support;
- implemented content-aware graybox dungeon presentation; and
- partial HUD/combat-intent interaction work.

Observed integration gaps that block the five-floor demo:

1. The current Godot host retains one generated floor-1 layout rather than a
   floor-aware deterministic layout provider/cache.
2. The hosted client does not expose the Core floor-change/leave flow required
   for legitimate floors 1–5 traversal.
3. Normal hosted movement is not yet composed with the authored encounter table.
4. Production feature instances/outcomes are not yet composed into the hosted
   dungeon session.
5. Production loot/treasure acquisition is not yet connected to normal hosted
   play.
6. The desktop launch path needs explicit demo gameplay configuration/player
   setup sufficient for a reliable combat demonstration.
7. A fixed-seed real-Godot floor-1-to-floor-5 acceptance route has not yet been
   recorded.

## Implementation sequence

### Milestone A — TEL-129: multi-floor hosted session

Compose deterministic layouts for floors 1–5 behind the Godot host/application
boundary. Movement always resolves against the player's current floor layout;
stair input invokes the existing authoritative floor-transition resolver; floor
changes update the rendered projection without Godot owning geography.

Exit criterion: a hosted test/observation can legitimately enter floor 1 and
move between adjacent first-slice floors using their own generated layouts.

### Milestone B — TEL-130: production runtime ecology

Connect the loaded first-slice content pack to normal hosted play: encounter
configuration, deterministic feature placement/activation, and loot/treasure
acquisition. Reuse existing Core/content contracts rather than adding renderer
rules.

Exit criterion: normal movement/interact play can produce an authored encounter,
an authored feature interaction, and unsecured treasure in the hosted session.

### Milestone C — TEL-131: demo setup and combat closure

Provide explicit demo-only configuration/setup through existing boundaries and
close the hosted encounter-phase flow so the player can reach and resolve a
usable combat action. Do not turn demo tuning into a hidden permanent balance
policy.

Exit criterion: the fixed demo start can reliably enter an encounter, receive
appropriate feedback, take an authoritative combat action, and continue the run.

### Milestone D — TEL-132: fixed-seed five-floor acceptance

Perform the real Godot route from startup through the designated floor-5
endpoint with no debug commands. Record exact seed/configuration, runtime,
player-input route, focused checks, and canonical verification.

Exit criterion: `docs/gates/FIVE-FLOOR-MVP-DEMO.md` passes.

## Relationship to the broader Godot plan

`GODOT-PLAYABLE-VERTICAL-SLICE.md` remains the post-MVP continuation plan. It
becomes the primary convergence again after this plan's acceptance gate passes.
This plan intentionally does not own the later persistence, full-client, or
art-readiness tickets.

## Validation policy

Each implementation ticket remains one reviewable transaction and uses focused
checks plus the canonical repository gate. Godot-visible tickets require the
observation specified by their ticket. TEL-132 specifically requires a real
Godot acceptance run; a headless-only proof cannot pass the MVP gate.

## Progress

- [x] 2026-08-25 — repository audit identified the production-host composition
  gaps that prevent legitimate five-floor play despite completed foundations.
- [x] 2026-08-25 — Five-Floor MVP Demo milestone, gate, task sequence, and
  scheduling priority documented.
-- [x] TEL-129 — deterministic floors 1–5 hosted session composition implemented,
  verified, and accepted after the Godot 4.7.2 five-floor traversal.
- [x] TEL-130 — production encounter/feature/treasure composition implemented;
  interactive Godot observation remains pending.
- [ ] TEL-131 — demo setup and combat playthrough closure.
- [ ] TEL-132 — fixed-seed five-floor Godot acceptance.

## Decision log

- **MVP before breadth:** prove five uninterrupted playable floors before adding
  persistence/polish breadth.
- **Fixed seed is allowed:** reproducibility is more valuable than setup breadth
  for the first demo.
- **Demo tuning stays explicit:** temporary MVP configuration does not silently
  become permanent balance policy.
- **Placeholder presentation is sufficient:** production art remains behind the
  later readiness gate.
- **No architecture shortcut:** Godot remains presentation/application intent;
  Core remains authoritative.
