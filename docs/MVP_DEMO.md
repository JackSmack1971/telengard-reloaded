# Five-Floor MVP Demo

Status: **current product milestone** as of 2026-08-25.

## Purpose

The immediate goal for Telengard Reloaded is a narrow, demonstrable Godot MVP:
a player can launch the game, enter the Upper Ruins, play through floors 1–5
using normal player input, and reach a clear end-of-demo state.

This milestone exists to force the already-implemented simulation, authored
first-slice content, and Godot client shell to compose into one uninterrupted
player experience before the project resumes the broader Playable Godot Vertical
Slice checklist.

The MVP is intentionally smaller than Core Alpha completeness, TEL-127, or Art
Production Ready. It does not replace those milestones; it orders them.

## Player-visible success definition

A passing MVP build lets a player, without developer/debug commands:

1. launch the Godot client into a usable demo start flow;
2. begin from an authoritative inn/demo-ready state;
3. enter floor 1 of the real first-slice dungeon;
4. traverse legitimate dungeon geometry and use real stairs to reach floors 2,
   3, 4, and 5;
5. encounter at least one authored first-slice monster through the normal
   encounter pipeline;
6. resolve a playable combat path through the authoritative Core command flow;
7. interact with at least one authored dungeon feature through the normal
   feature pipeline;
8. acquire at least one first-slice treasure reward into unsecured expedition
   state;
9. see enough HUD/feedback to understand floor, survival state, encounter state,
   and carried progress; and
10. reach a clear floor-5 demo endpoint such as an explicit
    `End of Telengard Reloaded Demo` presentation state.

A fixed world seed, fixed/demo character policy, placeholder/graybox visuals,
and explicitly supplied demo tuning are acceptable for this milestone.

## Required architectural constraints

The MVP must preserve the repository's existing contracts:

- Core remains authoritative for gameplay state and outcomes;
- Godot submits intent and renders renderer-safe projections/events;
- floor layouts, encounters, features, loot, and combat remain deterministic for
  stable inputs;
- authored content comes through the production `ContentPack` boundary rather
  than test-only fixtures;
- hidden information remains hidden until legitimately observed;
- unresolved permanent balance/tuning remains configurable rather than being
  silently frozen for the sake of the demo.

## Explicit non-goals

The MVP does **not** require:

- save/suspend/resume through Godot;
- all character-creation modes in the client;
- a polished load-game flow;
- full controller acceptance;
- production art, final animation, VFX, audio, or typography;
- complete inventory/equipment management;
- combat item use;
- comprehensive enemy AI or final combat balance;
- full death/Legacy replacement UX;
- every first-slice feature in one mandatory run;
- a second expedition; or
- the complete TEL-127 Playable Godot Vertical Slice gate.

Those remain valid later work, but they must not displace the five-floor demo
from the top of the scheduling queue.

## Remaining MVP work

The remaining work is intentionally split into four project-local TEL slices:

- **TEL-129 — Compose deterministic floors 1–5 Godot session.** Make the hosted
  session floor-aware and expose legitimate stair/exit transitions.
- **TEL-130 — Compose first-slice encounters, features, and treasure into the
  Godot demo.** Connect the authored production content to normal play.
- **TEL-131 — Close MVP demo setup and combat playthrough path.** Supply an
  explicit demo-ready player/configuration and ensure an encounter can progress
  through a usable combat action path.
- **TEL-132 — Verify the fixed-seed five-floor MVP demo.** Perform the real
  Godot acceptance run and record evidence against the MVP gate.

The scheduling intent is sequential: TEL-129 → TEL-130 → TEL-131 → TEL-132,
unless repository evidence shows a dependency can safely be relaxed.

## Relationship to later milestones

After TEL-132 passes, the project resumes the broader production-shaped client
work:

1. TEL-126 — Godot save/suspend/resume and lifecycle integration;
2. TEL-127 — complete Playable Godot Vertical Slice acceptance;
3. TEL-128 — Art Production Ready acceptance;
4. production-art/audio batches derived from the passed readiness gate.

The canonical MVP acceptance checklist is
[`docs/gates/FIVE-FLOOR-MVP-DEMO.md`](gates/FIVE-FLOOR-MVP-DEMO.md). The living
coordination plan is
[`docs/exec-plans/active/FIVE-FLOOR-MVP-DEMO.md`](exec-plans/active/FIVE-FLOOR-MVP-DEMO.md).
