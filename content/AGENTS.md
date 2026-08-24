# Content guidance

This directory is for data-defined monsters, items, spells, features, bands, loot tables, encounter tables, talents, and related definitions.

- Keep content data separate from authoritative simulation algorithms.
- Use stable explicit identifiers and validation once schemas are introduced.
- Do not encode renderer/UI behavior in content definitions.
- Do not create a second hidden rules engine in data files that disagrees with simulation code.
- Unknown formulas, weights, and balance values remain `CONFIGURATION/TUNING DECISION REQUIRED` unless the specification or user explicitly decides them.
- Changes that affect deterministic outcomes must have stable ordering/version semantics and deterministic fixture coverage.
- Content must not reveal hidden facts to the player merely because definitions contain them.
