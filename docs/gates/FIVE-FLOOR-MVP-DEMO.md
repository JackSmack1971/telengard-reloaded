# Five-Floor MVP Demo gate

## Owner

TEL-132 — Verify fixed-seed five-floor MVP demo.

## Purpose

This gate answers one narrow question: **can a player launch the Godot client and
play a real, deterministic Telengard expedition through floors 1–5 without
developer/debug commands?**

It is intentionally smaller than `GODOT-PLAYABLE-SLICE.md`. Passing this gate
makes the repository demonstrable; it does not mean save/resume, full client UX,
controller coverage, death/Legacy flow, or Art Production Ready are complete.

## Preconditions

The gate is not eligible to pass until:

- TEL-110 through TEL-116 first-slice content is implemented and verified;
- TEL-120, TEL-123, and TEL-124 production-shaped Godot foundations are
  implemented and verified;
- TEL-129 through TEL-131 are implemented and verified; and
- the demo uses the production `ContentPack` rather than test-only fixtures.

## Acceptance checklist

### Startup and authority

- [ ] Godot launches into a usable demo start flow.
- [ ] The demo starts from authoritative Core state.
- [ ] No required gameplay step uses developer/debug commands.
- [ ] Godot does not directly mutate authoritative gameplay state.

### Five-floor traversal

- [ ] The player enters the dungeon on floor 1.
- [ ] Floor 1 uses the real deterministic generated layout for the demo seed.
- [ ] The player can reach and use legitimate stairs to descend to floor 2.
- [ ] The player can continue through legitimate stairs to floors 3, 4, and 5.
- [ ] Movement after each transition uses the destination floor's own layout.
- [ ] The rendered map/HUD clearly identifies the current floor and player
      position.

### Representative gameplay

- [ ] At least one authored first-slice monster is encountered through the normal
      content-driven encounter pipeline.
- [ ] At least one combat encounter reaches a player-usable authoritative action
      path and can be resolved far enough to continue the run.
- [ ] At least one authored first-slice dungeon feature can be discovered and
      activated through the normal feature pipeline.
- [ ] At least one first-slice treasure reward is acquired into unsecured
      expedition state through the normal loot/treasure boundary.
- [ ] The HUD/feedback communicates survival state, encounter state, floor, and
      carried progress without exposing hidden exact information that should
      remain unknown.

### Demo completion

- [ ] Reaching the designated floor-5 endpoint produces a clear end-of-demo
      presentation state or equivalent unmistakable completion feedback.
- [ ] The completion state does not require a save/load flow or a second
      expedition.
- [ ] The fixed-seed route can be repeated with equivalent authoritative results
      for the asserted deterministic behavior.

### Verification evidence

- [ ] Record the exact repository commit.
- [ ] Record the world seed and demo-ready setup/configuration used.
- [ ] Record the Godot runtime/version used for the manual run.
- [ ] Record the player-input route from startup through floor 5.
- [ ] Record focused automated verification for the new integration boundaries.
- [ ] Record the canonical full repository verification result.

## Explicitly deferred to the broader playable-slice gate

The following do not block MVP acceptance:

- Godot save/suspend/resume;
- complete load-game UX;
- all character-creation modes;
- full controller parity;
- every combat verb and combat item use;
- complete inventory/equipment UX;
- full death/Legacy replacement flow;
- second-expedition acceptance;
- production art/audio; and
- the complete checklist in `GODOT-PLAYABLE-SLICE.md`.

## Result

When every required checkbox above passes, TEL-132 records the evidence and the
Five-Floor MVP Demo is complete. Only then should the autonomous implementation
queue return to TEL-126/TEL-127/TEL-128 unless the user explicitly changes the
milestone.
