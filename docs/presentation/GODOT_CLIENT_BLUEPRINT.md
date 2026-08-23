# Godot production client blueprint

## Purpose

This document defines the durable development methodology for moving from the
TEL-091 Modern renderer prototype to a complete playable Godot client without
undoing Telengard Reloaded's renderer-independent simulation architecture.

It is a project-local extension of `docs/modern-telengard-spec.md`, not a change
to the source specification. The source specification deliberately stops at a
presentation-separation proof before presentation becomes expensive. This
blueprint defines the next repository milestone and the gates that must precede
large-scale production art.

## Current repository boundary

TEL-090 through TEL-093 proved the presentation boundary:

```text
authoritative GameState
        |
        v
PresentationStateAdapter
        |
        v
ModernRenderFrame
        |
        v
visual-only Godot prototype
```

`src/Telengard.Godot/ModernRenderer.tscn` is therefore a renderer prototype,
not a complete game client. It does not currently own the application/session
lifecycle, simulation hosting, input-to-command bridge, simulation clock,
content bootstrap, save/resume flow, menu/navigation flow, or production asset
pipeline.

TEL-107 proves that the representative gameplay systems compose headlessly.
TEL-109 establishes the external content-pack boundary. TEL-110 through TEL-116
remain the authored first-slice content track.

## New milestone: Playable Godot Vertical Slice

The next presentation milestone is **Playable Godot Vertical Slice**.

Definition of done:

> A player can launch the Godot client and complete the representative floors
> 1-5 Telengard loop using ordinary keyboard/controller interaction and real
> authored slice content, while every authoritative state change remains owned
> by the renderer-independent simulation.

The client must support, with placeholder/graybox visuals where appropriate:

```text
launch
  -> create/select hero
  -> deterministic world setup
  -> inn/preparation
  -> enter dungeon
  -> move/explore/map
  -> interact with features
  -> encounter monster
  -> fight or flee
  -> acquire treasure
  -> descend or retreat
  -> die or reach safety
  -> bank progress / retain knowledge as applicable
  -> save/suspend/resume
  -> begin another expedition
```

Passing this milestone is governed by
`docs/gates/GODOT-PLAYABLE-SLICE.md`.

## Development tracks

Development proceeds as two coordinated tracks rather than one strictly serial
queue.

### Track A — Representative authored content

TEL-110 through TEL-116 author the §48 floors 1-5 content pack:

- biome/band data;
- monster roster;
- encounter ecology;
- item roster;
- loot tables;
- spell roster;
- fountain/altar/pit/teleporter definitions.

### Track B — Playable Godot client

TEL-120 onward owns the production-client transition. Foundation slices that do
not depend on final content identities may proceed while TEL-110 through
TEL-116 are still being authored. Content-dependent graybox/integration slices
must declare and respect their explicit TEL-110–TEL-116 dependencies.

The autonomous next-slice workflow must compare eligible work across both
tracks. It must not rigidly choose the lowest TEL number, and it must not starve
one track when a small dependency-critical slice on the other track would
unlock the convergence milestone.

## Convergence rule

The tracks converge before broad presentation production:

```text
TEL-110..116 real slice content -------+
                                       |
Godot host/input/session foundation ---+--> graybox playable client
                                       |
presentation + asset contracts --------+
                                              |
                                              v
                                  GODOT-PLAYABLE-SLICE gate
                                              |
                                              v
                                  ART-PRODUCTION-READY gate
                                              |
                                              v
                                  production asset batches
```

No final/expensive production-art batch should be introduced before
`ART-PRODUCTION-READY` passes. Concept exploration, style studies, UI
wireframes, placeholder assets, debug graphics, and graybox presentation are
allowed before that gate because they exist to validate the client and visual
language rather than lock production inventory.

## Client architecture contract

The production client must preserve this ownership flow:

```text
PLAYER INPUT
    |
    v
Godot input adapter
    |
    v
simulation command
    |
    v
authoritative simulation
    |
    +--> committed domain events
    |
    v
PresentationStateAdapter
    |
    v
Modern presentation projection
    |
    v
Godot scenes / UI / animation / audio
```

### Godot may own

- input-device detection and binding presentation;
- menus, focus, navigation, windowing, accessibility presentation;
- transient animation state;
- camera state;
- audio playback state;
- visual interpolation;
- resource loading/caching;
- local visual effects;
- scene composition;
- presentation-only settings that do not alter authoritative game rules.

### Godot must not own

- authoritative player position;
- movement legality;
- encounter resolution;
- combat damage/outcomes;
- item ownership or equipment truth;
- feature outcomes;
- knowledge acquisition truth;
- carried/secured wealth transitions;
- world/encounter/loot RNG;
- death/Legacy resolution;
- save-domain state;
- any second copy of a gameplay rule.

Animation signals and UI callbacks may request simulation commands. They may
not apply authoritative results themselves.

## Required host responsibilities

The playable host boundary must explicitly cover:

1. **Bootstrap** — load content, create or load authoritative state, initialize
   command/event services, and establish the current presentation scene.
2. **Input bridge** — translate keyboard/controller/UI intent to existing
   simulation commands.
3. **Command dispatch** — submit commands through the simulation boundary and
   report validation failures without local state mutation.
4. **Event collection** — capture committed domain events in order for
   presentation/audio/VFX cues.
5. **Projection refresh** — rebuild renderer-safe presentation state after
   committed changes rather than reading hidden runtime internals from Godot.
