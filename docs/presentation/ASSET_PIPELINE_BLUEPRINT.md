# Godot asset pipeline blueprint

## Purpose

Define the repository contract for turning approved presentation assets into
Godot resources without coupling authoritative simulation/content state to
engine-specific paths.

This is a pipeline blueprint. Concrete import settings may evolve through
focused TEL tickets, but production asset batches must follow one validated
mapping and naming strategy rather than ad hoc scene references.

## Ownership boundary

Authoritative simulation/content owns stable game/content identity and rules.
Godot presentation owns visual/audio resources and presentation mappings.

Preferred flow:

```text
stable content/presentation id
        |
        v
presentation asset registry
        |
        v
Godot resource(s)
```

Do not persist Godot resource paths, scene UIDs, texture paths, or audio paths
inside authoritative `GameState` or save DTOs.

## Source versus runtime assets

Before production art begins, define repository locations for:

- source artwork/project files where versioning is appropriate;
- exported runtime textures;
- spritesheets/atlases;
- Godot scenes/resources;
- UI assets;
- VFX assets;
- audio source/runtime files;
- reference/style material.

Do not commit generated caches/import outputs that Godot regenerates unless the
repository explicitly requires them.

## Stable naming

Presentation mappings should use stable content/presentation IDs rather than
display names.

Example logical registry:

```text
monster/crypt_stalker
feature/azure_fountain
item/tarnished_sword
spell/ember_bolt
biome/upper_ruins
ui/hud/player_status
```

Display-name changes must not silently break resource lookup.

## Registry requirements

The first production-capable asset registry should support:

- stable key -> required presentation resource mapping;
- deterministic lookup;
- validation of duplicate keys;
- validation of missing required resources;
- explicit optional resources;
- conspicuous placeholder fallback in development where permitted;
- testable separation from authoritative simulation;
- content-version/resource-pack compatibility notes where materially needed.

Do not implement a silent fallback that makes a missing production asset look
valid at release-readiness time.

## Placeholder policy

Before the Art Production Ready gate:

- missing final assets may resolve to explicit placeholders;
- placeholders should visibly identify the missing category/key when useful;
- placeholder lookup must still exercise the same registry path intended for
  final assets;
- scenes should not bypass the registry with temporary hard-coded paths that
  later need architectural replacement.

At production readiness, the required first-slice inventory must be enumerable
and validation must identify unresolved production entries.

## Import contract to decide before asset batches

Record and standardize at minimum:

- texture filtering;
- compression policy;
- atlas/spritesheet strategy;
- sprite frame conventions;
- pivot/origin conventions;
- world-unit/tile scale;
- UI stretch/scaling behavior;
- animation naming;
- VFX scene/resource naming;
- audio formats/import settings;
- loop metadata;
- loudness/normalization convention where applicable.

If pixel art is selected, document pixel-snap and filtering rules explicitly.
If non-pixel art is selected, document the corresponding scaling/filter rules.

## Resource-state vocabulary

Each asset family must enumerate the states presentation actually needs before
final production begins.

Examples:

### Monster

```text
idle/presence
attack or action cues
hit
killed/death
special behavior cues if visible
portrait/encounter representation if used
```

### Feature

```text
undiscovered (usually no resource shown)
discovered/inactive
interaction cue
activated/changed state when observable
ambient cue
```

### Item

```text
unidentified icon if applicable
identified icon
ground/loot representation if the client uses one
equipped feedback when visually represented
```

### Spell

```text
icon
cast cue
travel/area cue if applicable
impact/resolution cue
```

The simulation does not need to know these state names unless they correspond
to legitimate domain facts; presentation maps committed state/events to visual
states.

## Binary and repository policy

Before committing large production batches, decide and document:

- which source binaries belong in Git;
- whether Git LFS is required and for which extensions/size classes;
- whether source project files are retained;
- generated/exported asset policy;
- review expectations for binary changes;
- license/provenance metadata for third-party assets.

Do not introduce a large binary-art workflow without an explicit repository
policy.

## Validation expectation

Production-capable presentation work should make it possible to answer:

- Which first-slice content IDs require assets?
- Which mappings are missing?
- Which mappings still use placeholders?
- Which resources are unreferenced/orphaned?
- Are duplicate registry keys present?
- Can a renamed display label leave stable identity intact?

TEL tickets may implement these checks incrementally, but the Art Production
Ready gate must have auditable coverage for the complete first-slice inventory.

## Asset production matrix

Before the readiness gate, maintain an inventory with fields equivalent to:

```text
content/presentation id
category
required visual resources
required audio resources
observable states
placeholder status
production status
art-direction reference
approval/review notes
```

The exact file format is a later implementation decision. The matrix must be
machine-readable or reviewable enough to detect missing first-slice coverage.

## Non-goals

This blueprint does not select an art tool, final texture format, final audio
toolchain, or final visual style. Those choices should be made deliberately
when evidence exists, then recorded in the appropriate ADR/development docs.
