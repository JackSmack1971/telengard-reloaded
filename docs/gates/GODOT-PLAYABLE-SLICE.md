# Playable Godot Vertical Slice gate

## Purpose

This gate determines whether the project has moved beyond the TEL-091 renderer
prototype into a complete first-slice Godot client that exercises the real
authoritative simulation end to end.

Passing this gate does **not** require final production art. Placeholders and
graybox visuals are preferred until the client and presentation contracts are
stable.

## Sources of truth

- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`
- `docs/presentation/UX_INTERACTION_BLUEPRINT.md`
- `docs/modern-telengard-spec.md` §3, §12, §36–§39, §47–§48, §51–§52
- `docs/INVARIANTS.md`
- TEL-110 through TEL-127 and their verification evidence

## Preconditions

The gate is not eligible to pass until:

- TEL-110 through TEL-116 are implemented and verified;
- all Godot client tickets required by the ledger through the playable-slice
  integration ticket are implemented and verified;
- the current first-slice content pack loads through the production content
  boundary rather than test-only fixtures.

## Acceptance checklist

### Application/session lifecycle

- [ ] Godot launches into a usable startup/new/load flow.
- [ ] A new deterministic game can be created through the existing character
      creation and world-setup boundaries.
- [ ] A valid save can be loaded into the same authoritative simulation model.
- [ ] The client can return to a stable menu/inn state after expedition success,
      death, or Legacy replacement as applicable.

### Input and simulation authority

- [ ] Required first-slice gameplay actions are reachable by keyboard.
- [ ] Required first-slice gameplay actions are reachable by controller.
- [ ] Gameplay input is translated into simulation commands/application
      boundaries rather than direct GameState mutation.
- [ ] Invalid commands are rejected by the simulation without presentation-owned
      corrective mutation.
- [ ] Animation/UI callbacks do not apply authoritative gameplay outcomes.

### Simulation clock

- [ ] Simulation progression is independent of rendering FPS.
- [ ] Normal/slowed/paused presentation modes use the renderer-independent time
      boundary defined by the simulation/application architecture.
- [ ] Opening a presentation-only menu does not accidentally advance or mutate
      simulation state beyond the intended time-mode policy.

### End-to-end representative loop

Using real first-slice content, a player can:

- [ ] create a hero;
- [ ] begin at the inn/preparation state;
- [ ] enter the dungeon;
- [ ] explore and see only legitimately known geography;
- [ ] encounter at least one first-slice monster;
- [ ] receive threat communication without hidden exact stats;
- [ ] fight;
- [ ] flee;
- [ ] interact with fountain;
- [ ] interact with altar;
- [ ] encounter a pit position-changing outcome;
- [ ] encounter a teleporter position-changing outcome;
- [ ] find first-slice treasure;
- [ ] distinguish carried/unsecured from secured progress;
- [ ] descend and/or retreat;
- [ ] return to the inn and secure eligible progress;
- [ ] die and resolve the selected death mode;
- [ ] observe/persist applicable journal knowledge;
- [ ] begin another expedition.

### Persistence

- [ ] Suspend/save can be initiated through the client.
- [ ] Resume/load reproduces authoritative state through explicit save DTOs.
- [ ] Renderer-specific transient state is not required to preserve authoritative
      gameplay correctness.
- [ ] A fixed-seed scripted/manual route remains authoritative-equivalent across
      save/resume.

### Presentation contract

- [ ] Godot consumes renderer-safe projections/events rather than hidden Core
      internals.
- [ ] Visible tile/connection/biome information needed for the graybox renderer
      is available through a documented presentation contract.
- [ ] Observed monster/feature/item/spell presentation identity is available
      without exposing unobserved hidden facts.
- [ ] Unknown geography and hidden feature/monster information remain redacted.
- [ ] Presentation resource lookup uses the documented presentation asset
      registry/fallback path.

### UX completeness

- [ ] Startup/new/load flow exists.
- [ ] Character-creation UI covers all required creation modes.
- [ ] Inn/preparation UI exists.
- [ ] Exploration HUD and map/journal access exist.
- [ ] Feature interaction is usable without debug commands.
- [ ] Combat actions are usable without debug commands.
- [ ] Inventory/equipment UI exists for the required slice.
- [ ] Spell selection/quick-cast interaction exists for the required slice.
- [ ] Pause/settings navigation is usable.
- [ ] Death/Legacy flow is usable.
- [ ] Known information is not omitted in a way that creates accidental mystery.

### Graybox presentation

- [ ] Floors 1-5 can be rendered/read using placeholders or approved graybox
      assets.
- [ ] Player position and movement are visually understandable.
- [ ] Fog/unknown/observed/visited/current states are distinguishable.
- [ ] First-slice monsters have distinct placeholder presentation identities.
- [ ] Fountain, altar, pit, and teleporter have distinct placeholder presentation
      identities.
- [ ] First-slice item/spell UI entries can be distinguished.
- [ ] Encounter, damage, death, feature, treasure, and banking feedback have
      visible placeholder cues.

## Verification evidence

The gate review must record:

- exact repository verification commands and results;
- focused tests for command/projection/persistence boundaries;
- fixed-seed scenario used for the client acceptance run;
- Godot version/runtime used for manual presentation acceptance;
- concise keyboard/controller observations;
- any known visual-only defects explicitly deferred to production art/polish.

A headless test alone cannot pass this gate because the user-visible client is
the subject of the gate.

## Failure conditions

The gate fails if:

- the client only renders a supplied test/prototype frame and cannot host a real
  simulation session;
- Godot owns authoritative gameplay state or outcomes;
- required first-slice content is still test-fixture-only;
- save/resume requires renderer-specific authoritative data;
- any required user action is only possible through developer/debug tooling;
- required Godot/manual observation was not performed;
- hidden information is exposed to simplify presentation.

## Result

When all acceptance criteria pass, record the date, commit/PR, verification
commands, Godot acceptance environment, and remaining presentation-only defects.
Then proceed to `ART-PRODUCTION-READY.md`; do not treat this gate alone as
permission for broad final asset production.
