# Telengard.Godot guidance

This project is a presentation boundary for the renderer-independent
simulation. It may translate observed simulation state into scenes, resources,
input affordances, and visual feedback, but it must not own authoritative
gameplay state, rules, random outcomes, save semantics, or hidden information.

- Drive gameplay through the simulation's public command/session contracts.
- Map content and presentation resources through explicit stable identifiers;
  do not infer rules from scene or resource layout.
- Render only player-observed state. Keep unobserved map, encounter, and loot
  facts out of presentation state until the simulation exposes them.
- Keep input, clock, and scene flow deterministic with respect to the supplied
  simulation/session boundary; do not use Godot timing or random APIs for
  authoritative outcomes.
- Changes that affect playable behavior require the relevant manual/runtime
  observation in addition to headless verification.

## Review focus

Flag gameplay logic in nodes/resources, reverse dependencies into Core,
presentation-state leakage of hidden facts, unstable resource mapping, and
claims of runtime success without an executable observation.
