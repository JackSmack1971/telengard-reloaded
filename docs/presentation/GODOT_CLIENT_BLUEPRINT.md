# Godot production client blueprint

## Purpose

This document defines the durable development methodology for building a
playable Godot client without undoing Telengard Reloaded's renderer-independent
simulation architecture.

It is a project-local extension of `docs/modern-telengard-spec.md`, not a change
to the source specification. The blueprint defines how the repository moves from
presentation separation through a demonstrable MVP, a complete first-slice
client, and finally gated production art.

## Current product sequence

The presentation roadmap is now explicitly staged:

```text
Five-Floor MVP Demo
TEL-129 -> TEL-130 -> TEL-131 -> TEL-132
                |
                v
Broader Playable Godot Vertical Slice
TEL-126 -> TEL-127
                |
                v
Art Production Ready
TEL-128
                |
                v
production art/audio batches
```

The immediate product milestone is the **Five-Floor MVP Demo** defined by
`docs/MVP_DEMO.md` and `docs/gates/FIVE-FLOOR-MVP-DEMO.md`.

The broader TEL-127 Playable Godot Vertical Slice and TEL-128 Art Production
Ready gates remain valid, but they are post-MVP work and must not pull breadth
or polish ahead of the five-floor integration proof.

The generated task index retains `core-alpha` as its machine compatibility
milestone identifier. Human-facing product priority is defined by the MVP docs,
ledger, dependency graph, and completion state rather than by changing that
legacy identifier.

## Current repository boundary — reconciled 2026-08-25

The repository is no longer at the TEL-091 visual-only prototype stage.

Implemented or substantially implemented presentation foundations now include:

- TEL-090–TEL-093: renderer-safe presentation separation and compatibility;
- TEL-107: headless representative gameplay integration proof;
- TEL-109–TEL-116: external production first-slice content pack for floors 1–5;
- TEL-120: external authoritative Godot host/bootstrap;
- TEL-121: input/simulation-clock bridge implemented and headlessly verified,
  with broader manual observation still pending;
- TEL-122: session/scene-flow implementation and runtime probe, with broader
  visual/manual observation still pending;
- TEL-123: production presentation identity and asset registry;
- TEL-124: content-aware dungeon graybox, implemented and verified; and
- TEL-125: in-progress HUD/interaction work, including combat-intent bridging.

The current architecture is therefore closer to:

```text
authoritative Core + production ContentPack
        |
        v
external Telengard.GodotHost
        |
        v
renderer-safe Modern projection
        |
        v
Godot application/session/input shell
        |
        v
graybox world + HUD/panels
```

The remaining MVP gap is not another renderer prototype. It is production-host
composition: legitimate floors 1–5 traversal, authored runtime ecology,
treasure/feature integration, reliable demo setup/combat flow, and a real
five-floor acceptance run.

## Immediate milestone: Five-Floor MVP Demo

Definition of done:

> A player can launch Godot, start a deterministic demo-ready authoritative
> session, enter the real Upper Ruins, legitimately traverse floors 1–5, engage
> representative authored gameplay, and reach a clear floor-5 end-of-demo state
> without developer/debug commands.

The MVP may use:

- a fixed world seed;
- a fixed/demo character policy;
- explicit replaceable demo tuning;
- placeholder/graybox presentation; and
- keyboard-first acceptance.

The MVP does **not** require save/load breadth, all character-creation modes,
full controller parity, complete inventory/death/Legacy UX, or production art.
Those belong to later gates.

### MVP integration sequence

#### TEL-129 — floor-aware hosted session

Compose deterministic layouts for floors 1–5 behind the Godot host boundary,
use the player's current floor layout for movement, expose legitimate Core stair
transitions, and refresh the projection after floor changes.

#### TEL-130 — production runtime ecology

Connect the production encounter tables, authored features/outcomes, and
loot/treasure boundaries to normal hosted exploration. Do not reimplement these
rules in Godot.

#### TEL-131 — demo setup and combat closure

Supply explicit demo-ready authoritative setup/configuration and ensure a
naturally triggered encounter progresses through the existing Core combat phases
to a usable player action and continuation.

#### TEL-132 — real five-floor acceptance

Run the actual fixed-seed Godot route through floor 5 and record evidence against
`docs/gates/FIVE-FLOOR-MVP-DEMO.md`.

## Post-MVP milestone: Playable Godot Vertical Slice

