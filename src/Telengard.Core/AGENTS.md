# Telengard.Core guidance

`Telengard.Core` is the authoritative, renderer-independent simulation boundary.

- Keep all authoritative state transitions here or in other explicitly simulation-owned code.
- Inputs are commands/intents; validate before mutation.
- Emit domain events only for committed facts and keep payloads stable/minimal.
- Never reference Godot, terminal/UI types, sprites, scenes, fonts, animation, input devices, or renderer-specific state.
- Use `DeterministicRng`/scoped streams for authoritative randomness; include stable version/scope inputs and test same-input replay.
- Avoid dependence on iteration order of hash-based/unordered containers when order can affect authoritative results.
- Keep hidden state distinct from player-observed knowledge.
- When generation behavior changes, assess generator-version compatibility before changing outputs for existing seeds.
- Add focused tests for invariants, boundaries, determinism, and emitted events relevant to the change.

## Review focus
Flag changes that create presentation dependencies, bypass command validation, mutate after event publication, use uncontrolled randomness, or change deterministic generation without an explicit versioning decision.