6. **Simulation clock** — drive normal/slowed/paused simulation semantics
   independently of render FPS.
7. **Scene/session flow** — transition among setup, inn, dungeon, menus, death,
   and Legacy replacement as a presentation of simulation state.
8. **Persistence flow** — invoke explicit save/suspend/load boundaries and
   surface version/format errors safely.
9. **Content/resource binding** — resolve stable content/presentation identities
   to Godot assets without placing Godot resource paths in authoritative state.

## Presentation-contract rule

The current `ModernRenderFrame` is a prototype contract, not automatically the
final production visual contract. Before a Godot scene reaches into
`GameState`, content internals, or save DTOs to obtain missing drawing data,
expand the renderer-safe projection instead.

Production presentation should receive only information legitimately observable
by the player. Examples of likely required presentation identity include:

- visible tile/connection geometry;
- stair/door/feature markers;
- biome/environment theme identity;
- monster presentation identity for an observed encounter;
- item/spell presentation identity once legitimately known;
- safe UI state and command availability;
- presentation cues derived from committed events.

A projection must not expose hidden feature outcomes, raw danger values,
unobserved monster internals, undiscovered geography, or other information the
simulation intentionally withholds.

## Presentation asset identity

Presentation resources should be mapped from stable content/presentation IDs by
a presentation-side registry. Prefer a manifest/resource registry such as:

```text
monster: crypt_stalker
  world_sprite
  combat_visual
  hit_vfx
  death_vfx
  audio_set

item: tarnished_sword
  inventory_icon
  ground_visual

spell: ember_bolt
  icon
  cast_vfx
  impact_vfx
  audio

feature: azure_fountain
  scene
  ambient_vfx
  interaction_vfx
  audio
```

Do not scatter direct content-id-to-resource-path conditionals throughout Godot
scenes. Do not put Godot resource paths into authoritative simulation state.
Whether presentation keys become standardized fields in additional content
schemas requires an explicit architecture decision; until then, keep the
mapping presentation-side.

## Placeholder-first rule

Every presentation feature should first work with cheap, conspicuous
placeholders. A placeholder should prove:

- scale and camera fit;
- visibility/fog readability;
- interaction affordance;
- state transitions;
- controller/keyboard navigation;
- combat readability;
- asset-state requirements;
- deterministic replay compatibility.

Only after the placeholder path works should an implementation slice replace it
with final production assets.

## UX contract

Use `docs/presentation/UX_INTERACTION_BLUEPRINT.md` for user-flow and input
rules. Required design principles remain:

- keyboard-first;
- controller-first;
- configurable;
- contextual;
- low clutter;
- distinguish deliberate mystery from missing/bad UI information.

## Art and asset pipeline contract

Use:

- `docs/presentation/ART_DIRECTION_BLUEPRINT.md` for visual-language decisions;
- `docs/presentation/ASSET_PIPELINE_BLUEPRINT.md` for resource organization,
  import, validation, and content-ID mapping;
- `docs/gates/ART-PRODUCTION-READY.md` before production asset batches.

Art direction can be explored early. Production inventory should not be frozen
until the corresponding authored content identity and placeholder states are
stable.

## Ticket sequence

The canonical Godot client tickets live in `docs/tasks/README.md` and individual
`docs/tasks/TEL-120.md` onward files. Their dependency graph, not numeric order,
controls eligibility.

At a high level the sequence is:

```text
host/bootstrap
  -> input + simulation clock
  -> session/scene flow
  -> production presentation + asset contract
  -> content-aware dungeon graybox
  -> HUD/interaction graybox
  -> persistence + full playable integration
  -> art-production readiness review
```

## Autonomous selection policy

`$telengard-next-slice` must:

1. read this blueprint when any TEL-110–TEL-127 work is a serious candidate;
2. inspect both content and Godot tracks;
3. use explicit ticket dependencies to determine eligibility;
4. prefer dependency-unblocking work and convergence toward the playable slice;
5. avoid production-art work before the readiness gate;
6. require presentation observation for Godot-visible changes when the ticket
   acceptance criteria call for it;
7. if the current environment cannot perform required Godot observation,
   select another eligible non-Godot slice when one exists; otherwise stop with
   a concrete environment blocker rather than weakening acceptance criteria.

## Art-production transition

The project has three distinct presentation stages:

### 1. Visual development — allowed now

- styleframes;
- tile/camera studies;
- UI wireframes;
- monster silhouettes;
- lighting tests;
- atmosphere studies;
- placeholder/icon language experiments.

### 2. Graybox playable production — begins with TEL-120+

- actual Godot host and input path;
- placeholder dungeon rendering;
- placeholder monster/feature/item/spell representations;
- complete interactive UX;
- deterministic full-loop acceptance.

### 3. Production art — gated

Final tiles, sprites, animation sets, VFX, UI art, icons, and production audio
begin as systematic content batches only after
`docs/gates/ART-PRODUCTION-READY.md` passes.

## Non-goals

This blueprint does not:

- choose the final visual style;
- define permanent gameplay balance;
- authorize broad fifty-floor content production;
- replace the TEL-110–TEL-116 first-slice content work;
- move gameplay rules into Godot;
- require final art before the game is playable;
- require one giant "build the client" pull request.

The one-slice transaction rule remains in force. Each Godot ticket must be
small enough to implement, observe, review, verify, and merge independently.
