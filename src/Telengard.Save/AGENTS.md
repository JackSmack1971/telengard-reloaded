# Telengard.Save guidance

Persistence is an explicit compatibility boundary.

- Never serialize runtime domain objects as the save contract.
- Persist through explicit DTOs and the migration boundary.
- Preserve save/simulation/generator/content version fields required for replay and compatibility.
- For persisted-field changes, decide whether a schema/version increment and migration are required before editing the serializer.
- Reject malformed/unsupported state deliberately; do not silently coerce data into a plausible but different game state.
- Add round-trip tests and, when schema compatibility changes, old-version migration/rejection tests.
- Save/load must not invent gameplay events or become an alternate authoritative state owner.

## Review focus
Flag persisted shape changes without DTO/version/migration treatment, dropped version metadata, nondeterministic serialization that harms stable comparisons, or runtime-object serialization shortcuts.
