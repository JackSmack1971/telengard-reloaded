# Art direction blueprint

## Purpose

Define what visual decisions must become explicit before Telengard Reloaded
commits to large-scale production art. This is a decision framework and
readiness checklist, not the final art bible.

Visual exploration may begin before this document is fully resolved. Final
production asset batches must not begin until the Art Production Ready gate
passes.

## Scope

The first art-direction lock covers only the representative floors 1-5 vertical
slice and the playable Godot client. It does not require visual design for all
50 floors.

## Decisions to establish

### Camera and world presentation

Document:

- 2D camera model;
- tile/grid relationship;
- visible playfield scale;
- camera tracking behavior;
- whether actors are world sprites, portraits, overlays, or a combination;
- how stairs, doors, corridors, walls, features, hazards, and unknown geography
  remain readable.

### Native resolution and scaling

Document:

- reference viewport;
- tile dimensions;
- sprite reference scale;
- UI scale strategy;
- texture filtering expectations;
- pixel snapping rules if applicable;
- high-DPI/window scaling behavior.

### Upper Ruins visual language

For floors 1-5 establish:

- palette/value hierarchy;
- material language;
- architectural motifs;
- floor/wall/door/stair readability;
- lighting baseline;
- environmental atmosphere;
- landmark/feature contrast;
- how the biome differs from the inn.

Do not define deeper-band art merely to fill a future catalog.

### Fog and knowledge states

The visual language must clearly distinguish at minimum:

- unknown;
- observed;
- visited;
- current/visible;
- discovered feature;
- suspected/partial information when future knowledge systems require it.

Unknown geography must not appear fully mapped.

### Actor silhouette and encounter readability

Establish:

- player silhouette/marker treatment;
- monster scale range;
- family-level visual cues without revealing hidden stats;
- threat presentation language;
- damage/hit/death readability;
- selection/targeting language;
- minimum readable animation state set.

The first-slice monster roster from TEL-111 defines the production inventory.
Do not lock final monster assets before those identities and roles are stable.

### Feature readability and mystery

For fountain, altar, pit, and teleporter establish:

- silhouette/shape language;
- interaction affordance;
- ambient cues;
- activated/discovered state requirements;
- how visual observations can support learnable mystery without exposing hidden
  outcome probabilities.

TEL-116 defines the authored identities and outcomes used by the production
inventory.

### Item and spell iconography

Once TEL-113 and TEL-115 stabilize, define:

- inventory icon dimensions;
- unidentified-versus-identified language;
- equipment category language;
- curse/affix communication rules that do not expose hidden facts;
- spell icon family;
- cast/impact VFX readability and intensity.

### UI visual hierarchy

Use `UX_INTERACTION_BLUEPRINT.md` as the behavioral source. Art direction must
cover:

- typography;
- focus/selection states;
- HUD hierarchy;
- carried versus secured wealth distinction;
- encounter/threat presentation;
- journal/map visual language;
- modal hierarchy;
- controller glyph policy;
- accessibility alternatives to color-only meaning where practical.

### Lighting and atmosphere

Modern mode is expected to support dynamic lighting and atmospheric effects.
Define:

- what information lighting may communicate;
- what remains cosmetic;
- safe luminance/readability limits;
- how danger is suggested without exposing a raw danger meter;
- contrast between inn safety and dungeon pressure.

### Animation cadence

Before production animation batches, define:

- target timing/cadence;
- state machine vocabulary needed only for presentation;
- movement interpolation policy;
- hit/death/feature interaction timing;
- whether animation may delay command submission or only visualize committed
  outcomes.

Animation callbacks must never become gameplay authority.

## Placeholder requirement

Every planned final asset category must have a working placeholder state before
production art begins. This includes:

- environment tiles;
- player;
- each first-slice monster presentation identity;
- each first-slice feature;
- item icons/ground representation where required;
- spell icons/VFX placeholders;
- HUD and menu wireframes;
- critical feedback cues.

Placeholder failure is a client/contract problem, not an art-production problem.

## Art review packet for the readiness gate

The Art Production Ready review should be able to inspect:

1. reference viewport and tile/sprite scale sheet;
2. Upper Ruins environment styleframe(s);
3. inn-versus-dungeon contrast study;
4. fog/visibility state study;
5. first-slice monster silhouette/reference sheet;
6. four feature visual studies;
7. representative item/spell icon language;
8. HUD/menu wireframes with focus states;
9. lighting/atmosphere study;
10. animation/VFX state list;
11. accessibility/readability notes.

These artifacts can be concept/reference material. They do not need to be final
production assets.

## Production lock rule

A content identity is ready for final art only when:

- its stable content ID exists;
- its gameplay purpose/state vocabulary is known;
- the renderer-safe presentation contract exposes the required observable data;
- its placeholder works in the playable client;
- its scale/style requirements are covered by this art-direction contract;
- the asset pipeline has a validated mapping entry.

If any item is missing, continue using a placeholder rather than guessing a
final production asset contract.