After TEL-132 passes, the project resumes the broader production-shaped client
milestone governed by `docs/gates/GODOT-PLAYABLE-SLICE.md` and TEL-127.

That later gate adds the breadth deliberately deferred by the MVP, including:

```text
startup / new / load
  -> complete required hero/setup flows
  -> inn/preparation
  -> full first-slice exploration/interaction breadth
  -> fight + flee + features + treasure
  -> death/Legacy outcomes
  -> return/banking/knowledge
  -> save/suspend/resume
  -> keyboard + controller acceptance
  -> begin another expedition
```

TEL-126 owns Godot persistence/application lifecycle integration before TEL-127
can pass.

A separate **Art Production Ready** transition follows TEL-127. TEL-128 owns that
gate so the project can be fully playable while still explicitly blocked from
systematic final asset production by unresolved art-direction or pipeline
choices.

## Development tracks

The earlier content/client parallel-development strategy has converged: the
representative TEL-110–TEL-116 content pack is now implemented. The immediate
queue is therefore integration-first rather than two-track content authoring.

Current scheduling priority:

1. TEL-129 — multi-floor host composition;
2. TEL-130 — encounters/features/treasure composition;
3. TEL-131 — demo setup/combat closure;
4. TEL-132 — Five-Floor MVP acceptance;
5. TEL-126 — persistence breadth;
6. TEL-127 — full playable-client acceptance;
7. TEL-128 — Art Production Ready.

Status and dependency eligibility remain canonical in `docs/tasks/README.md` and
the generated `docs/tasks/index.json`.

## Client architecture contract

The production client must preserve this ownership flow:

```text
PLAYER INPUT
    |
    v
Godot input adapter
    |
    v
host/application request boundary
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
- movement or stair legality;
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

Animation signals and UI callbacks may request simulation commands. They may not
apply authoritative results themselves.

## Required host responsibilities

The production host/application boundary must explicitly cover:

1. **Bootstrap** — load production content, create/load authoritative state, and
   establish a renderer-safe initial frame.
2. **Floor composition** — obtain deterministic layouts for the authoritative
   current floor and adjacent transition destinations without storing geography
   authority in Godot.
3. **Input bridge** — translate keyboard/controller/UI intent to existing
   simulation commands.
4. **Command dispatch** — submit commands through the simulation boundary and
   report validation failures without local state mutation.
5. **Runtime ecology composition** — supply authored encounter/feature/loot
   configuration to the existing Core resolvers rather than implementing rules
   in the host or renderer.
6. **Event collection** — capture committed domain events in order for
   presentation/audio/VFX cues.
7. **Projection refresh** — rebuild renderer-safe presentation state after
   committed changes rather than reading hidden runtime internals from Godot.
8. **Simulation clock** — drive normal/slowed/paused semantics independently of
   render FPS.
9. **Scene/session flow** — present setup, inn, dungeon, menus, death, return,
   and later load/resume states as views of simulation/application truth.
10. **Persistence flow** — after MVP, invoke explicit save/suspend/load
    boundaries and surface version/format errors safely.
11. **Content/resource binding** — resolve stable content/presentation identities
    to Godot assets without putting Godot resource paths in authoritative state.

## Floor-aware session rule

A hosted session must never assume one `FloorLayout` represents the whole
dungeon. The authoritative player floor determines which deterministic layout is
used for movement/render projection. A stair transition uses the current layout
and the adjacent destination layout through the existing Core transition
resolver.

For the MVP, the client deliberately stops at the first-slice floor-5 endpoint
even though Core supports deeper floors.

This rule is an application-composition requirement, not a new simulation rule.

## Runtime ecology rule

Authored first-slice content must reach normal play through existing content/Core
boundaries:

- encounter tables produce floor-appropriate `EncounterTriggerConfiguration`;
- first-slice feature definitions produce deterministic runtime feature state and
  use existing fountain/altar/pit/teleporter resolvers;
- loot tables feed the existing treasure acquisition boundary;
- carried/unsecured and secured progress remain distinct.

Do not introduce renderer-owned encounter rolls, feature outcomes, drop rolls,
or wealth mutation to make the MVP easier.

## Presentation-contract rule

The `ModernRenderFrame`/presentation projection is a renderer-safe contract, not
permission for Godot to reach into `GameState`, content internals, or save DTOs.
When the client needs new observable drawing/UI data, extend the renderer-safe
projection at the appropriate boundary.

Production presentation should receive only information legitimately observable
by the player, including when applicable:

- visible tile/connection geometry;
- current floor/player position;
- stair/door/observed-feature markers;
- biome/environment theme identity;
- observed monster presentation identity;
- item/spell presentation identity once legitimately known;
- safe HUD state and command availability;
- presentation cues derived from committed events.

A projection must not expose hidden feature outcomes, raw danger values,
unobserved monster internals, undiscovered geography, or other information the
simulation intentionally withholds.

## Presentation asset identity

Presentation resources map from stable content/presentation IDs through the
presentation-side registry. Do not scatter direct ID-to-resource-path
conditionals throughout Godot scenes and do not put Godot resource paths into
authoritative simulation/save state.

Representative identity shape:

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

Whether presentation keys become standardized fields in additional content
schemas requires an explicit architecture decision; until then, keep mapping
presentation-side.

## Placeholder-first rule

Every presentation feature should first work with cheap, conspicuous
placeholders. A placeholder should prove:

- scale and camera fit;
- visibility/fog readability;
- interaction affordance;
- state transitions;
- controller/keyboard navigation where required by the current gate;
- combat readability;
- asset-state requirements;
- deterministic replay compatibility.

Placeholder quality is enough for TEL-132 and TEL-127. Final production assets
still wait for TEL-128.

## UX contract

Use `docs/presentation/UX_INTERACTION_BLUEPRINT.md` for user-flow and input
rules. Durable principles remain:

- keyboard-first;
- controller-capable;
- configurable;
- contextual;
- low clutter;
- distinguish deliberate mystery from missing/bad UI information.

For the Five-Floor MVP, keyboard-first normal input is sufficient; complete
controller parity remains a TEL-127 requirement.

## Art and asset pipeline contract

Use:

- `docs/presentation/ART_DIRECTION_BLUEPRINT.md` for visual-language decisions;
- `docs/presentation/ASSET_PIPELINE_BLUEPRINT.md` for resource organization,
  import, validation, and content-ID mapping;
- `docs/gates/ART-PRODUCTION-READY.md` for TEL-128 acceptance before systematic
  production asset batches.

Art direction can be explored early. Production inventory must not displace MVP
integration and should not be frozen until TEL-128 passes.

## Autonomous selection policy

`$telengard-next-slice` must:

1. read `docs/MVP_DEMO.md` while TEL-132 is unfinished and the repository marks
   the Five-Floor MVP Demo as the current product checkpoint;
2. select TEL-129, then TEL-130, TEL-131, and TEL-132 as dependencies allow;
3. require evidence before allowing post-MVP TEL-126–TEL-128 work to pre-empt an
   eligible MVP ticket;
4. use explicit ticket dependencies rather than numeric order alone;
5. keep production-art work blocked until TEL-128 passes;
6. require presentation observation for Godot-visible changes when the selected
   ticket requires it;
7. use `eng/godot-doctor.ps1` before claiming the runtime is unavailable; and
8. stop on unresolved tuning/visual/repository policy rather than inventing a
   permanent policy to keep automation moving.

## Art-production transition

The project has four distinct presentation stages:

### 1. Visual development — allowed

- styleframes;
- tile/camera studies;
- UI wireframes;
- monster silhouettes;
- lighting tests;
- atmosphere studies;
- placeholder/icon language experiments.

### 2. Five-Floor MVP integration — TEL-129 through TEL-132

- real multi-floor hosted session;
- production encounters/features/treasure in normal play;
- reliable demo combat/setup path;
- fixed-seed five-floor acceptance.

### 3. Broad graybox playable production — TEL-126/TEL-127

- save/load lifecycle;
- complete required first-slice UX breadth;
- keyboard/controller acceptance;
- full representative loop acceptance.

### 4. Production art — TEL-128 gated

Final tiles, sprites, animation sets, VFX, UI art, icons, and production audio
begin as systematic content batches only after TEL-128 records passing evidence
for `docs/gates/ART-PRODUCTION-READY.md`.

## Non-goals

This blueprint does not:

- choose the final visual style;
- define permanent gameplay balance;
- authorize broad fifty-floor content production;
- move gameplay rules into Godot;
- require save/load or full controller breadth before the five-floor MVP;
- require final art before the game is playable;
- require one giant "build the client" pull request.

The one-slice transaction rule remains in force. Each TEL ticket must be small
enough to implement, observe, review, verify, and merge independently.
